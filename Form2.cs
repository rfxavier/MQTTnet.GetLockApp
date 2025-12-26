using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json.Linq;

namespace MQTTnet.GetLockApp
{
    public partial class Form2 : Form
    {
        private const int QUEUE_CAPACITY = 1000;
        private const int WORKER_COUNT = 2;

        private readonly string _connectionString =
            $@"Server={ConfigurationManager.AppSettings["sqlServer"]};
               Database={ConfigurationManager.AppSettings["sqlServerDatabase"]};
               User Id={ConfigurationManager.AppSettings["sqlServerUser"]};
               Password={ConfigurationManager.AppSettings["sqlServerPassword"]};";

        private IManagedMqttClient _mqttClient;

        private BlockingCollection<MqttWorkItem> _queue;
        private CancellationTokenSource _cts;
        private Task[] _workers;

        public Form2()
        {
            InitializeComponent();
            Load += Form2_Load;
            FormClosing += Form2_FormClosing;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            StartPipeline();
            StartMqtt();
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopPipeline();
            try { _mqttClient?.StopAsync().GetAwaiter().GetResult(); } catch { }
        }

        // =====================================================
        // PIPELINE
        // =====================================================
        private void StartPipeline()
        {
            _queue = new BlockingCollection<MqttWorkItem>(QUEUE_CAPACITY);
            _cts = new CancellationTokenSource();

            _workers = new Task[WORKER_COUNT];
            for (int i = 0; i < WORKER_COUNT; i++)
                _workers[i] = Task.Run(() => WorkerLoop(_cts.Token));
        }

        private void StopPipeline()
        {
            try
            {
                _cts.Cancel();
                _queue.CompleteAdding();
                Task.WaitAll(_workers, TimeSpan.FromSeconds(5));
            }
            catch { }
        }

        private async Task WorkerLoop(CancellationToken ct)
        {
            foreach (var item in _queue.GetConsumingEnumerable(ct))
            {
                try
                {
                    var parsed = ParseMessage(item.Payload);
                    await InsertMessageAsync(parsed, ct).ConfigureAwait(false);
                    await _mqttClient.PublishAsync(BuildAck()).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch { /* silent by design */ }
            }
        }

        // =====================================================
        // MQTT (OLDER MQTTnet API - EVENT BASED)
        // =====================================================

        private async void StartMqtt()
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateManagedMqttClient();

            _mqttClient.UseApplicationMessageReceivedHandler(e =>
            {
                var topic = e.ApplicationMessage?.Topic;
                if (string.IsNullOrEmpty(topic))
                    return;

                // === EXACT semantic equivalent to Form1 ===
                // Only process topics that end with "/MESSAGE" (case-insensitive)
                if (!topic.EndsWith("/MESSAGE", StringComparison.OrdinalIgnoreCase))
                    return;

                string payload =
                    e.ApplicationMessage.Payload == null
                        ? string.Empty
                        : Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                _queue.TryAdd(new MqttWorkItem(payload), 50);
            });

            // Resolve settings with fallbacks (no UI logging)
            string mqttServer = GetRequiredAppSetting(new[]
            {
                "mqttServer"
            });

            int mqttPort = GetAppSettingInt(new[]
            {
                "mqttServerPort"
            }, defaultValue: 1883);

            string mqttClientId = GetAppSetting(new[]
            {
                "mqttClientId"
            }) ?? ("mqtt-winforms-client-" + Guid.NewGuid().ToString("N"));

            // Credentials are optional on many brokers; we’ll read them if present.
            string mqttUser = GetAppSetting(new[]
            {
                "mqttServerUsername"
            });

            string mqttPassword = GetAppSetting(new[]
            {
                "mqttServerPassword"
            });

            var builder = new MqttClientOptionsBuilder()
                .WithClientId(mqttClientId)
                .WithTcpServer(mqttServer, mqttPort);

            if (!string.IsNullOrWhiteSpace(mqttUser))
                builder = builder.WithCredentials(mqttUser, mqttPassword ?? string.Empty);

            var clientOptions = builder.Build();

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithClientOptions(clientOptions)
                .Build();

            await _mqttClient.StartAsync(managedOptions);
            await _mqttClient.SubscribeAsync("/#");
        }

        // ---------- Helpers (add to Form2) ----------

        private static string GetAppSetting(string[] keys)
        {
            foreach (var k in keys)
            {
                var v = ConfigurationManager.AppSettings[k];
                if (!string.IsNullOrWhiteSpace(v))
                    return v.Trim();
            }
            return null;
        }

        private static string GetRequiredAppSetting(string[] keys)
        {
            var v = GetAppSetting(keys);
            if (string.IsNullOrWhiteSpace(v))
            {
                // No UI logging; throw a diagnostic exception
                throw new ConfigurationErrorsException(
                    "Missing required AppSetting. Tried keys: " + string.Join(", ", keys));
            }
            return v;
        }

        private static int GetAppSettingInt(string[] keys, int defaultValue)
        {
            var s = GetAppSetting(keys);
            if (string.IsNullOrWhiteSpace(s))
                return defaultValue;

            if (int.TryParse(s.Trim(), out var value))
                return value;

            throw new ConfigurationErrorsException(
                $"Invalid integer AppSetting value '{s}' for keys: {string.Join(", ", keys)}");
        }


        // =====================================================
        // PARSE MESSAGE
        // =====================================================
        private ParsedMessage ParseMessage(string payload)
        {
            var jo = JObject.Parse(payload);

            var info = jo["INFO"] as JObject;
            var data = jo["DATA"] as JObject;
            var user = jo["USER"] as JObject;
            var balance = jo["BALANCE"] as JObject;

            return new ParsedMessage
            {
                // INFO
                InfoId = (string)info?["ID"],
                InfoIp = (string)info?["IP"],
                InfoMac = (string)info?["MAC"],
                InfoJson = (string)info?["JSON"],

                // DATA (note: numeric types in your sample)
                DataHash = data?["HASH"]?.ToString(),
                DataTmstBegin = data?["TMST_BEGIN"]?.ToString(),
                DataTmstEnd = data?["TMST_END"]?.ToString(),
                DataType = data?["TYPE"]?.ToString(),

                // Examples of “$” keys (optional: map only if you need them later)
                DataCurrencyTotal = (decimal?)data?["$Total"],
                Bill2 = (long?)data?["$2"],
                Bill5 = (long?)data?["$5"],
                Bill10 = (long?)data?["$10"],
                Bill20 = (long?)data?["$20"],
                Bill50 = (long?)data?["$50"],
                Bill100 = (long?)data?["$100"],
                Bill200 = (long?)data?["$200"],
                BillRejected = (long?)data?["$REJ"],

                DataSensor = (decimal?)data?["SENSOR"],
                NumEnv = (long?)data?["NUM_ENV"],

                // USER
                UserId = user?["ID"]?.ToString(),
                UserName = (string)user?["NAME"],
                UserLastName = (string)user?["LASTNAME"],

                // BALANCE
                Balance = (decimal?)balance?["TOTAL"],
                LimitDepositEnabled = (bool?)balance?["LIMIT-DEPOSIT-ENABLE"] ?? false,
                LimitDepositValue = (decimal?)balance?["LIMIT-DEPOSIT-VALUE"],
                BalanceId = balance?["ID"]?.ToString()
            };
        }

        // =====================================================
        // SQL INSERT
        // =====================================================
        private async Task InsertMessageAsync(ParsedMessage m, CancellationToken ct)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(
                "INSERT INTO dbo.[message] (info_id, info_ip, info_mac, info_json, data_hash, data_tmst_begin, data_tmst_end, data_user, data_type, balance_id) " +
                "VALUES (@info_id,@info_ip,@info_mac,@info_json,@data_hash,@data_tmst_begin,@data_tmst_end,@data_user,@data_type,@balance_id)", conn))
            {
                cmd.Parameters.AddWithValue("@info_id", (object)m.InfoId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@info_ip", (object)m.InfoIp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@info_mac", (object)m.InfoMac ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@info_json", (object)m.InfoJson ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@data_hash", (object)m.DataHash ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@data_tmst_begin", (object)m.DataTmstBegin ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@data_tmst_end", (object)m.DataTmstEnd ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@data_user", (object)m.DataUser ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@data_type", (object)m.DataType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@balance_id", (object)m.BalanceId ?? DBNull.Value);

                await conn.OpenAsync(ct).ConfigureAwait(false);
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
        }

        // =====================================================
        // ACK
        // =====================================================
        private MqttApplicationMessage BuildAck()
        {
            return new MqttApplicationMessageBuilder()
                .WithTopic("/ACK")
                .WithPayload("{\"ack\":\"OK\"}")
                .WithAtLeastOnceQoS()
                .Build();
        }

        // =====================================================
        // MODELS
        // =====================================================
        private sealed class MqttWorkItem
        {
            public string Payload { get; }
            public MqttWorkItem(string payload) => Payload = payload;
        }

        private sealed class ParsedMessage
        {
            public string InfoId;
            public string InfoIp;
            public string InfoMac;
            public string InfoJson;

            public string DataHash;
            public string DataTmstBegin;
            public string DataTmstEnd;
            public string DataUser;
            public string DataType;

            public decimal? DataCurrencyTotal;
            public long? Bill2;
            public long? Bill5;
            public long? Bill10;
            public long? Bill20;
            public long? Bill50;
            public long? Bill100;
            public long? Bill200;
            public long? BillRejected;

            public decimal? DataSensor;
            public long? NumEnv;

            public string UserId;
            public string UserName;
            public string UserLastName;

            public decimal? Balance;
            public bool LimitDepositEnabled;
            public decimal? LimitDepositValue;
            public string BalanceId;
        }
    }
}

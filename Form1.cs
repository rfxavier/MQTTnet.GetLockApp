// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Form1.cs" company="Hämmer Electronics">
//   Copyright (c) 2020 All rights reserved.
// </copyright>
// <summary>
//   The main form.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MQTTnet.GetLockApp.WinForm
{
    using System;
    using System.Configuration;
    using System.Data.SqlClient;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using System.Timers;
    using System.Windows.Forms;

    using MQTTnet.Client.Connecting;
    using MQTTnet.Client.Disconnecting;
    using MQTTnet.Client.Options;
    using MQTTnet.Client.Receiving;
    using MQTTnet.Extensions.ManagedClient;
    using MQTTnet.Formatter;
    using MQTTnet.Protocol;
    using MQTTnet.Server;
    using Newtonsoft.Json;
    using Timer = System.Timers.Timer;

    /// <summary>
    /// The main form.
    /// </summary>
    public partial class Form1 : Form
    {
        /// <summary>
        /// The managed publisher client.
        /// </summary>
        private IManagedMqttClient managedMqttClientPublisher;

        /// <summary>
        /// The managed subscriber client.
        /// </summary>
        private IManagedMqttClient managedMqttClientSubscriber;

        /// <summary>
        /// The MQTT server.
        /// </summary>
        private IMqttServer mqttServer;

        /// <summary>
        /// The port.
        /// </summary>
        private string port = "1883";

        /// <summary>
        /// Initializes a new instance of the <see cref="Form1"/> class.
        /// </summary>
        public Form1()
        {
            this.InitializeComponent();

            var timer = new Timer
            {
                AutoReset = true, Enabled = true, Interval = 1000
            };

            timer.Elapsed += this.TimerElapsed;
        }

        /// <summary>
        /// Handles the publisher connected event.
        /// </summary>
        /// <param name="x">The MQTT client connected event args.</param>
        private static void OnPublisherConnected(MqttClientConnectedEventArgs x)
        {
            // MessageBox.Show($"Publisher Connected to {ConfigurationManager.AppSettings["mqttServer"]}", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Handles the publisher disconnected event.
        /// </summary>
        /// <param name="x">The MQTT client disconnected event args.</param>
        private static void OnPublisherDisconnected(MqttClientDisconnectedEventArgs x)
        {
            // MessageBox.Show("Publisher Disconnected", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Handles the subscriber connected event.
        /// </summary>
        /// <param name="x">The MQTT client connected event args.</param>
        private static void OnSubscriberConnected(MqttClientConnectedEventArgs x)
        {
             // MessageBox.Show($"Subscriber Connected to {ConfigurationManager.AppSettings["mqttServer"]}", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Handles the subscriber disconnected event.
        /// </summary>
        /// <param name="x">The MQTT client disconnected event args.</param>
        private static void OnSubscriberDisconnected(MqttClientDisconnectedEventArgs x)
        {
            // MessageBox.Show("Subscriber Disconnected", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// The method that handles the button click to generate a message.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void ButtonGeneratePublishedMessageClick(object sender, EventArgs e)
        {
            var message = $"{{\"dt\":\"{DateTime.Now.ToLongDateString()} {DateTime.Now.ToLongTimeString()}\"}}";
            this.TextBoxPublish.Text = message;
        }

        /// <summary>
        /// The method that handles the button click to publish a message.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private async void ButtonPublishClick(object sender, EventArgs e)
        {
            ((Button)sender).Enabled = false;

            try
            {
                var payload = Encoding.UTF8.GetBytes(this.TextBoxPublish.Text);
                var message = new MqttApplicationMessageBuilder().WithTopic(this.TextBoxTopicPublished.Text.Trim()).WithPayload(payload).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).WithRetainFlag().Build();

                if (this.managedMqttClientPublisher != null)
                {
                    await this.managedMqttClientPublisher.PublishAsync(message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Occurs", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            ((Button)sender).Enabled = true;
        }

        /// <summary>
        /// The method that handles the button click to start the publisher.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private async void ButtonPublisherStartClick(object sender, EventArgs e)
        {
            var mqttFactory = new MqttFactory();

            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = false, IgnoreCertificateChainErrors = true, IgnoreCertificateRevocationErrors = true, AllowUntrustedCertificates = true
            };

            var options = new MqttClientOptions
            {
                ClientId = $"ClientPublisherGetLock{Guid.NewGuid()}",
                ProtocolVersion = MqttProtocolVersion.V311,
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = ConfigurationManager.AppSettings["mqttServer"], Port = int.Parse(ConfigurationManager.AppSettings["mqttServerPort"]), TlsOptions = tlsOptions
                }
            };

            if (options.ChannelOptions == null)
            {
                throw new InvalidOperationException();
            }

            options.Credentials = new MqttClientCredentials
            {
                Username = ConfigurationManager.AppSettings["mqttServerUsername"], Password = Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["mqttServerPassword"])
            };

            options.CleanSession = true;
            options.KeepAlivePeriod = TimeSpan.FromSeconds(5);
            this.managedMqttClientPublisher = mqttFactory.CreateManagedMqttClient();
            this.managedMqttClientPublisher.UseApplicationMessageReceivedHandler(this.HandleReceivedApplicationMessage);
            this.managedMqttClientPublisher.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnPublisherConnected);
            this.managedMqttClientPublisher.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnPublisherDisconnected);

            await this.managedMqttClientPublisher.StartAsync(
                new ManagedMqttClientOptions
                {
                    ClientOptions = options
                });
        }

        /// <summary>
        /// The method that handles the button click to stop the publisher.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private async void ButtonPublisherStopClick(object sender, EventArgs e)
        {
            if (this.managedMqttClientPublisher == null)
            {
                return;
            }

            await this.managedMqttClientPublisher.StopAsync();
            this.managedMqttClientPublisher = null;
        }

        /// <summary>
        /// The method that handles the button click to start the server.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private async void ButtonServerStartClick(object sender, EventArgs e)
        {
            if (this.mqttServer != null)
            {
                return;
            }

            var storage = new JsonServerStorage();
            storage.Clear();
            this.mqttServer = new MqttFactory().CreateMqttServer();
            var options = new MqttServerOptions();
            options.DefaultEndpointOptions.Port = int.Parse(ConfigurationManager.AppSettings["mqttServerPort"]);
            options.Storage = storage;
            options.EnablePersistentSessions = true;
            options.ConnectionValidator = new MqttServerConnectionValidatorDelegate(
                c =>
                {
                    if (c.ClientId.Length < 10)
                    {
                        c.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
                        return;
                    }

                    if (c.Username != "username")
                    {
                        c.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                        return;
                    }

                    if (c.Password != "password")
                    {
                        c.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                        return;
                    }

                    c.ReasonCode = MqttConnectReasonCode.Success;
                });

            try
            {
                await this.mqttServer.StartAsync(options);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Occurs", MessageBoxButtons.OK, MessageBoxIcon.Error);
                await this.mqttServer.StopAsync();
                this.mqttServer = null;
            }
        }

        /// <summary>
        /// The method that handles the button click to stop the server.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private async void ButtonServerStopClick(object sender, EventArgs e)
        {
            if (this.mqttServer == null)
            {
                return;
            }

            await this.mqttServer.StopAsync();
            this.mqttServer = null;
        }

        /// <summary>
        /// The method that handles the button click to subscribe to a certain topic.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private async void ButtonSubscribeClick(object sender, EventArgs e)
        {
            var topicFilter = new MqttTopicFilter { Topic = "/#" };
            await this.managedMqttClientSubscriber.SubscribeAsync(topicFilter);
            
            // MessageBox.Show("Topic /# is subscribed", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            lblSubscribed.Text = "Subscribed to topic /#";
            lblSubscribed.Visible = true;
        }

        /// <summary>
        /// The method that handles the button click to start the subscriber.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private async void ButtonSubscriberStartClick(object sender, EventArgs e)
        {
            var mqttFactory = new MqttFactory();

            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = false, IgnoreCertificateChainErrors = true, IgnoreCertificateRevocationErrors = true, AllowUntrustedCertificates = true
            };

            var options = new MqttClientOptions
            {
                ClientId = $"ClientSubscriberGetLock{Guid.NewGuid()}",
                ProtocolVersion = MqttProtocolVersion.V311,
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = ConfigurationManager.AppSettings["mqttServer"], Port = int.Parse(ConfigurationManager.AppSettings["mqttServerPort"]), TlsOptions = tlsOptions
                }
            };

            if (options.ChannelOptions == null)
            {
                throw new InvalidOperationException();
            }

            options.Credentials = new MqttClientCredentials
            {
                Username = ConfigurationManager.AppSettings["mqttServerUsername"],
                Password = Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["mqttServerPassword"])
            };

            options.CleanSession = true;
            options.KeepAlivePeriod = TimeSpan.FromSeconds(5);

            this.managedMqttClientSubscriber = mqttFactory.CreateManagedMqttClient();
            this.managedMqttClientSubscriber.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnSubscriberConnected);
            this.managedMqttClientSubscriber.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnSubscriberDisconnected);
            this.managedMqttClientSubscriber.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(this.OnSubscriberMessageReceived);

            await this.managedMqttClientSubscriber.StartAsync(
                new ManagedMqttClientOptions
                {
                    ClientOptions = options
                });
        }

        /// <summary>
        /// The method that handles the button click to stop the subscriber.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private async void ButtonSubscriberStopClick(object sender, EventArgs e)
        {
            if (this.managedMqttClientSubscriber == null)
            {
                return;
            }

            await this.managedMqttClientSubscriber.StopAsync();
            this.managedMqttClientSubscriber = null;
        }

        /// <summary>
        /// Handles the received application message event.
        /// </summary>
        /// <param name="x">The MQTT application message received event args.</param>
        private void HandleReceivedApplicationMessage(MqttApplicationMessageReceivedEventArgs x)
        {
            var item = $"Timestamp: {DateTime.Now:O} | Topic: {x.ApplicationMessage.Topic} | Payload: {x.ApplicationMessage.ConvertPayloadToString()} | QoS: {x.ApplicationMessage.QualityOfServiceLevel}";
            this.BeginInvoke((MethodInvoker)delegate { this.TextBoxSubscriber.Text = item + Environment.NewLine + this.TextBoxSubscriber.Text; });
        }

        /// <summary>
        /// Handles the received subscriber message event.
        /// </summary>
        /// <param name="x">The MQTT application message received event args.</param>
        private void OnSubscriberMessageReceived(MqttApplicationMessageReceivedEventArgs x)
        {
            var now = DateTime.Now;

            var dateTimeNow = $"{now:O}";

            var item = $"Timestamp: {dateTimeNow} | Topic: {x.ApplicationMessage.Topic} | Payload: {x.ApplicationMessage.ConvertPayloadToString()}";

            var topicPrefix = x.ApplicationMessage.Topic.Split("/")[0];

            if (topicPrefix == "" && x.ApplicationMessage.Topic.Substring(Math.Max(0, x.ApplicationMessage.Topic.Length - 8)).ToUpper() == "/MESSAGE")
            {
                string idCofre = "";

                string[] splicedTopic = x.ApplicationMessage.Topic.Split('/', StringSplitOptions.None);

                if (splicedTopic.Length > 1)
                {
                    idCofre = splicedTopic[1];
                }

                dynamic payload = JsonConvert.DeserializeObject(x.ApplicationMessage.ConvertPayloadToString().Replace("\"$", "\"R"));

                string infoId = payload.INFO.ID;
                string infoIp = payload.INFO.IP;
                string infoMac = payload.INFO.MAC;
                string infoJson = payload.INFO.JSON;
                
                string dataHash = payload.DATA.HASH;
                string dataTmstBegin = payload.DATA.TMST_BEGIN;
                string dataTmstEnd = payload.DATA.TMST_END;
                string dataUser = payload.DATA.USER;
                string dataType = payload.DATA.TYPE;
                string dataCurrencyTotal = payload.DATA.RTOTAL;
                string dataCurrencyB2 = payload.DATA.R2;
                string dataCurrencyB5 = payload.DATA.R5;
                string dataCurrencyB10 = payload.DATA.R10;
                string dataCurrencyB20 = payload.DATA.R20;
                string dataCurrencyB50 = payload.DATA.R50;
                string dataCurrencyB100 = payload.DATA.R100;
                string dataCurrencyB200 = payload.DATA.R200;
                string dataCurrencyBREJ = payload.DATA.RREJ;
                string dataCurrencyEnvelope = payload.DATA.ENV;
                string dataCurrencyEnvelopeTotal = payload.DATA.RENV;
                Nullable<DateTime> dataTmstBeginDateTime = null;
                Nullable<DateTime> dataTmstEndDateTime = null;

                if (dataTmstBegin != null)
                {
                    dataTmstBeginDateTime = UnixTimeStampToDateTime(Convert.ToInt64(dataTmstBegin));
                }

                if (dataTmstEnd != null)
                {
                    dataTmstEndDateTime = UnixTimeStampToDateTime(Convert.ToInt64(dataTmstEnd));
                }

                SqlConnection conn = new SqlConnection(@$"Server={ConfigurationManager.AppSettings["sqlServer"]};Database={ConfigurationManager.AppSettings["sqlServerDatabase"]};User Id={ConfigurationManager.AppSettings["sqlServerUser"]};Password={ConfigurationManager.AppSettings["sqlServerPassword"]};");
                conn.Open();

                SqlCommand command = new SqlCommand("Select id_cofre, info_id, data_hash, trackCreationTime, trackLastWriteTime from message where id_cofre=@idCofre and info_id=@infoId and data_hash=@dataHash", conn);
                command.Parameters.AddWithValue("@idCofre", idCofre);
                command.Parameters.AddWithValue("@infoId", infoId);
                command.Parameters.AddWithValue("@dataHash", dataHash);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        reader.Close();

                        SqlCommand command2 = new SqlCommand("select cod_loja from cofre where id_cofre=@idCofre", conn);
                        command2.Parameters.AddWithValue("@idCofre", idCofre);

                        string codLoja = null;

                        using (SqlDataReader reader2 = command2.ExecuteReader())
                        {
                            if (reader2.Read())
                            {
                                codLoja = reader2["cod_loja"].ToString();
                            }
                            reader2.Close();
                        }
                        
                        string insert_query = "INSERT INTO message (id_cofre, info_id, info_ip, info_mac, info_json, data_hash, data_tmst_begin, data_tmst_begin_datetime, data_tmst_end, data_tmst_end_datetime, data_user, data_type, data_currency_total, data_currency_bill_2, data_currency_bill_5, data_currency_bill_10, data_currency_bill_20, data_currency_bill_50, data_currency_bill_100, data_currency_bill_200, data_currency_bill_rejected, data_currency_envelope, data_currency_envelope_total, cod_loja) VALUES (@idCofre, @infoId, @infoIp, @infoMac, @infoJson, @dataHash, @dataTmstBegin, @dataTmstBeginDateTime, @dataTmstEnd, @dataTmstEndDateTime, @dataUser, @dataType, @dataCurrencyTotal, @dataCurrencyB2, @dataCurrencyB5, @dataCurrencyB10, @dataCurrencyB20, @dataCurrencyB50, @dataCurrencyB100, @dataCurrencyB200, @dataCurrencyBREJ, @dataCurrencyEnvelope, @dataCurrencyEnvelopeTotal, @codLoja)";
                        SqlCommand cmd = new SqlCommand(insert_query, conn);

                        cmd.Parameters.AddWithValue("@idCofre", idCofre == null ? DBNull.Value : idCofre);
                        cmd.Parameters.AddWithValue("@infoId", infoId == null ? DBNull.Value : infoId);
                        cmd.Parameters.AddWithValue("@infoIp", infoIp == null ? DBNull.Value : infoIp);
                        cmd.Parameters.AddWithValue("@infoMac", infoMac == null ? DBNull.Value : infoMac);
                        cmd.Parameters.AddWithValue("@infoJson", infoJson == null ? DBNull.Value : infoJson);

                        cmd.Parameters.AddWithValue("@dataHash", dataHash == null ? DBNull.Value : dataHash);
                        cmd.Parameters.AddWithValue("@dataTmstBegin", dataTmstBegin == null ? DBNull.Value : dataTmstBegin);
                        cmd.Parameters.AddWithValue("@dataTmstBeginDateTime", dataTmstBeginDateTime == null ? DBNull.Value : dataTmstBeginDateTime);
                        cmd.Parameters.AddWithValue("@dataTmstEnd", dataTmstEnd == null ? DBNull.Value : dataTmstEnd);
                        cmd.Parameters.AddWithValue("@dataTmstEndDateTime", dataTmstEndDateTime == null ? DBNull.Value : dataTmstEndDateTime);
                        cmd.Parameters.AddWithValue("@dataUser", dataUser == null ? DBNull.Value : dataUser);
                        cmd.Parameters.AddWithValue("@dataType", dataType == null ? DBNull.Value : dataType);
                        cmd.Parameters.AddWithValue("@dataCurrencyTotal", dataCurrencyTotal == null ? DBNull.Value : dataCurrencyTotal);
                        cmd.Parameters.AddWithValue("@dataCurrencyB2", dataCurrencyB2 == null ? DBNull.Value : dataCurrencyB2);
                        cmd.Parameters.AddWithValue("@dataCurrencyB5", dataCurrencyB5 == null ? DBNull.Value : dataCurrencyB5);
                        cmd.Parameters.AddWithValue("@dataCurrencyB10", dataCurrencyB10 == null ? DBNull.Value : dataCurrencyB10);
                        cmd.Parameters.AddWithValue("@dataCurrencyB20", dataCurrencyB20 == null ? DBNull.Value : dataCurrencyB20);
                        cmd.Parameters.AddWithValue("@dataCurrencyB50", dataCurrencyB50 == null ? DBNull.Value : dataCurrencyB50);
                        cmd.Parameters.AddWithValue("@dataCurrencyB100", dataCurrencyB100 == null ? DBNull.Value : dataCurrencyB100);
                        cmd.Parameters.AddWithValue("@dataCurrencyB200", dataCurrencyB200 == null ? DBNull.Value : dataCurrencyB200);
                        cmd.Parameters.AddWithValue("@dataCurrencyBREJ", dataCurrencyBREJ == null ? DBNull.Value : dataCurrencyBREJ);
                        cmd.Parameters.AddWithValue("@dataCurrencyEnvelope", dataCurrencyEnvelope == null ? DBNull.Value : dataCurrencyEnvelope);
                        cmd.Parameters.AddWithValue("@dataCurrencyEnvelopeTotal", dataCurrencyEnvelopeTotal == null ? DBNull.Value : dataCurrencyEnvelopeTotal);
                        
                        cmd.Parameters.AddWithValue("@codLoja", codLoja == null ? DBNull.Value : codLoja);

                        cmd.ExecuteNonQuery();
                    }
                }

                conn.Close();

                try
                {
                    var ackTopic = $"/{idCofre}/COMMAND";
                    var ackPayload = $@"{{ ""ACK"": {{ ""HASH"":""{dataHash}"", ""TYPE"":{dataType} }} }}";
                    var message = new MqttApplicationMessageBuilder().WithTopic(ackTopic).WithPayload(ackPayload).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).WithRetainFlag(false).Build();

                    if (this.managedMqttClientPublisher != null)
                    {
                        Task.Run(async () => await this.managedMqttClientPublisher.PublishAsync(message));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error Occurs", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else 
            {
                var fileName = topicPrefix + "_" + dateTimeNow.Replace("-", "").Replace(":", "").Replace(".", "") + ".txt";

                var year = now.Year.ToString("0000");
                var month = now.Month.ToString("00");
                var day = now.Day.ToString("00");
                var hour = now.Hour.ToString("00");

                var fullPath = Path.Combine("..", topicPrefix, "fileHandling", "incoming", $"{year}", $"{month}", $"{day}", $"{hour}");
                var fullFileName = Path.Combine("..", topicPrefix, "fileHandling", "incoming", $"{year}", $"{month}", $"{day}", $"{hour}", fileName);

                try
                {
                    var content = "Message;Topic" + Environment.NewLine + x.ApplicationMessage.ConvertPayloadToString() + ";" + x.ApplicationMessage.Topic;
                    DirectoryInfo di = Directory.CreateDirectory(fullPath);
                    File.WriteAllText(fullFileName, content);
                    //lblFileErr.Visible = false;
                }
                catch (Exception ex)
                {
                    this.BeginInvoke((MethodInvoker)delegate { this.lblFileErr.Visible = true; this.lblFileErr.Text = $"Error writing file: {@fullFileName}"; });
                }

                this.BeginInvoke((MethodInvoker)delegate { this.TextBoxSubscriber.Text = item; });
            }
        }

        private string getPathFromTopicPrefix(string topicPrefix)
        {
            return ".";
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
        /// <summary>
        /// The method that handles the text changes in the text box.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void TextBoxPortTextChanged(object sender, EventArgs e)
        {
            // ReSharper disable once StyleCop.SA1126
            if (int.TryParse(this.TextBoxPort.Text, out _))
            {
                this.port = this.TextBoxPort.Text.Trim();
            }
            else
            {
                this.TextBoxPort.Text = this.port;
                this.TextBoxPort.SelectionStart = this.TextBoxPort.Text.Length;
                this.TextBoxPort.SelectionLength = 0;
            }
        }

        /// <summary>
        /// The method that handles the timer events.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            this.BeginInvoke(
                (MethodInvoker)delegate
                {
                    // Server
                    this.TextBoxPort.Enabled = this.mqttServer == null;
                    this.ButtonServerStart.Enabled = this.mqttServer == null;
                    this.ButtonServerStop.Enabled = this.mqttServer != null;

                    // Publisher
                    this.ButtonPublisherStart.Enabled = this.managedMqttClientPublisher == null;
                    this.ButtonPublisherStop.Enabled = this.managedMqttClientPublisher != null;

                    // Subscriber
                    this.ButtonSubscriberStart.Enabled = this.managedMqttClientSubscriber == null;
                    this.ButtonSubscriberStop.Enabled = this.managedMqttClientSubscriber != null;
                });
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ButtonSubscriberStartClick(sender, e);
            ButtonSubscribeClick(sender, e);

            ButtonPublisherStartClick(sender, e);
        }

    }
}

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
                AutoReset = true,
                Enabled = true,
                Interval = Convert.ToInt32(ConfigurationManager.AppSettings["zabbixTimerInterval"])
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
                dynamic payload = JsonConvert.DeserializeObject(x.ApplicationMessage.ConvertPayloadToString().Replace("\"$", "\"R")
                    .Replace("GET-STATUS", "GET_STATUS")
                    .Replace("DEVICE-SENSORS", "DEVICE_SENSORS")
                    .Replace("DEVICE-STATUS", "DEVICE_STATUS")
                    .Replace("BILLMACHINE-STATUS", "BILLMACHINE_STATUS")
                    .Replace("BILLMACHINE-ERROR", "BILLMACHINE_ERROR")
                    .Replace("LEVEL-SENSOR", "LEVEL_SENSOR")
                    .Replace("UPTIME-SEC", "UPTIME_SEC")
                    .Replace("DEV-LOCK", "DEV_LOCK")
                    .Replace("GET-INFO", "GET_INFO")
                    .Replace("FIRM-VERSION", "FIRM_VERSION")
                    .Replace("BILL-MACHINE", "BILL_MACHINE")
                    .Replace("GET-USERLIST", "GET_USERLIST")
                    .Replace("UPDATE-FIRMWARE", "UPDATE_FIRMWARE"));

                if (payload.INFO != null & payload.DATA != null)
                {
                    string idCofre = "";

                    string[] splicedTopic = x.ApplicationMessage.Topic.Split('/', StringSplitOptions.None);

                    if (splicedTopic.Length > 1)
                    {
                        idCofre = splicedTopic[1];
                    }

                    string infoId = payload.INFO.ID;
                    string infoIp = payload.INFO.IP;
                    string infoMac = payload.INFO.MAC;
                    string infoJson = payload.INFO.JSON;

                    string dataHash = payload.DATA.HASH;
                    string dataTmstBegin = payload.DATA.TMST_BEGIN;
                    string dataTmstEnd = payload.DATA.TMST_END;
                    string dataUser = payload.DATA.USER;
                    string dataType = payload.DATA.TYPE;
                    string dataCurrencyTotal = payload.DATA.RTotal;
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
                    string dataCurrencyBill = payload.DATA.TBILL;
                    string dataCurrencyBillTotal = payload.DATA.RTBILL;
                    string dataSensor = payload.DATA.SENSOR;
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

                    string userId = null;
                    string userName = null;
                    string userLastName = null;

                    if (payload.USER != null)
                    {
                        userId = payload.USER.ID;
                        userName = payload.USER.NAME;
                        userLastName = payload.USER.LASTNAME;
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

                            string insert_query = "INSERT INTO message (id_cofre, info_id, info_ip, info_mac, info_json, data_hash, data_tmst_begin, data_tmst_begin_datetime, data_tmst_end, data_tmst_end_datetime, data_user, data_type, data_currency_total, data_currency_bill_2, data_currency_bill_5, data_currency_bill_10, data_currency_bill_20, data_currency_bill_50, data_currency_bill_100, data_currency_bill_200, data_currency_bill_rejected, data_currency_envelope, data_currency_envelope_total, cod_loja, data_currency_bill, data_currency_bill_total, data_sensor, user_id, user_name, user_lastname) VALUES (@idCofre, @infoId, @infoIp, @infoMac, @infoJson, @dataHash, @dataTmstBegin, @dataTmstBeginDateTime, @dataTmstEnd, @dataTmstEndDateTime, @dataUser, @dataType, @dataCurrencyTotal, @dataCurrencyB2, @dataCurrencyB5, @dataCurrencyB10, @dataCurrencyB20, @dataCurrencyB50, @dataCurrencyB100, @dataCurrencyB200, @dataCurrencyBREJ, @dataCurrencyEnvelope, @dataCurrencyEnvelopeTotal, @codLoja, @dataCurrencyBill, @dataCurrencyBillTotal, @dataSensor, @userId, @userName, @userLastName)";
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

                            cmd.Parameters.AddWithValue("@dataCurrencyBill", dataCurrencyBill == null ? DBNull.Value : dataCurrencyBill);
                            cmd.Parameters.AddWithValue("@dataCurrencyBillTotal", dataCurrencyBillTotal == null ? DBNull.Value : dataCurrencyBillTotal);
                            cmd.Parameters.AddWithValue("@dataSensor", dataSensor == null ? DBNull.Value : dataSensor);

                            cmd.Parameters.AddWithValue("@userId", userId == null ? DBNull.Value : userId);
                            cmd.Parameters.AddWithValue("@UserName", userName == null ? DBNull.Value : userName);
                            cmd.Parameters.AddWithValue("@userLastName", userLastName == null ? DBNull.Value : userLastName);

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
                else if ((payload.ACK?.COMMAND.GET_STATUS != null) || (payload.COMMAND?.GET_STATUS != null))
                {
                    var IsAck = payload.ACK?.COMMAND != null;

                    string idCofre = "";

                    string[] splicedTopic = x.ApplicationMessage.Topic.Split('/', StringSplitOptions.None);

                    if (splicedTopic.Length > 1)
                    {
                        idCofre = splicedTopic[1];
                    }

                    string TopicDeviceId = idCofre;
                    long? Destiny = IsAck ? payload.ACK.COMMAND.DESTINY : payload.COMMAND.DESTINY;

                    long? DeviceSensors = IsAck ? payload.ACK.COMMAND.GET_STATUS.DEVICE_SENSORS : payload.COMMAND.GET_STATUS.DEVICE_SENSORS;
                    long? DeviceStatus = IsAck ? payload.ACK.COMMAND.GET_STATUS.DEVICE_STATUS : payload.COMMAND.GET_STATUS.DEVICE_STATUS;

                    if (DeviceStatus == null) DeviceStatus = DeviceSensors;
                    if (DeviceStatus == null) DeviceStatus = 0;

                        long DeviceStatusValue = DeviceStatus == null ? 0 : (long)DeviceStatus;
                    string DeviceStatusBinaryValue = Convert.ToString(DeviceStatusValue, 2);

                    string DeviceStatusBits = DeviceStatusBinaryValue.PadLeft(32, '0');
                    long DeviceStatusBit0 = Convert.ToInt64(DeviceStatusBits.Substring(31, 1));
                    long DeviceStatusBit1 = Convert.ToInt64(DeviceStatusBits.Substring(30, 1));
                    long DeviceStatusBit2 = Convert.ToInt64(DeviceStatusBits.Substring(29, 1));
                    long DeviceStatusBit3 = Convert.ToInt64(DeviceStatusBits.Substring(28, 1));
                    long DeviceStatusBit4 = Convert.ToInt64(DeviceStatusBits.Substring(27, 1));
                    long DeviceStatusBit5 = Convert.ToInt64(DeviceStatusBits.Substring(26, 1));
                    long DeviceStatusBit6 = Convert.ToInt64(DeviceStatusBits.Substring(25, 1));
                    long DeviceStatusBit7 = Convert.ToInt64(DeviceStatusBits.Substring(24, 1));
                    long DeviceStatusBit8 = Convert.ToInt64(DeviceStatusBits.Substring(23, 1));
                    long DeviceStatusBit9 = Convert.ToInt64(DeviceStatusBits.Substring(22, 1));
                    long DeviceStatusBit10 = Convert.ToInt64(DeviceStatusBits.Substring(21, 1));
                    long DeviceStatusBit11 = Convert.ToInt64(DeviceStatusBits.Substring(20, 1));
                    long DeviceStatusBit12 = Convert.ToInt64(DeviceStatusBits.Substring(19, 1));
                    long DeviceStatusBit13 = Convert.ToInt64(DeviceStatusBits.Substring(18, 1));
                    long DeviceStatusBit14 = Convert.ToInt64(DeviceStatusBits.Substring(17, 1));
                    long DeviceStatusBit15 = Convert.ToInt64(DeviceStatusBits.Substring(16, 1));
                    long DeviceStatusBit16 = Convert.ToInt64(DeviceStatusBits.Substring(15, 1));
                    long DeviceStatusBit17 = Convert.ToInt64(DeviceStatusBits.Substring(14, 1));
                    long DeviceStatusBit18 = Convert.ToInt64(DeviceStatusBits.Substring(13, 1));
                    long DeviceStatusBit19 = Convert.ToInt64(DeviceStatusBits.Substring(12, 1));
                    long DeviceStatusBit20 = Convert.ToInt64(DeviceStatusBits.Substring(11, 1));
                    long DeviceStatusBit21 = Convert.ToInt64(DeviceStatusBits.Substring(10, 1));
                    long DeviceStatusBit22 = Convert.ToInt64(DeviceStatusBits.Substring(9, 1));
                    long DeviceStatusBit23 = Convert.ToInt64(DeviceStatusBits.Substring(8, 1));
                    long DeviceStatusBit24 = Convert.ToInt64(DeviceStatusBits.Substring(7, 1));
                    long DeviceStatusBit25 = Convert.ToInt64(DeviceStatusBits.Substring(6, 1));
                    long DeviceStatusBit26 = Convert.ToInt64(DeviceStatusBits.Substring(5, 1));
                    long DeviceStatusBit27 = Convert.ToInt64(DeviceStatusBits.Substring(4, 1));
                    long DeviceStatusBit28 = Convert.ToInt64(DeviceStatusBits.Substring(3, 1));
                    long DeviceStatusBit29 = Convert.ToInt64(DeviceStatusBits.Substring(2, 1));
                    long DeviceStatusBit30 = Convert.ToInt64(DeviceStatusBits.Substring(1, 1));
                    long DeviceStatusBit31 = Convert.ToInt64(DeviceStatusBits.Substring(0, 1));

                    long? BillMachineStatus = IsAck ? payload.ACK.COMMAND.GET_STATUS.BILLMACHINE_STATUS : payload.COMMAND.GET_STATUS.BILLMACHINE_STATUS;

                    long BillMachineStatusValue = BillMachineStatus == null ? 0 : (long)BillMachineStatus;
                    string BillMachineStatusBinaryValue = Convert.ToString(BillMachineStatusValue, 2);

                    string BillMachineStatusBits = BillMachineStatusBinaryValue.PadLeft(32, '0');
                    long BillMachineStatusBit0 = Convert.ToInt64(BillMachineStatusBits.Substring(31, 1));
                    long BillMachineStatusBit1 = Convert.ToInt64(BillMachineStatusBits.Substring(30, 1));
                    long BillMachineStatusBit2 = Convert.ToInt64(BillMachineStatusBits.Substring(29, 1));
                    long BillMachineStatusBit3 = Convert.ToInt64(BillMachineStatusBits.Substring(28, 1));
                    long BillMachineStatusBit4 = Convert.ToInt64(BillMachineStatusBits.Substring(27, 1));
                    long BillMachineStatusBit5 = Convert.ToInt64(BillMachineStatusBits.Substring(26, 1));
                    long BillMachineStatusBit6 = Convert.ToInt64(BillMachineStatusBits.Substring(25, 1));
                    long BillMachineStatusBit7 = Convert.ToInt64(BillMachineStatusBits.Substring(24, 1));
                    long BillMachineStatusBit8 = Convert.ToInt64(BillMachineStatusBits.Substring(23, 1));
                    long BillMachineStatusBit9 = Convert.ToInt64(BillMachineStatusBits.Substring(22, 1));
                    long BillMachineStatusBit10 = Convert.ToInt64(BillMachineStatusBits.Substring(21, 1));
                    long BillMachineStatusBit11 = Convert.ToInt64(BillMachineStatusBits.Substring(20, 1));
                    long BillMachineStatusBit12 = Convert.ToInt64(BillMachineStatusBits.Substring(19, 1));
                    long BillMachineStatusBit13 = Convert.ToInt64(BillMachineStatusBits.Substring(18, 1));
                    long BillMachineStatusBit14 = Convert.ToInt64(BillMachineStatusBits.Substring(17, 1));
                    long BillMachineStatusBit15 = Convert.ToInt64(BillMachineStatusBits.Substring(16, 1));
                    long BillMachineStatusBit16 = Convert.ToInt64(BillMachineStatusBits.Substring(15, 1));
                    long BillMachineStatusBit17 = Convert.ToInt64(BillMachineStatusBits.Substring(14, 1));
                    long BillMachineStatusBit18 = Convert.ToInt64(BillMachineStatusBits.Substring(13, 1));
                    long BillMachineStatusBit19 = Convert.ToInt64(BillMachineStatusBits.Substring(12, 1));
                    long BillMachineStatusBit20 = Convert.ToInt64(BillMachineStatusBits.Substring(11, 1));
                    long BillMachineStatusBit21 = Convert.ToInt64(BillMachineStatusBits.Substring(10, 1));
                    long BillMachineStatusBit22 = Convert.ToInt64(BillMachineStatusBits.Substring(9, 1));
                    long BillMachineStatusBit23 = Convert.ToInt64(BillMachineStatusBits.Substring(8, 1));
                    long BillMachineStatusBit24 = Convert.ToInt64(BillMachineStatusBits.Substring(7, 1));
                    long BillMachineStatusBit25 = Convert.ToInt64(BillMachineStatusBits.Substring(6, 1));
                    long BillMachineStatusBit26 = Convert.ToInt64(BillMachineStatusBits.Substring(5, 1));
                    long BillMachineStatusBit27 = Convert.ToInt64(BillMachineStatusBits.Substring(4, 1));
                    long BillMachineStatusBit28 = Convert.ToInt64(BillMachineStatusBits.Substring(3, 1));
                    long BillMachineStatusBit29 = Convert.ToInt64(BillMachineStatusBits.Substring(2, 1));
                    long BillMachineStatusBit30 = Convert.ToInt64(BillMachineStatusBits.Substring(1, 1));
                    long BillMachineStatusBit31 = Convert.ToInt64(BillMachineStatusBits.Substring(0, 1));

                    long? BillMachineError = IsAck ? payload.ACK.COMMAND.GET_STATUS.BILLMACHINE_ERROR : payload.COMMAND.GET_STATUS.BILLMACHINE_ERROR;

                    long BillMachineErrorValue = BillMachineError == null ? 0 : (long)BillMachineError;
                    string BillMachineErrorBinaryValue = Convert.ToString(BillMachineErrorValue, 2);

                    string BillMachineErrorBits = BillMachineErrorBinaryValue.PadLeft(32, '0');

                    long BillMachineErrorBit0 = Convert.ToInt64(BillMachineErrorBits.Substring(31, 1));
                    long BillMachineErrorBit1 = Convert.ToInt64(BillMachineErrorBits.Substring(30, 1));
                    long BillMachineErrorBit2 = Convert.ToInt64(BillMachineErrorBits.Substring(29, 1));
                    long BillMachineErrorBit3 = Convert.ToInt64(BillMachineErrorBits.Substring(28, 1));
                    long BillMachineErrorBit4 = Convert.ToInt64(BillMachineErrorBits.Substring(27, 1));
                    long BillMachineErrorBit5 = Convert.ToInt64(BillMachineErrorBits.Substring(26, 1));
                    long BillMachineErrorBit6 = Convert.ToInt64(BillMachineErrorBits.Substring(25, 1));
                    long BillMachineErrorBit7 = Convert.ToInt64(BillMachineErrorBits.Substring(24, 1));
                    long BillMachineErrorBit8 = Convert.ToInt64(BillMachineErrorBits.Substring(23, 1));
                    long BillMachineErrorBit9 = Convert.ToInt64(BillMachineErrorBits.Substring(22, 1));
                    long BillMachineErrorBit10 = Convert.ToInt64(BillMachineErrorBits.Substring(21, 1));
                    long BillMachineErrorBit11 = Convert.ToInt64(BillMachineErrorBits.Substring(20, 1));
                    long BillMachineErrorBit12 = Convert.ToInt64(BillMachineErrorBits.Substring(19, 1));
                    long BillMachineErrorBit13 = Convert.ToInt64(BillMachineErrorBits.Substring(18, 1));
                    long BillMachineErrorBit14 = Convert.ToInt64(BillMachineErrorBits.Substring(17, 1));
                    long BillMachineErrorBit15 = Convert.ToInt64(BillMachineErrorBits.Substring(16, 1));
                    long BillMachineErrorBit16 = Convert.ToInt64(BillMachineErrorBits.Substring(15, 1));
                    long BillMachineErrorBit17 = Convert.ToInt64(BillMachineErrorBits.Substring(14, 1));
                    long BillMachineErrorBit18 = Convert.ToInt64(BillMachineErrorBits.Substring(13, 1));
                    long BillMachineErrorBit19 = Convert.ToInt64(BillMachineErrorBits.Substring(12, 1));
                    long BillMachineErrorBit20 = Convert.ToInt64(BillMachineErrorBits.Substring(11, 1));
                    long BillMachineErrorBit21 = Convert.ToInt64(BillMachineErrorBits.Substring(10, 1));
                    long BillMachineErrorBit22 = Convert.ToInt64(BillMachineErrorBits.Substring(9, 1));
                    long BillMachineErrorBit23 = Convert.ToInt64(BillMachineErrorBits.Substring(8, 1));
                    long BillMachineErrorBit24 = Convert.ToInt64(BillMachineErrorBits.Substring(7, 1));
                    long BillMachineErrorBit25 = Convert.ToInt64(BillMachineErrorBits.Substring(6, 1));
                    long BillMachineErrorBit26 = Convert.ToInt64(BillMachineErrorBits.Substring(5, 1));
                    long BillMachineErrorBit27 = Convert.ToInt64(BillMachineErrorBits.Substring(4, 1));
                    long BillMachineErrorBit28 = Convert.ToInt64(BillMachineErrorBits.Substring(3, 1));
                    long BillMachineErrorBit29 = Convert.ToInt64(BillMachineErrorBits.Substring(2, 1));
                    long BillMachineErrorBit30 = Convert.ToInt64(BillMachineErrorBits.Substring(1, 1));
                    long BillMachineErrorBit31 = Convert.ToInt64(BillMachineErrorBits.Substring(0, 1));

                    long? LevelSensor = IsAck ? payload.ACK.COMMAND.GET_STATUS.LEVEL_SENSOR : payload.COMMAND.GET_STATUS.LEVEL_SENSOR;
                    long? UptimeSec = IsAck ? payload.ACK.COMMAND.GET_STATUS.UPTIME_SEC : payload.COMMAND.GET_STATUS.UPTIME_SEC;
                    string Timestamp = IsAck ? payload.ACK.COMMAND.TIMESTAMP : payload.COMMAND.TIMESTAMP;
                    Nullable<DateTime> TimestampDateTime = null;

                    if (Timestamp != null)
                    {
                        TimestampDateTime = UnixTimeStampToDateTime(Convert.ToInt64(Timestamp));
                    }


                    SqlConnection conn = new SqlConnection(@$"Server={ConfigurationManager.AppSettings["sqlServer"]};Database={ConfigurationManager.AppSettings["sqlServerDatabase"]};User Id={ConfigurationManager.AppSettings["sqlServerUser"]};Password={ConfigurationManager.AppSettings["sqlServerPassword"]};");
                    conn.Open();

                    string insert_query = "INSERT INTO message_get_status ( TopicDeviceId, Destiny, DeviceStatus, DeviceStatusBit0, DeviceStatusBit1, DeviceStatusBit2, DeviceStatusBit3, DeviceStatusBit4, DeviceStatusBit5, DeviceStatusBit6, DeviceStatusBit7, DeviceStatusBit8, DeviceStatusBit9, DeviceStatusBit10, DeviceStatusBit11, DeviceStatusBit12, DeviceStatusBit13, DeviceStatusBit14, DeviceStatusBit15, DeviceStatusBit16, DeviceStatusBit17, DeviceStatusBit18, DeviceStatusBit19, DeviceStatusBit20, DeviceStatusBit21, DeviceStatusBit22, DeviceStatusBit23, DeviceStatusBit24, DeviceStatusBit25, DeviceStatusBit26, DeviceStatusBit27, DeviceStatusBit28, DeviceStatusBit29, DeviceStatusBit30, DeviceStatusBit31, BillMachineStatus, BillMachineStatusBit0, BillMachineStatusBit1, BillMachineStatusBit2, BillMachineStatusBit3, BillMachineStatusBit4, BillMachineStatusBit5, BillMachineStatusBit6, BillMachineStatusBit7, BillMachineStatusBit8, BillMachineStatusBit9, BillMachineStatusBit10, BillMachineStatusBit11, BillMachineStatusBit12, BillMachineStatusBit13, BillMachineStatusBit14, BillMachineStatusBit15, BillMachineStatusBit16, BillMachineStatusBit17, BillMachineStatusBit18, BillMachineStatusBit19, BillMachineStatusBit20, BillMachineStatusBit21, BillMachineStatusBit22, BillMachineStatusBit23, BillMachineStatusBit24, BillMachineStatusBit25, BillMachineStatusBit26, BillMachineStatusBit27, BillMachineStatusBit28, BillMachineStatusBit29, BillMachineStatusBit30, BillMachineStatusBit31, BillMachineError, BillMachineErrorBit0, BillMachineErrorBit1, BillMachineErrorBit2, BillMachineErrorBit3, BillMachineErrorBit4, BillMachineErrorBit5, BillMachineErrorBit6, BillMachineErrorBit7, BillMachineErrorBit8, BillMachineErrorBit9, BillMachineErrorBit10, BillMachineErrorBit11, BillMachineErrorBit12, BillMachineErrorBit13, BillMachineErrorBit14, BillMachineErrorBit15, BillMachineErrorBit16, BillMachineErrorBit17, BillMachineErrorBit18, BillMachineErrorBit19, BillMachineErrorBit20, BillMachineErrorBit21, BillMachineErrorBit22, BillMachineErrorBit23, BillMachineErrorBit24, BillMachineErrorBit25, BillMachineErrorBit26, BillMachineErrorBit27, BillMachineErrorBit28, BillMachineErrorBit29, BillMachineErrorBit30, BillMachineErrorBit31, LevelSensor, UptimeSec, Timestamp, TimestampDateTime, IsAck) VALUES ( @TopicDeviceId, @Destiny, @DeviceStatus, @DeviceStatusBit0, @DeviceStatusBit1, @DeviceStatusBit2, @DeviceStatusBit3, @DeviceStatusBit4, @DeviceStatusBit5, @DeviceStatusBit6, @DeviceStatusBit7, @DeviceStatusBit8, @DeviceStatusBit9, @DeviceStatusBit10, @DeviceStatusBit11, @DeviceStatusBit12, @DeviceStatusBit13, @DeviceStatusBit14, @DeviceStatusBit15, @DeviceStatusBit16, @DeviceStatusBit17, @DeviceStatusBit18, @DeviceStatusBit19, @DeviceStatusBit20, @DeviceStatusBit21, @DeviceStatusBit22, @DeviceStatusBit23, @DeviceStatusBit24, @DeviceStatusBit25, @DeviceStatusBit26, @DeviceStatusBit27, @DeviceStatusBit28, @DeviceStatusBit29, @DeviceStatusBit30, @DeviceStatusBit31, @BillMachineStatus, @BillMachineStatusBit0, @BillMachineStatusBit1, @BillMachineStatusBit2, @BillMachineStatusBit3, @BillMachineStatusBit4, @BillMachineStatusBit5, @BillMachineStatusBit6, @BillMachineStatusBit7, @BillMachineStatusBit8, @BillMachineStatusBit9, @BillMachineStatusBit10, @BillMachineStatusBit11, @BillMachineStatusBit12, @BillMachineStatusBit13, @BillMachineStatusBit14, @BillMachineStatusBit15, @BillMachineStatusBit16, @BillMachineStatusBit17, @BillMachineStatusBit18, @BillMachineStatusBit19, @BillMachineStatusBit20, @BillMachineStatusBit21, @BillMachineStatusBit22, @BillMachineStatusBit23, @BillMachineStatusBit24, @BillMachineStatusBit25, @BillMachineStatusBit26, @BillMachineStatusBit27, @BillMachineStatusBit28, @BillMachineStatusBit29, @BillMachineStatusBit30, @BillMachineStatusBit31, @BillMachineError, @BillMachineErrorBit0, @BillMachineErrorBit1, @BillMachineErrorBit2, @BillMachineErrorBit3, @BillMachineErrorBit4, @BillMachineErrorBit5, @BillMachineErrorBit6, @BillMachineErrorBit7, @BillMachineErrorBit8, @BillMachineErrorBit9, @BillMachineErrorBit10, @BillMachineErrorBit11, @BillMachineErrorBit12, @BillMachineErrorBit13, @BillMachineErrorBit14, @BillMachineErrorBit15, @BillMachineErrorBit16, @BillMachineErrorBit17, @BillMachineErrorBit18, @BillMachineErrorBit19, @BillMachineErrorBit20, @BillMachineErrorBit21, @BillMachineErrorBit22, @BillMachineErrorBit23, @BillMachineErrorBit24, @BillMachineErrorBit25, @BillMachineErrorBit26, @BillMachineErrorBit27, @BillMachineErrorBit28, @BillMachineErrorBit29, @BillMachineErrorBit30, @BillMachineErrorBit31, @LevelSensor, @UptimeSec, @Timestamp, @TimestampDateTime, @IsAck)";
                    SqlCommand cmd = new SqlCommand(insert_query, conn);

                    cmd.Parameters.AddWithValue("@TopicDeviceId", TopicDeviceId == null ? DBNull.Value : TopicDeviceId);
                    cmd.Parameters.AddWithValue("@Destiny", Destiny == null ? DBNull.Value : Destiny);
                    cmd.Parameters.AddWithValue("@DeviceStatus", DeviceStatus == null ? DBNull.Value : DeviceStatus);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit0", DeviceStatusBit0 == null ? DBNull.Value : DeviceStatusBit0);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit1", DeviceStatusBit1 == null ? DBNull.Value : DeviceStatusBit1);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit2", DeviceStatusBit2 == null ? DBNull.Value : DeviceStatusBit2);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit3", DeviceStatusBit3 == null ? DBNull.Value : DeviceStatusBit3);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit4", DeviceStatusBit4 == null ? DBNull.Value : DeviceStatusBit4);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit5", DeviceStatusBit5 == null ? DBNull.Value : DeviceStatusBit5);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit6", DeviceStatusBit6 == null ? DBNull.Value : DeviceStatusBit6);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit7", DeviceStatusBit7 == null ? DBNull.Value : DeviceStatusBit7);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit8", DeviceStatusBit8 == null ? DBNull.Value : DeviceStatusBit8);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit9", DeviceStatusBit9 == null ? DBNull.Value : DeviceStatusBit9);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit10", DeviceStatusBit10 == null ? DBNull.Value : DeviceStatusBit10);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit11", DeviceStatusBit11 == null ? DBNull.Value : DeviceStatusBit11);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit12", DeviceStatusBit12 == null ? DBNull.Value : DeviceStatusBit12);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit13", DeviceStatusBit13 == null ? DBNull.Value : DeviceStatusBit13);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit14", DeviceStatusBit14 == null ? DBNull.Value : DeviceStatusBit14);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit15", DeviceStatusBit15 == null ? DBNull.Value : DeviceStatusBit15);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit16", DeviceStatusBit16 == null ? DBNull.Value : DeviceStatusBit16);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit17", DeviceStatusBit17 == null ? DBNull.Value : DeviceStatusBit17);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit18", DeviceStatusBit18 == null ? DBNull.Value : DeviceStatusBit18);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit19", DeviceStatusBit19 == null ? DBNull.Value : DeviceStatusBit19);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit20", DeviceStatusBit20 == null ? DBNull.Value : DeviceStatusBit20);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit21", DeviceStatusBit21 == null ? DBNull.Value : DeviceStatusBit21);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit22", DeviceStatusBit22 == null ? DBNull.Value : DeviceStatusBit22);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit23", DeviceStatusBit23 == null ? DBNull.Value : DeviceStatusBit23);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit24", DeviceStatusBit24 == null ? DBNull.Value : DeviceStatusBit24);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit25", DeviceStatusBit25 == null ? DBNull.Value : DeviceStatusBit25);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit26", DeviceStatusBit26 == null ? DBNull.Value : DeviceStatusBit26);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit27", DeviceStatusBit27 == null ? DBNull.Value : DeviceStatusBit27);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit28", DeviceStatusBit28 == null ? DBNull.Value : DeviceStatusBit28);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit29", DeviceStatusBit29 == null ? DBNull.Value : DeviceStatusBit29);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit30", DeviceStatusBit30 == null ? DBNull.Value : DeviceStatusBit30);
                    cmd.Parameters.AddWithValue("@DeviceStatusBit31", DeviceStatusBit31 == null ? DBNull.Value : DeviceStatusBit31);
                    cmd.Parameters.AddWithValue("@BillMachineStatus", BillMachineStatus == null ? DBNull.Value : BillMachineStatus);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit0", BillMachineStatusBit0 == null ? DBNull.Value : BillMachineStatusBit0);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit1", BillMachineStatusBit1 == null ? DBNull.Value : BillMachineStatusBit1);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit2", BillMachineStatusBit2 == null ? DBNull.Value : BillMachineStatusBit2);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit3", BillMachineStatusBit3 == null ? DBNull.Value : BillMachineStatusBit3);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit4", BillMachineStatusBit4 == null ? DBNull.Value : BillMachineStatusBit4);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit5", BillMachineStatusBit5 == null ? DBNull.Value : BillMachineStatusBit5);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit6", BillMachineStatusBit6 == null ? DBNull.Value : BillMachineStatusBit6);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit7", BillMachineStatusBit7 == null ? DBNull.Value : BillMachineStatusBit7);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit8", BillMachineStatusBit8 == null ? DBNull.Value : BillMachineStatusBit8);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit9", BillMachineStatusBit9 == null ? DBNull.Value : BillMachineStatusBit9);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit10", BillMachineStatusBit10 == null ? DBNull.Value : BillMachineStatusBit10);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit11", BillMachineStatusBit11 == null ? DBNull.Value : BillMachineStatusBit11);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit12", BillMachineStatusBit12 == null ? DBNull.Value : BillMachineStatusBit12);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit13", BillMachineStatusBit13 == null ? DBNull.Value : BillMachineStatusBit13);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit14", BillMachineStatusBit14 == null ? DBNull.Value : BillMachineStatusBit14);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit15", BillMachineStatusBit15 == null ? DBNull.Value : BillMachineStatusBit15);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit16", BillMachineStatusBit16 == null ? DBNull.Value : BillMachineStatusBit16);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit17", BillMachineStatusBit17 == null ? DBNull.Value : BillMachineStatusBit17);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit18", BillMachineStatusBit18 == null ? DBNull.Value : BillMachineStatusBit18);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit19", BillMachineStatusBit19 == null ? DBNull.Value : BillMachineStatusBit19);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit20", BillMachineStatusBit20 == null ? DBNull.Value : BillMachineStatusBit20);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit21", BillMachineStatusBit21 == null ? DBNull.Value : BillMachineStatusBit21);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit22", BillMachineStatusBit22 == null ? DBNull.Value : BillMachineStatusBit22);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit23", BillMachineStatusBit23 == null ? DBNull.Value : BillMachineStatusBit23);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit24", BillMachineStatusBit24 == null ? DBNull.Value : BillMachineStatusBit24);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit25", BillMachineStatusBit25 == null ? DBNull.Value : BillMachineStatusBit25);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit26", BillMachineStatusBit26 == null ? DBNull.Value : BillMachineStatusBit26);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit27", BillMachineStatusBit27 == null ? DBNull.Value : BillMachineStatusBit27);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit28", BillMachineStatusBit28 == null ? DBNull.Value : BillMachineStatusBit28);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit29", BillMachineStatusBit29 == null ? DBNull.Value : BillMachineStatusBit29);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit30", BillMachineStatusBit30 == null ? DBNull.Value : BillMachineStatusBit30);
                    cmd.Parameters.AddWithValue("@BillMachineStatusBit31", BillMachineStatusBit31 == null ? DBNull.Value : BillMachineStatusBit31);
                    cmd.Parameters.AddWithValue("@BillMachineError", BillMachineError == null ? DBNull.Value : BillMachineError);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit0", BillMachineErrorBit0 == null ? DBNull.Value : BillMachineErrorBit0);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit1", BillMachineErrorBit1 == null ? DBNull.Value : BillMachineErrorBit1);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit2", BillMachineErrorBit2 == null ? DBNull.Value : BillMachineErrorBit2);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit3", BillMachineErrorBit3 == null ? DBNull.Value : BillMachineErrorBit3);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit4", BillMachineErrorBit4 == null ? DBNull.Value : BillMachineErrorBit4);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit5", BillMachineErrorBit5 == null ? DBNull.Value : BillMachineErrorBit5);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit6", BillMachineErrorBit6 == null ? DBNull.Value : BillMachineErrorBit6);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit7", BillMachineErrorBit7 == null ? DBNull.Value : BillMachineErrorBit7);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit8", BillMachineErrorBit8 == null ? DBNull.Value : BillMachineErrorBit8);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit9", BillMachineErrorBit9 == null ? DBNull.Value : BillMachineErrorBit9);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit10", BillMachineErrorBit10 == null ? DBNull.Value : BillMachineErrorBit10);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit11", BillMachineErrorBit11 == null ? DBNull.Value : BillMachineErrorBit11);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit12", BillMachineErrorBit12 == null ? DBNull.Value : BillMachineErrorBit12);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit13", BillMachineErrorBit13 == null ? DBNull.Value : BillMachineErrorBit13);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit14", BillMachineErrorBit14 == null ? DBNull.Value : BillMachineErrorBit14);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit15", BillMachineErrorBit15 == null ? DBNull.Value : BillMachineErrorBit15);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit16", BillMachineErrorBit16 == null ? DBNull.Value : BillMachineErrorBit16);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit17", BillMachineErrorBit17 == null ? DBNull.Value : BillMachineErrorBit17);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit18", BillMachineErrorBit18 == null ? DBNull.Value : BillMachineErrorBit18);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit19", BillMachineErrorBit19 == null ? DBNull.Value : BillMachineErrorBit19);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit20", BillMachineErrorBit20 == null ? DBNull.Value : BillMachineErrorBit20);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit21", BillMachineErrorBit21 == null ? DBNull.Value : BillMachineErrorBit21);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit22", BillMachineErrorBit22 == null ? DBNull.Value : BillMachineErrorBit22);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit23", BillMachineErrorBit23 == null ? DBNull.Value : BillMachineErrorBit23);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit24", BillMachineErrorBit24 == null ? DBNull.Value : BillMachineErrorBit24);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit25", BillMachineErrorBit25 == null ? DBNull.Value : BillMachineErrorBit25);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit26", BillMachineErrorBit26 == null ? DBNull.Value : BillMachineErrorBit26);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit27", BillMachineErrorBit27 == null ? DBNull.Value : BillMachineErrorBit27);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit28", BillMachineErrorBit28 == null ? DBNull.Value : BillMachineErrorBit28);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit29", BillMachineErrorBit29 == null ? DBNull.Value : BillMachineErrorBit29);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit30", BillMachineErrorBit30 == null ? DBNull.Value : BillMachineErrorBit30);
                    cmd.Parameters.AddWithValue("@BillMachineErrorBit31", BillMachineErrorBit31 == null ? DBNull.Value : BillMachineErrorBit31);
                    cmd.Parameters.AddWithValue("@LevelSensor", LevelSensor == null ? DBNull.Value : LevelSensor);
                    cmd.Parameters.AddWithValue("@UptimeSec", UptimeSec == null ? DBNull.Value : UptimeSec);
                    cmd.Parameters.AddWithValue("@Timestamp", Timestamp == null ? DBNull.Value : Timestamp);
                    cmd.Parameters.AddWithValue("@TimestampDateTime", TimestampDateTime == null ? DBNull.Value : TimestampDateTime);
                    cmd.Parameters.AddWithValue("@IsAck", IsAck);

                    cmd.ExecuteNonQuery();

                    conn.Close();

                    try
                    {
                        if(!IsAck)
                        {
                            var ackTopic = $"/{idCofre}/COMMAND";
                            var ackPayload = $@"{{ ""ACK"": {{ ""COMMAND"": {{ ""DESTINY"": {idCofre}, ""CMD"": ""GET-STATUS"" }} }} }}";
                            var message = new MqttApplicationMessageBuilder().WithTopic(ackTopic).WithPayload(ackPayload).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).WithRetainFlag(false).Build();

                            if (this.managedMqttClientPublisher != null)
                            {
                                Task.Run(async () => await this.managedMqttClientPublisher.PublishAsync(message));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error Occurs", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
                else if ((payload.ACK?.COMMAND["DEV_LOCK"] != null || (payload.COMMAND?["DEV_LOCK"] != null)))
                {
                    var IsAck = payload.ACK?.COMMAND != null;

                    string idCofre = "";

                    string[] splicedTopic = x.ApplicationMessage.Topic.Split('/', StringSplitOptions.None);

                    if (splicedTopic.Length > 1)
                    {
                        idCofre = splicedTopic[1];
                    }

                    string TopicDeviceId = idCofre;
                    long? Destiny = IsAck ? payload.ACK.COMMAND.DESTINY : payload.COMMAND.DESTINY;

                    string DevLock = IsAck ? payload.ACK.COMMAND.DEV_LOCK : payload.COMMAND.DEV_LOCK;

                    bool DeviceLockValue = DevLock == null ? false : Convert.ToBoolean(DevLock);

                    string Timestamp = IsAck ? payload.ACK.COMMAND.TIMESTAMP : payload.COMMAND.TIMESTAMP;
                    Nullable<DateTime> TimestampDateTime = null;

                    if (Timestamp != null)
                    {
                        TimestampDateTime = UnixTimeStampToDateTime(Convert.ToInt64(Timestamp));
                    }

                    SqlConnection conn = new SqlConnection(@$"Server={ConfigurationManager.AppSettings["sqlServer"]};Database={ConfigurationManager.AppSettings["sqlServerDatabase"]};User Id={ConfigurationManager.AppSettings["sqlServerUser"]};Password={ConfigurationManager.AppSettings["sqlServerPassword"]};");
                    conn.Open();

                    string insert_query = "INSERT INTO message_dev_lock ( TopicDeviceId, Destiny, DevLock, Timestamp, TimestampDateTime, IsAck) VALUES ( @TopicDeviceId, @Destiny, @DevLock, @Timestamp, @TimestampDateTime, @IsAck)";
                    SqlCommand cmd = new SqlCommand(insert_query, conn);

                    cmd.Parameters.AddWithValue("@TopicDeviceId", TopicDeviceId == null ? DBNull.Value : TopicDeviceId);
                    cmd.Parameters.AddWithValue("@Destiny", Destiny == null ? DBNull.Value : Destiny);
                    cmd.Parameters.AddWithValue("@DevLock", DevLock);
                    cmd.Parameters.AddWithValue("@Timestamp", Timestamp == null ? DBNull.Value : Timestamp);
                    cmd.Parameters.AddWithValue("@TimestampDateTime", TimestampDateTime == null ? DBNull.Value : TimestampDateTime);
                    cmd.Parameters.AddWithValue("@IsAck", IsAck);

                    cmd.ExecuteNonQuery();

                    conn.Close();

                    try
                    {
                        if (!IsAck)
                        {
                            var ackTopic = $"/{idCofre}/COMMAND";
                            var ackPayload = $@"{{ ""ACK"": {{ ""COMMAND"": {{ ""DESTINY"": {idCofre}, ""CMD"": ""DEV-LOCK"" }} }} }}";
                            var message = new MqttApplicationMessageBuilder().WithTopic(ackTopic).WithPayload(ackPayload).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).WithRetainFlag(false).Build();

                            if (this.managedMqttClientPublisher != null)
                            {
                                Task.Run(async () => await this.managedMqttClientPublisher.PublishAsync(message));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error Occurs", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if ((payload.ACK?.COMMAND.GET_INFO != null) || (payload.COMMAND?.GET_INFO != null))
                {
                    var IsAck = payload.ACK?.COMMAND != null;

                    string idCofre = "";

                    string[] splicedTopic = x.ApplicationMessage.Topic.Split('/', StringSplitOptions.None);

                    if (splicedTopic.Length > 1)
                    {
                        idCofre = splicedTopic[1];
                    }

                    string TopicDeviceId = idCofre;
                    long? Destiny = IsAck ? payload.ACK.COMMAND.DESTINY : payload.COMMAND.DESTINY;

                    string CompanyName = IsAck ? payload.ACK.COMMAND.GET_INFO.COMPANY.NAME : payload.COMMAND.GET_INFO.COMPANY.NAME;
                    string CompanyCNPJ = IsAck ? payload.ACK.COMMAND.GET_INFO.COMPANY.CNPJ : payload.COMMAND.GET_INFO.COMPANY.CNPJ;

                    string DeviceSN = IsAck ? payload.ACK.COMMAND.GET_INFO.DEVICE.SN : payload.COMMAND.GET_INFO.DEVICE.SN;
                    string DeviceFirmVersion = IsAck ? payload.ACK.COMMAND.GET_INFO.DEVICE.FIRM_VERSION : payload.COMMAND.GET_INFO.DEVICE.FIRM_VERSION;
                    bool DeviceBlocked = IsAck ? payload.ACK.COMMAND.GET_INFO.DEVICE.BLOCKED : payload.COMMAND.GET_INFO.DEVICE.BLOCKED;

                    string BillMachineType = IsAck ? payload.ACK.COMMAND.GET_INFO.BILL_MACHINE.TYPE : payload.COMMAND.GET_INFO.BILL_MACHINE.TYPE;
                    string BillMachineSN = IsAck ? payload.ACK.COMMAND.GET_INFO.BILL_MACHINE.SN : payload.COMMAND.GET_INFO.BILL_MACHINE.SN;

                    string Timestamp = IsAck ? payload.ACK.COMMAND.TIMESTAMP : payload.COMMAND.TIMESTAMP;
                    Nullable<DateTime> TimestampDateTime = null;

                    if (Timestamp != null)
                    {
                        TimestampDateTime = UnixTimeStampToDateTime(Convert.ToInt64(Timestamp));
                    }

                    SqlConnection conn = new SqlConnection(@$"Server={ConfigurationManager.AppSettings["sqlServer"]};Database={ConfigurationManager.AppSettings["sqlServerDatabase"]};User Id={ConfigurationManager.AppSettings["sqlServerUser"]};Password={ConfigurationManager.AppSettings["sqlServerPassword"]};");
                    conn.Open();

                    string insert_query = "INSERT INTO message_get_info (TopicDeviceId, Destiny, CompanyName, CompanyCNPJ, DeviceSN, DeviceFirmVersion, DeviceBlocked, BillMachineType, BillMachineSN, Timestamp, TimestampDatetime, IsAck) VALUES(@TopicDeviceId, @Destiny, @CompanyName, @CompanyCNPJ, @DeviceSN, @DeviceFirmVersion, @DeviceBlocked, @BillMachineType, @BillMachineSN, @Timestamp, @TimestampDatetime, @IsAck)";
                    SqlCommand cmd = new SqlCommand(insert_query, conn);

                    cmd.Parameters.AddWithValue("@TopicDeviceId", TopicDeviceId == null ? DBNull.Value : TopicDeviceId);
                    cmd.Parameters.AddWithValue("@Destiny", Destiny == null ? DBNull.Value : Destiny);
                    cmd.Parameters.AddWithValue("@CompanyName", CompanyName == null ? DBNull.Value : CompanyName);
                    cmd.Parameters.AddWithValue("@CompanyCNPJ", CompanyCNPJ == null ? DBNull.Value : CompanyCNPJ);
                    cmd.Parameters.AddWithValue("@DeviceSN", DeviceSN == null ? DBNull.Value : DeviceSN);
                    cmd.Parameters.AddWithValue("@DeviceFirmVersion", DeviceFirmVersion == null ? DBNull.Value : DeviceFirmVersion);
                    cmd.Parameters.AddWithValue("@DeviceBlocked", DeviceBlocked);
                    cmd.Parameters.AddWithValue("@BillMachineType", BillMachineType == null ? DBNull.Value : BillMachineType);
                    cmd.Parameters.AddWithValue("@BillMachineSN", BillMachineSN == null ? DBNull.Value : BillMachineSN);

                    cmd.Parameters.AddWithValue("@Timestamp", Timestamp == null ? DBNull.Value : Timestamp);
                    cmd.Parameters.AddWithValue("@TimestampDateTime", TimestampDateTime == null ? DBNull.Value : TimestampDateTime);
                    cmd.Parameters.AddWithValue("@IsAck", IsAck);

                    cmd.ExecuteNonQuery();

                    conn.Close();

                    try
                    {
                        if (!IsAck)
                        {
                            var ackTopic = $"/{idCofre}/COMMAND";
                            var ackPayload = $@"{{ ""ACK"": {{ ""COMMAND"": {{ ""DESTINY"": {idCofre}, ""CMD"": ""GET-INFO"" }} }} }}";
                            var message = new MqttApplicationMessageBuilder().WithTopic(ackTopic).WithPayload(ackPayload).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).WithRetainFlag(false).Build();

                            if (this.managedMqttClientPublisher != null)
                            {
                                Task.Run(async () => await this.managedMqttClientPublisher.PublishAsync(message));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error Occurs", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
                else if ((payload.ACK?.COMMAND.GET_USERLIST != null) || (payload.COMMAND?.GET_USERLIST != null))
                {
                    var IsAck = payload.ACK?.COMMAND != null;

                    string idCofre = "";

                    string[] splicedTopic = x.ApplicationMessage.Topic.Split('/', StringSplitOptions.None);

                    if (splicedTopic.Length > 1)
                    {
                        idCofre = splicedTopic[1];
                    }

                    string TopicDeviceId = idCofre;
                    long? Destiny = IsAck ? payload.ACK.COMMAND.DESTINY : payload.COMMAND.DESTINY;

                    string Total = IsAck ? payload.ACK.COMMAND.GET_USERLIST.TOTAL : payload.COMMAND.GET_USERLIST.TOTAL;
                    var Users = IsAck ? payload.ACK.COMMAND.GET_USERLIST.USERS : payload.COMMAND.GET_USERLIST.USERS;


                    string Timestamp = IsAck ? payload.ACK.COMMAND.TIMESTAMP : payload.COMMAND.TIMESTAMP;
                    Nullable<DateTime> TimestampDateTime = null;

                    if (Timestamp != null)
                    {
                        TimestampDateTime = UnixTimeStampToDateTime(Convert.ToInt64(Timestamp));
                    }

                    SqlConnection conn = new SqlConnection(@$"Server={ConfigurationManager.AppSettings["sqlServer"]};Database={ConfigurationManager.AppSettings["sqlServerDatabase"]};User Id={ConfigurationManager.AppSettings["sqlServerUser"]};Password={ConfigurationManager.AppSettings["sqlServerPassword"]};");
                    conn.Open();

                    foreach (var user in Users)
                    {
                        var UserIndex = Convert.ToString(user.INDEX);
                        var UserId = Convert.ToString(user.ID);
                        var UserEnable = Convert.ToBoolean(user.ENABLE);
                        var UserAccessLevel = Convert.ToString(user.ACCESSLEVEL);
                        var UserName = Convert.ToString(user.NAME);
                        var UserLastName = Convert.ToString(user.LASTNAME);
                        var UserPasswd = Convert.ToString(user.PASSWD);

                        string insert_query = "INSERT INTO message_get_userlist (TopicDeviceId, Destiny, Total, UserIndex, UserId, UserEnable, UserAccessLevel, UserName, UserLastName, UserPasswd, Timestamp, TimestampDatetime, IsAck) VALUES(@TopicDeviceId, @Destiny, @Total, @UserIndex, @UserId, @UserEnable, @UserAccessLevel, @UserName, @UserLastName, @UserPasswd, @Timestamp, @TimestampDatetime, @IsAck)";
                        SqlCommand cmd = new SqlCommand(insert_query, conn);

                        cmd.Parameters.AddWithValue("@TopicDeviceId", TopicDeviceId == null ? DBNull.Value : TopicDeviceId);
                        cmd.Parameters.AddWithValue("@Destiny", Destiny == null ? DBNull.Value : Destiny);
                        cmd.Parameters.AddWithValue("@Total", Destiny == null ? DBNull.Value : Total);
                        cmd.Parameters.AddWithValue("@UserIndex", Destiny == null ? DBNull.Value : UserIndex);
                        cmd.Parameters.AddWithValue("@UserId", Destiny == null ? DBNull.Value : UserId);
                        cmd.Parameters.AddWithValue("@UserEnable", UserEnable);
                        cmd.Parameters.AddWithValue("@UserAccessLevel", Destiny == null ? DBNull.Value : UserAccessLevel);
                        cmd.Parameters.AddWithValue("@UserName", Destiny == null ? DBNull.Value : UserName);
                        cmd.Parameters.AddWithValue("@UserLastName", Destiny == null ? DBNull.Value : UserLastName);
                        cmd.Parameters.AddWithValue("@UserPasswd", Destiny == null ? DBNull.Value : UserPasswd);

                        cmd.Parameters.AddWithValue("@Timestamp", Timestamp == null ? DBNull.Value : Timestamp);
                        cmd.Parameters.AddWithValue("@TimestampDateTime", TimestampDateTime == null ? DBNull.Value : TimestampDateTime);
                        cmd.Parameters.AddWithValue("@IsAck", IsAck);

                        cmd.ExecuteNonQuery();
                    }


                    conn.Close();

                    try
                    {
                        if (!IsAck)
                        {
                            var ackTopic = $"/{idCofre}/COMMAND";
                            var ackPayload = $@"{{ ""ACK"": {{ ""COMMAND"": {{ ""DESTINY"": {idCofre}, ""CMD"": ""GET-USERLIST"" }} }} }}";
                            var message = new MqttApplicationMessageBuilder().WithTopic(ackTopic).WithPayload(ackPayload).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).WithRetainFlag(false).Build();

                            if (this.managedMqttClientPublisher != null)
                            {
                                Task.Run(async () => await this.managedMqttClientPublisher.PublishAsync(message));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error Occurs", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
                else if ((payload.ACK?.COMMAND.UPDATE_FIRMWARE != null || (payload.COMMAND?.UPDATE_FIRMWARE != null)))
                {
                    var IsAck = payload.ACK?.COMMAND != null;

                    string idCofre = "";

                    string[] splicedTopic = x.ApplicationMessage.Topic.Split('/', StringSplitOptions.None);

                    if (splicedTopic.Length > 1)
                    {
                        idCofre = splicedTopic[1];
                    }

                    string TopicDeviceId = idCofre;
                    long? Destiny = IsAck ? payload.ACK.COMMAND.DESTINY : payload.COMMAND.DESTINY;

                    string UpdateFirmware = IsAck ? payload.ACK.COMMAND.UPDATE_FIRMWARE : payload.COMMAND.UPDATE_FIRMWARE;

                    string Timestamp = IsAck ? payload.ACK.COMMAND.TIMESTAMP : payload.COMMAND.TIMESTAMP;
                    Nullable<DateTime> TimestampDateTime = null;

                    if (Timestamp != null)
                    {
                        TimestampDateTime = UnixTimeStampToDateTime(Convert.ToInt64(Timestamp));
                    }

                    SqlConnection conn = new SqlConnection(@$"Server={ConfigurationManager.AppSettings["sqlServer"]};Database={ConfigurationManager.AppSettings["sqlServerDatabase"]};User Id={ConfigurationManager.AppSettings["sqlServerUser"]};Password={ConfigurationManager.AppSettings["sqlServerPassword"]};");
                    conn.Open();

                    string insert_query = "INSERT INTO message_update_firmware ( TopicDeviceId, Destiny, UpdateFirmware, Timestamp, TimestampDateTime, IsAck) VALUES ( @TopicDeviceId, @Destiny, @UpdateFirmware, @Timestamp, @TimestampDateTime, @IsAck)";
                    SqlCommand cmd = new SqlCommand(insert_query, conn);

                    cmd.Parameters.AddWithValue("@TopicDeviceId", TopicDeviceId == null ? DBNull.Value : TopicDeviceId);
                    cmd.Parameters.AddWithValue("@Destiny", Destiny == null ? DBNull.Value : Destiny);
                    cmd.Parameters.AddWithValue("@UpdateFirmware", UpdateFirmware);
                    cmd.Parameters.AddWithValue("@Timestamp", Timestamp == null ? DBNull.Value : Timestamp);
                    cmd.Parameters.AddWithValue("@TimestampDateTime", TimestampDateTime == null ? DBNull.Value : TimestampDateTime);
                    cmd.Parameters.AddWithValue("@IsAck", IsAck);

                    cmd.ExecuteNonQuery();

                    conn.Close();

                    try
                    {
                        if (!IsAck)
                        {
                            var ackTopic = $"/{idCofre}/COMMAND";
                            var ackPayload = $@"{{ ""ACK"": {{ ""COMMAND"": {{ ""DESTINY"": {idCofre}, ""CMD"": ""UPDATE-FIRMWARE"" }} }} }}";
                            var message = new MqttApplicationMessageBuilder().WithTopic(ackTopic).WithPayload(ackPayload).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).WithRetainFlag(false).Build();

                            if (this.managedMqttClientPublisher != null)
                            {
                                Task.Run(async () => await this.managedMqttClientPublisher.PublishAsync(message));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error Occurs", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
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

                    if (ConfigurationManager.AppSettings["zabbixTimerOnOff"] == "ON")
                    {
                        System.Diagnostics.Process process = new System.Diagnostics.Process();
                        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                        //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        startInfo.FileName = $"{ConfigurationManager.AppSettings["zabbixSenderPath"]}zabbix_sender.exe";
                        startInfo.Arguments = $"-z {ConfigurationManager.AppSettings["zabbixServer"]} -s {ConfigurationManager.AppSettings["zabbixHostTimer"]} -k {ConfigurationManager.AppSettings["zabbixItemTimer"]} -o 1";

                        startInfo.UseShellExecute = false;
                        startInfo.RedirectStandardOutput = true;
                        startInfo.CreateNoWindow = true;
                        process.StartInfo = startInfo;
                        //lblSubscribed.Text = $"{DateTime.Now:G} {startInfo.FileName} {startInfo.Arguments}";

                        process.Start();
                        lblZabbixPing.Text = $"{DateTime.Now:G} {startInfo.Arguments}";
                        lblZabbix.Text = process.StandardOutput.ReadToEnd();
                    }

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

﻿// --------------------------------------------------------------------------------------------------------------------
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
                dynamic payload = JsonConvert.DeserializeObject(x.ApplicationMessage.ConvertPayloadToString().Replace("\"$", "\"R")
                    .Replace("GET-STATUS", "GET_STATUS")
                    .Replace("DEVICE-SENSORS", "DEVICE_SENSORS")
                    .Replace("BILLMACHINE-STATUS", "BILLMACHINE_STATUS")
                    .Replace("BILLMACHINE-ERROR", "BILLMACHINE_ERROR")
                    .Replace("LEVEL-SENSOR", "LEVEL_SENSOR")
                    .Replace("UPTIME-SEC", "UPTIME_SEC"));

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
                else if (payload.ACK.COMMAND.GET_STATUS != null)
                {
                    string idCofre = "";

                    string[] splicedTopic = x.ApplicationMessage.Topic.Split('/', StringSplitOptions.None);

                    if (splicedTopic.Length > 1)
                    {
                        idCofre = splicedTopic[1];
                    }

                    int TopicDeviceId = int.TryParse(idCofre, out TopicDeviceId) ? TopicDeviceId : 0; ;
                    long? Destiny = payload.ACK.COMMAND.DESTINY;

                    long? DeviceSensors = payload.ACK.COMMAND.GET_STATUS.DEVICE_SENSORS;

                    long DeviceSensorsValue = DeviceSensors == null ? 0 : (long)DeviceSensors;
                    string DeviceSensorsBinaryValue = Convert.ToString(DeviceSensorsValue, 2);

                    string DeviceSensorsBits = DeviceSensorsBinaryValue.PadLeft(32, '0');
                    long DeviceSensorsBit0 = Convert.ToInt64(DeviceSensorsBits.Substring(31, 1));
                    long DeviceSensorsBit1 = Convert.ToInt64(DeviceSensorsBits.Substring(30, 1));
                    long DeviceSensorsBit2 = Convert.ToInt64(DeviceSensorsBits.Substring(29, 1));
                    long DeviceSensorsBit3 = Convert.ToInt64(DeviceSensorsBits.Substring(28, 1));
                    long DeviceSensorsBit4 = Convert.ToInt64(DeviceSensorsBits.Substring(27, 1));
                    long DeviceSensorsBit5 = Convert.ToInt64(DeviceSensorsBits.Substring(26, 1));
                    long DeviceSensorsBit6 = Convert.ToInt64(DeviceSensorsBits.Substring(25, 1));
                    long DeviceSensorsBit7 = Convert.ToInt64(DeviceSensorsBits.Substring(24, 1));
                    long DeviceSensorsBit8 = Convert.ToInt64(DeviceSensorsBits.Substring(23, 1));
                    long DeviceSensorsBit9 = Convert.ToInt64(DeviceSensorsBits.Substring(22, 1));
                    long DeviceSensorsBit10 = Convert.ToInt64(DeviceSensorsBits.Substring(21, 1));
                    long DeviceSensorsBit11 = Convert.ToInt64(DeviceSensorsBits.Substring(20, 1));
                    long DeviceSensorsBit12 = Convert.ToInt64(DeviceSensorsBits.Substring(19, 1));
                    long DeviceSensorsBit13 = Convert.ToInt64(DeviceSensorsBits.Substring(18, 1));
                    long DeviceSensorsBit14 = Convert.ToInt64(DeviceSensorsBits.Substring(17, 1));
                    long DeviceSensorsBit15 = Convert.ToInt64(DeviceSensorsBits.Substring(16, 1));
                    long DeviceSensorsBit16 = Convert.ToInt64(DeviceSensorsBits.Substring(15, 1));
                    long DeviceSensorsBit17 = Convert.ToInt64(DeviceSensorsBits.Substring(14, 1));
                    long DeviceSensorsBit18 = Convert.ToInt64(DeviceSensorsBits.Substring(13, 1));
                    long DeviceSensorsBit19 = Convert.ToInt64(DeviceSensorsBits.Substring(12, 1));
                    long DeviceSensorsBit20 = Convert.ToInt64(DeviceSensorsBits.Substring(11, 1));
                    long DeviceSensorsBit21 = Convert.ToInt64(DeviceSensorsBits.Substring(10, 1));
                    long DeviceSensorsBit22 = Convert.ToInt64(DeviceSensorsBits.Substring(9, 1));
                    long DeviceSensorsBit23 = Convert.ToInt64(DeviceSensorsBits.Substring(8, 1));
                    long DeviceSensorsBit24 = Convert.ToInt64(DeviceSensorsBits.Substring(7, 1));
                    long DeviceSensorsBit25 = Convert.ToInt64(DeviceSensorsBits.Substring(6, 1));
                    long DeviceSensorsBit26 = Convert.ToInt64(DeviceSensorsBits.Substring(5, 1));
                    long DeviceSensorsBit27 = Convert.ToInt64(DeviceSensorsBits.Substring(4, 1));
                    long DeviceSensorsBit28 = Convert.ToInt64(DeviceSensorsBits.Substring(3, 1));
                    long DeviceSensorsBit29 = Convert.ToInt64(DeviceSensorsBits.Substring(2, 1));
                    long DeviceSensorsBit30 = Convert.ToInt64(DeviceSensorsBits.Substring(1, 1));
                    long DeviceSensorsBit31 = Convert.ToInt64(DeviceSensorsBits.Substring(0, 1));

                    long? BillMachineStatus = payload.ACK.COMMAND.GET_STATUS.BILLMACHINE_STATUS;

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

                    long? BillMachineError = payload.ACK.COMMAND.GET_STATUS.BILLMACHINE_ERROR;

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

                    long? LevelSensor = payload.ACK.COMMAND.GET_STATUS.LEVEL_SENSOR;
                    long? UptimeSec = payload.ACK.COMMAND.GET_STATUS.UPTIME_SEC;

                    SqlConnection conn = new SqlConnection(@$"Server={ConfigurationManager.AppSettings["sqlServer"]};Database={ConfigurationManager.AppSettings["sqlServerDatabase"]};User Id={ConfigurationManager.AppSettings["sqlServerUser"]};Password={ConfigurationManager.AppSettings["sqlServerPassword"]};");
                    conn.Open();

                    string insert_query = "INSERT INTO message_get_status ( TopicDeviceId, Destiny, DeviceSensors, DeviceSensorsBit0, DeviceSensorsBit1, DeviceSensorsBit2, DeviceSensorsBit3, DeviceSensorsBit4, DeviceSensorsBit5, DeviceSensorsBit6, DeviceSensorsBit7, DeviceSensorsBit8, DeviceSensorsBit9, DeviceSensorsBit10, DeviceSensorsBit11, DeviceSensorsBit12, DeviceSensorsBit13, DeviceSensorsBit14, DeviceSensorsBit15, DeviceSensorsBit16, DeviceSensorsBit17, DeviceSensorsBit18, DeviceSensorsBit19, DeviceSensorsBit20, DeviceSensorsBit21, DeviceSensorsBit22, DeviceSensorsBit23, DeviceSensorsBit24, DeviceSensorsBit25, DeviceSensorsBit26, DeviceSensorsBit27, DeviceSensorsBit28, DeviceSensorsBit29, DeviceSensorsBit30, DeviceSensorsBit31, BillMachineStatus, BillMachineStatusBit0, BillMachineStatusBit1, BillMachineStatusBit2, BillMachineStatusBit3, BillMachineStatusBit4, BillMachineStatusBit5, BillMachineStatusBit6, BillMachineStatusBit7, BillMachineStatusBit8, BillMachineStatusBit9, BillMachineStatusBit10, BillMachineStatusBit11, BillMachineStatusBit12, BillMachineStatusBit13, BillMachineStatusBit14, BillMachineStatusBit15, BillMachineStatusBit16, BillMachineStatusBit17, BillMachineStatusBit18, BillMachineStatusBit19, BillMachineStatusBit20, BillMachineStatusBit21, BillMachineStatusBit22, BillMachineStatusBit23, BillMachineStatusBit24, BillMachineStatusBit25, BillMachineStatusBit26, BillMachineStatusBit27, BillMachineStatusBit28, BillMachineStatusBit29, BillMachineStatusBit30, BillMachineStatusBit31, BillMachineError, BillMachineErrorBit0, BillMachineErrorBit1, BillMachineErrorBit2, BillMachineErrorBit3, BillMachineErrorBit4, BillMachineErrorBit5, BillMachineErrorBit6, BillMachineErrorBit7, BillMachineErrorBit8, BillMachineErrorBit9, BillMachineErrorBit10, BillMachineErrorBit11, BillMachineErrorBit12, BillMachineErrorBit13, BillMachineErrorBit14, BillMachineErrorBit15, BillMachineErrorBit16, BillMachineErrorBit17, BillMachineErrorBit18, BillMachineErrorBit19, BillMachineErrorBit20, BillMachineErrorBit21, BillMachineErrorBit22, BillMachineErrorBit23, BillMachineErrorBit24, BillMachineErrorBit25, BillMachineErrorBit26, BillMachineErrorBit27, BillMachineErrorBit28, BillMachineErrorBit29, BillMachineErrorBit30, BillMachineErrorBit31, LevelSensor, UptimeSec) VALUES ( @TopicDeviceId, @Destiny, @DeviceSensors, @DeviceSensorsBit0, @DeviceSensorsBit1, @DeviceSensorsBit2, @DeviceSensorsBit3, @DeviceSensorsBit4, @DeviceSensorsBit5, @DeviceSensorsBit6, @DeviceSensorsBit7, @DeviceSensorsBit8, @DeviceSensorsBit9, @DeviceSensorsBit10, @DeviceSensorsBit11, @DeviceSensorsBit12, @DeviceSensorsBit13, @DeviceSensorsBit14, @DeviceSensorsBit15, @DeviceSensorsBit16, @DeviceSensorsBit17, @DeviceSensorsBit18, @DeviceSensorsBit19, @DeviceSensorsBit20, @DeviceSensorsBit21, @DeviceSensorsBit22, @DeviceSensorsBit23, @DeviceSensorsBit24, @DeviceSensorsBit25, @DeviceSensorsBit26, @DeviceSensorsBit27, @DeviceSensorsBit28, @DeviceSensorsBit29, @DeviceSensorsBit30, @DeviceSensorsBit31, @BillMachineStatus, @BillMachineStatusBit0, @BillMachineStatusBit1, @BillMachineStatusBit2, @BillMachineStatusBit3, @BillMachineStatusBit4, @BillMachineStatusBit5, @BillMachineStatusBit6, @BillMachineStatusBit7, @BillMachineStatusBit8, @BillMachineStatusBit9, @BillMachineStatusBit10, @BillMachineStatusBit11, @BillMachineStatusBit12, @BillMachineStatusBit13, @BillMachineStatusBit14, @BillMachineStatusBit15, @BillMachineStatusBit16, @BillMachineStatusBit17, @BillMachineStatusBit18, @BillMachineStatusBit19, @BillMachineStatusBit20, @BillMachineStatusBit21, @BillMachineStatusBit22, @BillMachineStatusBit23, @BillMachineStatusBit24, @BillMachineStatusBit25, @BillMachineStatusBit26, @BillMachineStatusBit27, @BillMachineStatusBit28, @BillMachineStatusBit29, @BillMachineStatusBit30, @BillMachineStatusBit31, @BillMachineError, @BillMachineErrorBit0, @BillMachineErrorBit1, @BillMachineErrorBit2, @BillMachineErrorBit3, @BillMachineErrorBit4, @BillMachineErrorBit5, @BillMachineErrorBit6, @BillMachineErrorBit7, @BillMachineErrorBit8, @BillMachineErrorBit9, @BillMachineErrorBit10, @BillMachineErrorBit11, @BillMachineErrorBit12, @BillMachineErrorBit13, @BillMachineErrorBit14, @BillMachineErrorBit15, @BillMachineErrorBit16, @BillMachineErrorBit17, @BillMachineErrorBit18, @BillMachineErrorBit19, @BillMachineErrorBit20, @BillMachineErrorBit21, @BillMachineErrorBit22, @BillMachineErrorBit23, @BillMachineErrorBit24, @BillMachineErrorBit25, @BillMachineErrorBit26, @BillMachineErrorBit27, @BillMachineErrorBit28, @BillMachineErrorBit29, @BillMachineErrorBit30, @BillMachineErrorBit31, @LevelSensor, @UptimeSec)";
                    SqlCommand cmd = new SqlCommand(insert_query, conn);

                    cmd.Parameters.AddWithValue("@TopicDeviceId", TopicDeviceId);
                    cmd.Parameters.AddWithValue("@Destiny", Destiny == null ? DBNull.Value : Destiny);
                    cmd.Parameters.AddWithValue("@DeviceSensors", DeviceSensors == null ? DBNull.Value : DeviceSensors);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit0", DeviceSensorsBit0 == null ? DBNull.Value : DeviceSensorsBit0);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit1", DeviceSensorsBit1 == null ? DBNull.Value : DeviceSensorsBit1);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit2", DeviceSensorsBit2 == null ? DBNull.Value : DeviceSensorsBit2);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit3", DeviceSensorsBit3 == null ? DBNull.Value : DeviceSensorsBit3);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit4", DeviceSensorsBit4 == null ? DBNull.Value : DeviceSensorsBit4);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit5", DeviceSensorsBit5 == null ? DBNull.Value : DeviceSensorsBit5);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit6", DeviceSensorsBit6 == null ? DBNull.Value : DeviceSensorsBit6);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit7", DeviceSensorsBit7 == null ? DBNull.Value : DeviceSensorsBit7);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit8", DeviceSensorsBit8 == null ? DBNull.Value : DeviceSensorsBit8);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit9", DeviceSensorsBit9 == null ? DBNull.Value : DeviceSensorsBit9);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit10", DeviceSensorsBit10 == null ? DBNull.Value : DeviceSensorsBit10);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit11", DeviceSensorsBit11 == null ? DBNull.Value : DeviceSensorsBit11);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit12", DeviceSensorsBit12 == null ? DBNull.Value : DeviceSensorsBit12);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit13", DeviceSensorsBit13 == null ? DBNull.Value : DeviceSensorsBit13);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit14", DeviceSensorsBit14 == null ? DBNull.Value : DeviceSensorsBit14);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit15", DeviceSensorsBit15 == null ? DBNull.Value : DeviceSensorsBit15);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit16", DeviceSensorsBit16 == null ? DBNull.Value : DeviceSensorsBit16);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit17", DeviceSensorsBit17 == null ? DBNull.Value : DeviceSensorsBit17);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit18", DeviceSensorsBit18 == null ? DBNull.Value : DeviceSensorsBit18);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit19", DeviceSensorsBit19 == null ? DBNull.Value : DeviceSensorsBit19);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit20", DeviceSensorsBit20 == null ? DBNull.Value : DeviceSensorsBit20);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit21", DeviceSensorsBit21 == null ? DBNull.Value : DeviceSensorsBit21);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit22", DeviceSensorsBit22 == null ? DBNull.Value : DeviceSensorsBit22);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit23", DeviceSensorsBit23 == null ? DBNull.Value : DeviceSensorsBit23);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit24", DeviceSensorsBit24 == null ? DBNull.Value : DeviceSensorsBit24);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit25", DeviceSensorsBit25 == null ? DBNull.Value : DeviceSensorsBit25);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit26", DeviceSensorsBit26 == null ? DBNull.Value : DeviceSensorsBit26);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit27", DeviceSensorsBit27 == null ? DBNull.Value : DeviceSensorsBit27);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit28", DeviceSensorsBit28 == null ? DBNull.Value : DeviceSensorsBit28);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit29", DeviceSensorsBit29 == null ? DBNull.Value : DeviceSensorsBit29);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit30", DeviceSensorsBit30 == null ? DBNull.Value : DeviceSensorsBit30);
                    cmd.Parameters.AddWithValue("@DeviceSensorsBit31", DeviceSensorsBit31 == null ? DBNull.Value : DeviceSensorsBit31);
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

                    cmd.ExecuteNonQuery();
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

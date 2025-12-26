
namespace MQTTnet.GetLockApp
{
    partial class Form2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblZabbixPing = new System.Windows.Forms.Label();
            this.lblZabbix = new System.Windows.Forms.Label();
            this.lblFileErr = new System.Windows.Forms.Label();
            this.lblSubscribed = new System.Windows.Forms.Label();
            this.TextBoxSubscriber = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.ButtonServerStop = new System.Windows.Forms.Button();
            this.ButtonServerStart = new System.Windows.Forms.Button();
            this.TextBoxPort = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.TextBoxTopicPublished = new System.Windows.Forms.TextBox();
            this.ButtonPublisherStop = new System.Windows.Forms.Button();
            this.ButtonPublish = new System.Windows.Forms.Button();
            this.ButtonSubscriberStop = new System.Windows.Forms.Button();
            this.ButtonSubscriberStart = new System.Windows.Forms.Button();
            this.ButtonGeneratePublishedMessage = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.TextBoxTopicSubscribed = new System.Windows.Forms.TextBox();
            this.ButtonSubscribe = new System.Windows.Forms.Button();
            this.maskedTextBox1 = new System.Windows.Forms.MaskedTextBox();
            this.ButtonPublisherStart = new System.Windows.Forms.Button();
            this.TextBoxPublish = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // lblZabbixPing
            // 
            this.lblZabbixPing.AutoSize = true;
            this.lblZabbixPing.Location = new System.Drawing.Point(30, 36);
            this.lblZabbixPing.Name = "lblZabbixPing";
            this.lblZabbixPing.Size = new System.Drawing.Size(89, 20);
            this.lblZabbixPing.TabIndex = 36;
            this.lblZabbixPing.Text = "Zabbix ping";
            // 
            // lblZabbix
            // 
            this.lblZabbix.AutoSize = true;
            this.lblZabbix.Location = new System.Drawing.Point(30, 72);
            this.lblZabbix.Name = "lblZabbix";
            this.lblZabbix.Size = new System.Drawing.Size(0, 20);
            this.lblZabbix.TabIndex = 35;
            // 
            // lblFileErr
            // 
            this.lblFileErr.CausesValidation = false;
            this.lblFileErr.ForeColor = System.Drawing.Color.Red;
            this.lblFileErr.Location = new System.Drawing.Point(128, 16);
            this.lblFileErr.Name = "lblFileErr";
            this.lblFileErr.Size = new System.Drawing.Size(537, 20);
            this.lblFileErr.TabIndex = 34;
            this.lblFileErr.Text = "Error writing file:";
            this.lblFileErr.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblFileErr.Visible = false;
            // 
            // lblSubscribed
            // 
            this.lblSubscribed.AutoSize = true;
            this.lblSubscribed.Location = new System.Drawing.Point(30, 16);
            this.lblSubscribed.Name = "lblSubscribed";
            this.lblSubscribed.Size = new System.Drawing.Size(58, 20);
            this.lblSubscribed.TabIndex = 33;
            this.lblSubscribed.Text = "Topic #";
            this.lblSubscribed.Visible = false;
            // 
            // TextBoxSubscriber
            // 
            this.TextBoxSubscriber.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.TextBoxSubscriber.Location = new System.Drawing.Point(30, 115);
            this.TextBoxSubscriber.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.TextBoxSubscriber.Multiline = true;
            this.TextBoxSubscriber.Name = "TextBoxSubscriber";
            this.TextBoxSubscriber.Size = new System.Drawing.Size(635, 567);
            this.TextBoxSubscriber.TabIndex = 32;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(30, 351);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(120, 20);
            this.label5.TabIndex = 29;
            this.label5.Text = "Client Subscriber";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(30, 198);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(111, 20);
            this.label4.TabIndex = 31;
            this.label4.Text = "Client Publisher";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(30, 158);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(113, 20);
            this.label3.TabIndex = 30;
            this.label3.Text = "Topic Published";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(30, 120);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 20);
            this.label2.TabIndex = 26;
            this.label2.Text = "Server";
            // 
            // ButtonServerStop
            // 
            this.ButtonServerStop.Location = new System.Drawing.Point(579, 115);
            this.ButtonServerStop.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ButtonServerStop.Name = "ButtonServerStop";
            this.ButtonServerStop.Size = new System.Drawing.Size(86, 31);
            this.ButtonServerStop.TabIndex = 23;
            this.ButtonServerStop.Text = "Stop";
            this.ButtonServerStop.UseVisualStyleBackColor = true;
            // 
            // ButtonServerStart
            // 
            this.ButtonServerStart.Location = new System.Drawing.Point(487, 115);
            this.ButtonServerStart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ButtonServerStart.Name = "ButtonServerStart";
            this.ButtonServerStart.Size = new System.Drawing.Size(86, 31);
            this.ButtonServerStart.TabIndex = 17;
            this.ButtonServerStart.Text = "Start";
            this.ButtonServerStart.UseVisualStyleBackColor = true;
            // 
            // TextBoxPort
            // 
            this.TextBoxPort.Location = new System.Drawing.Point(189, 115);
            this.TextBoxPort.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.TextBoxPort.Name = "TextBoxPort";
            this.TextBoxPort.Size = new System.Drawing.Size(114, 27);
            this.TextBoxPort.TabIndex = 15;
            this.TextBoxPort.Text = "1883";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(128, 119);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 20);
            this.label1.TabIndex = 12;
            this.label1.Text = "Port:";
            // 
            // TextBoxTopicPublished
            // 
            this.TextBoxTopicPublished.Location = new System.Drawing.Point(179, 154);
            this.TextBoxTopicPublished.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.TextBoxTopicPublished.Name = "TextBoxTopicPublished";
            this.TextBoxTopicPublished.Size = new System.Drawing.Size(485, 27);
            this.TextBoxTopicPublished.TabIndex = 14;
            this.TextBoxTopicPublished.Text = "brand/type/group/code";
            // 
            // ButtonPublisherStop
            // 
            this.ButtonPublisherStop.Location = new System.Drawing.Point(579, 192);
            this.ButtonPublisherStop.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ButtonPublisherStop.Name = "ButtonPublisherStop";
            this.ButtonPublisherStop.Size = new System.Drawing.Size(86, 31);
            this.ButtonPublisherStop.TabIndex = 21;
            this.ButtonPublisherStop.Text = "Stop";
            this.ButtonPublisherStop.UseVisualStyleBackColor = true;
            // 
            // ButtonPublish
            // 
            this.ButtonPublish.Location = new System.Drawing.Point(579, 231);
            this.ButtonPublish.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ButtonPublish.Name = "ButtonPublish";
            this.ButtonPublish.Size = new System.Drawing.Size(86, 31);
            this.ButtonPublish.TabIndex = 24;
            this.ButtonPublish.Text = "Publish";
            this.ButtonPublish.UseVisualStyleBackColor = true;
            // 
            // ButtonSubscriberStop
            // 
            this.ButtonSubscriberStop.Location = new System.Drawing.Point(579, 346);
            this.ButtonSubscriberStop.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ButtonSubscriberStop.Name = "ButtonSubscriberStop";
            this.ButtonSubscriberStop.Size = new System.Drawing.Size(86, 31);
            this.ButtonSubscriberStop.TabIndex = 25;
            this.ButtonSubscriberStop.Text = "Stop";
            this.ButtonSubscriberStop.UseVisualStyleBackColor = true;
            // 
            // ButtonSubscriberStart
            // 
            this.ButtonSubscriberStart.Location = new System.Drawing.Point(487, 346);
            this.ButtonSubscriberStart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ButtonSubscriberStart.Name = "ButtonSubscriberStart";
            this.ButtonSubscriberStart.Size = new System.Drawing.Size(86, 31);
            this.ButtonSubscriberStart.TabIndex = 20;
            this.ButtonSubscriberStart.Text = "Start";
            this.ButtonSubscriberStart.UseVisualStyleBackColor = true;
            // 
            // ButtonGeneratePublishedMessage
            // 
            this.ButtonGeneratePublishedMessage.Location = new System.Drawing.Point(487, 230);
            this.ButtonGeneratePublishedMessage.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ButtonGeneratePublishedMessage.Name = "ButtonGeneratePublishedMessage";
            this.ButtonGeneratePublishedMessage.Size = new System.Drawing.Size(86, 31);
            this.ButtonGeneratePublishedMessage.TabIndex = 19;
            this.ButtonGeneratePublishedMessage.Text = "Random";
            this.ButtonGeneratePublishedMessage.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(30, 388);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(122, 20);
            this.label6.TabIndex = 28;
            this.label6.Text = "Topic Subscribed";
            // 
            // TextBoxTopicSubscribed
            // 
            this.TextBoxTopicSubscribed.Location = new System.Drawing.Point(179, 384);
            this.TextBoxTopicSubscribed.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.TextBoxTopicSubscribed.Name = "TextBoxTopicSubscribed";
            this.TextBoxTopicSubscribed.Size = new System.Drawing.Size(393, 27);
            this.TextBoxTopicSubscribed.TabIndex = 13;
            this.TextBoxTopicSubscribed.Text = "brand/type/group/code";
            // 
            // ButtonSubscribe
            // 
            this.ButtonSubscribe.Location = new System.Drawing.Point(579, 383);
            this.ButtonSubscribe.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ButtonSubscribe.Name = "ButtonSubscribe";
            this.ButtonSubscribe.Size = new System.Drawing.Size(86, 31);
            this.ButtonSubscribe.TabIndex = 22;
            this.ButtonSubscribe.Text = "Subscribe";
            this.ButtonSubscribe.UseVisualStyleBackColor = true;
            // 
            // maskedTextBox1
            // 
            this.maskedTextBox1.Location = new System.Drawing.Point(0, 0);
            this.maskedTextBox1.Name = "maskedTextBox1";
            this.maskedTextBox1.Size = new System.Drawing.Size(100, 27);
            this.maskedTextBox1.TabIndex = 27;
            this.maskedTextBox1.Text = "maskedTextBox1";
            this.maskedTextBox1.Visible = false;
            // 
            // ButtonPublisherStart
            // 
            this.ButtonPublisherStart.Location = new System.Drawing.Point(487, 192);
            this.ButtonPublisherStart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ButtonPublisherStart.Name = "ButtonPublisherStart";
            this.ButtonPublisherStart.Size = new System.Drawing.Size(86, 31);
            this.ButtonPublisherStart.TabIndex = 18;
            this.ButtonPublisherStart.Text = "Start";
            this.ButtonPublisherStart.UseVisualStyleBackColor = true;
            // 
            // TextBoxPublish
            // 
            this.TextBoxPublish.Location = new System.Drawing.Point(30, 231);
            this.TextBoxPublish.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.TextBoxPublish.Name = "TextBoxPublish";
            this.TextBoxPublish.Size = new System.Drawing.Size(450, 27);
            this.TextBoxPublish.TabIndex = 16;
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(717, 708);
            this.Controls.Add(this.lblZabbixPing);
            this.Controls.Add(this.lblZabbix);
            this.Controls.Add(this.lblFileErr);
            this.Controls.Add(this.lblSubscribed);
            this.Controls.Add(this.TextBoxSubscriber);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.ButtonServerStop);
            this.Controls.Add(this.ButtonServerStart);
            this.Controls.Add(this.TextBoxPort);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.TextBoxTopicPublished);
            this.Controls.Add(this.ButtonPublisherStop);
            this.Controls.Add(this.ButtonPublish);
            this.Controls.Add(this.ButtonSubscriberStop);
            this.Controls.Add(this.ButtonSubscriberStart);
            this.Controls.Add(this.ButtonGeneratePublishedMessage);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.TextBoxTopicSubscribed);
            this.Controls.Add(this.ButtonSubscribe);
            this.Controls.Add(this.maskedTextBox1);
            this.Controls.Add(this.ButtonPublisherStart);
            this.Controls.Add(this.TextBoxPublish);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form2";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Agyliti MQTT GetLock v2";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblZabbixPing;
        private System.Windows.Forms.Label lblZabbix;
        private System.Windows.Forms.Label lblFileErr;
        private System.Windows.Forms.Label lblSubscribed;
        private System.Windows.Forms.TextBox TextBoxSubscriber;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button ButtonServerStop;
        private System.Windows.Forms.Button ButtonServerStart;
        private System.Windows.Forms.TextBox TextBoxPort;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox TextBoxTopicPublished;
        private System.Windows.Forms.Button ButtonPublisherStop;
        private System.Windows.Forms.Button ButtonPublish;
        private System.Windows.Forms.Button ButtonSubscriberStop;
        private System.Windows.Forms.Button ButtonSubscriberStart;
        private System.Windows.Forms.Button ButtonGeneratePublishedMessage;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox TextBoxTopicSubscribed;
        private System.Windows.Forms.Button ButtonSubscribe;
        private System.Windows.Forms.MaskedTextBox maskedTextBox1;
        private System.Windows.Forms.Button ButtonPublisherStart;
        private System.Windows.Forms.TextBox TextBoxPublish;
    }
}
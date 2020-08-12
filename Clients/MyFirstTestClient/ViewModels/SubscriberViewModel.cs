// <copyright company="ROSEN Swiss AG">
//  Copyright (c) ROSEN Swiss AG
//  This computer program includes confidential, proprietary
//  information and is a trade secret of ROSEN. All use,
//  disclosure, or reproduction is prohibited unless authorized in
//  writing by an officer of ROSEN. All Rights Reserved.
// </copyright>

namespace MyFirstTestClient.ViewModels
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Media;
    using MQTTnet;
    using MQTTnet.Client;
    using MQTTnet.Client.Options;
    using MQTTnet.Client.Subscribing;
    using MyFirstTestClient.Annotations;

    internal class SubscriberViewModel : INotifyPropertyChanged
    {
        #region Fields

        private static IMqttClient mqttClient;

        private double myColor_Lamp1;

        #endregion

        #region Properties

        public double Color_Lamp1
        {
            get => this.myColor_Lamp1;
            set
            {
                this.myColor_Lamp1 = value;
                this.OnPropertyChanged();
            }
        }

        public double Color_Lamp2 { get; set; }

        public double Color_Lamp3 { get; set; }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Constructors

        public SubscriberViewModel()
        {
            var factory = new MqttFactory();
            SubscriberViewModel.mqttClient = factory.CreateMqttClient();
            Task.Run(this.Connect);
            SubscriberViewModel.mqttClient.UseConnectedHandler(
                async e =>
                {
                    await SubscriberViewModel.mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter("/Theater/lamp1").Build());
                    await SubscriberViewModel.mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter("/Theater/lamp2").Build());
                    await SubscriberViewModel.mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter("/Theater/lamp3").Build());
                    await SubscriberViewModel.mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter("/Theater/music1").Build());
                });
            SubscriberViewModel.mqttClient.UseApplicationMessageReceivedHandler(this.client_MqttMsgPublishReceived);
        }

        #endregion

        #region Methods

        private void client_MqttMsgPublishReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            var enc = new ASCIIEncoding();
            if (e.ApplicationMessage.Payload == null)
            {
                e.ApplicationMessage.Payload = enc.GetBytes("0");
            }

            if (enc.GetString(e.ApplicationMessage.Payload) == "RequestConnectionStatus")
            {
                PublishMessageToConnectionTopic($"Connected {e.ApplicationMessage.Topic.Split('/').Last()}", "/Connection");
                return;
            }

            switch (e.ApplicationMessage.Topic)
            {
                case "/Theater/lamp1":
                    this.Color_Lamp1 = double.Parse(enc.GetString(e.ApplicationMessage.Payload));
                    this.OnPropertyChanged(nameof(Color_Lamp1));
                    break;
                case "/Theater/lamp2":
                    this.Color_Lamp2 = double.Parse(enc.GetString(e.ApplicationMessage.Payload));
                    this.OnPropertyChanged(nameof(Color_Lamp2));
                    break;
                case "/Theater/lamp3":
                    this.Color_Lamp3 = double.Parse(enc.GetString(e.ApplicationMessage.Payload));
                    this.OnPropertyChanged(nameof(Color_Lamp3));
                    break;
                case "/Theater/music1":
                    this.Play(enc.GetString(e.ApplicationMessage.Payload));
                    break;
            }
        }

        private async Task Connect()
        {
            await this.ConnectClient();
            await PublishMessageToConnectionTopic("Connected lamp1", "/Connection");
            await PublishMessageToConnectionTopic("Connected lamp2", "/Connection");
            await PublishMessageToConnectionTopic("Connected lamp3", "/Connection");
            await PublishMessageToConnectionTopic("Connected music1", "/Connection");
        }

        private async Task ConnectClient()
        {
            var options = new MqttClientOptionsBuilder().WithTcpServer("linvm2416", 1883).Build();
            await mqttClient.ConnectAsync(options, CancellationToken.None);
        }

        private void MediaPlayer_MediaFailed(object sender, ExceptionEventArgs e)
        {
            throw new NotImplementedException();
        }

        public static async Task OnClosing()
        {
            await Task.Run(() => PublishMessageToConnectionTopic("Disconnected lamp1", "/Connection"));
            await PublishMessageToConnectionTopic("Disconnected lamp2", "/Connection");
            await PublishMessageToConnectionTopic("Disconnected lamp3", "/Connection");
            await PublishMessageToConnectionTopic("Disconnected music1", "/Connection");
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //private MediaPlayer player;

        private void Play(string song)
        {
            var uri = new Uri(@"../../Resources/" + song, UriKind.Relative);
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + uri))
            {
                return;
            }

            var player = new MediaPlayer();
            player.Open(uri);
            player.Balance = 0;
            player.Volume = 0.1;
            player.Position = new TimeSpan(0);
            player.Play();
        }

        private static async Task PublishMessageToConnectionTopic(string payload, string topic)
        {
            var message = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(payload).WithExactlyOnceQoS().WithRetainFlag(false)
                .Build(); //.WithRetainFlag()

            await mqttClient.PublishAsync(message, CancellationToken.None);
        }

        #endregion
    }
}
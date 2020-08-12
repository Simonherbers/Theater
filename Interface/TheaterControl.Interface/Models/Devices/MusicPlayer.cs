using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheaterControl.Interface.Models.Devices
{
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Windows.Input;
    using MQTTnet;
    using MQTTnet.Client;
    using TheaterControl.Interface.Helper;
    using TheaterControl.Interface.ViewModels;

    class MusicPlayer: IDevice
    {
        public string Topic { get; set; }

        public object Value { get; set; }

        public int Id { get; set; }

        public bool ConnectionStatus { get; set; }

        public IMqttClient MqttClient { get; set; }

        public ICommand PublishCommand { get; set; }

        public Uri DeviceImageUri { get; set; }

        public void PublishValue()
        {
            this.Publish();
        }

        public string Name { get; set; }

        public MusicPlayer()
        {
            this.Topic = "/Theater/music";
            this.Value = "sample.mp3";
            this.Id = 1;
            this.ConnectionStatus = false;
            this.PublishCommand = new RelayCommand(this.PublishExecute);
        }

        private void PublishExecute(object sender)
        {
            this.Publish();
        }
        private async void Publish()
        {
            var message = new MqttApplicationMessageBuilder().WithTopic(this.Topic).WithPayload(this.Value.ToString()).WithExactlyOnceQoS().WithRetainFlag(false).Build();

            await this.MqttClient.PublishAsync(message, CancellationToken.None);
        }
    }
}

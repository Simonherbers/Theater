using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheaterControl.Interface.Models
{
    using System.Drawing;
    using System.Threading;
    using System.Windows.Input;
    using MQTTnet;
    using MQTTnet.Client;
    using TheaterControl.Interface.Helper;

    internal class Fog: IDevice
    {
        public Fog()
        {
            this.PublishCommand = new RelayCommand(this.PublishExecute);
            this.Topic = "/Theater/fog";
            this.Value = "1";
        }

        public void PublishExecute(object sender)
        {
            this.Publish();
        }

        public string Topic { get; set; }

        public object Value { get; set; }

        public int Id { get; set; }

        public bool ConnectionStatus
        {
            get; set;
        }

        public IMqttClient MqttClient { get; set; }

        public ICommand PublishCommand { get; set; }

        public Uri DeviceImageUri { get; set; }

        public void PublishValue()
        {
            this.Publish();
        }

        public string Name { get; set; }

        private async void Publish()
        {
            if (!double.TryParse(this.Value.ToString(), out var result))
            {
                return;
            }
            var message = new MqttApplicationMessageBuilder().WithTopic(this.Topic).WithPayload(this.Value.ToString()).WithExactlyOnceQoS().WithRetainFlag(false).Build();

            await this.MqttClient.PublishAsync(message, CancellationToken.None);
        }
    }
}

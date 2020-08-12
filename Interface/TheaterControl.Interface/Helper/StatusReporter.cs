// <copyright company="ROSEN Swiss AG">
//  Copyright (c) ROSEN Swiss AG
//  This computer program includes confidential, proprietary
//  information and is a trade secret of ROSEN. All use,
//  disclosure, or reproduction is prohibited unless authorized in
//  writing by an officer of ROSEN. All Rights Reserved.
// </copyright>

namespace TheaterControl.Interface.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using MQTTnet;
    using MQTTnet.Client;
    using MQTTnet.Client.Subscribing;

    class StatusReporter
    {
        #region Fields

        private readonly Queue<MqttApplicationMessageReceivedEventArgs> DeviceConnectionQueue = new Queue<MqttApplicationMessageReceivedEventArgs>();

        private static IMqttClient mqttClient;

        public event EventHandler<(string, bool)> UpdateStatus;
        #endregion

        #region Properties

        public ObservableCollection<IDevice> Devices { get; set; }

        #endregion

        #region Constructors

        private StatusReporter(IMqttClient client)
        {
            mqttClient = client;
            mqttClient.UseConnectedHandler(
                async e =>
                {
                    await mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter("/Connection").Build());
                });
            mqttClient.UseApplicationMessageReceivedHandler(this.HandleConnectionMessage);
        }

        #endregion

        #region Methods

        private void HandleConnectionMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            this.DeviceConnectionQueue.Enqueue(e);
        }
        
        private async Task PeriodicDeviceConnectionCheck(TimeSpan interval, CancellationToken cancellationToken)
        {
            while (true)
            {
                if (this.DeviceConnectionQueue.Count > 0)
                {
                    this.UpdateDeviceStatus(this.DeviceConnectionQueue.Dequeue());
                }

                await Task.Delay(interval, cancellationToken);
            }
        }

        public static async void RequestConnectionStatus(IDevice device)
        {
            var message = new MqttApplicationMessageBuilder().WithTopic(device.Topic).WithPayload("RequestConnectionStatus").WithExactlyOnceQoS().WithRetainFlag(false)
                .Build();

            await mqttClient.PublishAsync(message, CancellationToken.None);
        }

        public void Start()
        {
            Task.Run(() => this.PeriodicDeviceConnectionCheck(new TimeSpan(100), CancellationToken.None));
        }

        internal static StatusReporter StartReporting(ref ObservableCollection<IDevice> devices, IMqttClient client)
        {
            var reporter = new StatusReporter(client) { Devices = devices };
            reporter.Start();
            return reporter;
        }

        private void UpdateDeviceStatus(MqttApplicationMessageReceivedEventArgs e)
        {
            if (this.Devices.Count == 0)
            {
                return;
            }

            var enc = new ASCIIEncoding();
            var payload = enc.GetString(e.ApplicationMessage.Payload).Split(' ');
            var status = payload[0];
            var deviceTopic = payload[1];
            var device = this.Devices.ToList().Find(x => x.Topic.Split('/').Last() == deviceTopic);
            if (device == null)
            {
                return;
            }
            var state = status == "Connected";
            device.ConnectionStatus = state;
            this.UpdateStatus?.Invoke(this, (device.Topic, state));
        }

        #endregion
    }
}
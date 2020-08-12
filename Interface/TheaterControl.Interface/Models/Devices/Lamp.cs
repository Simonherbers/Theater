// <copyright company="ROSEN Swiss AG">
//  Copyright (c) ROSEN Swiss AG
//  This computer program includes confidential, proprietary
//  information and is a trade secret of ROSEN. All use,
//  disclosure, or reproduction is prohibited unless authorized in
//  writing by an officer of ROSEN. All Rights Reserved.
// </copyright>

namespace TheaterControl.Interface.Models
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Windows.Input;
    using MQTTnet;
    using MQTTnet.Client;
    using TheaterControl.Interface.Annotations;
    using TheaterControl.Interface.Helper;
    using TheaterControl.Interface.ViewModels;

    internal class Lamp : IDevice, INotifyPropertyChanged
    {
        #region Fields

        private bool myConnectionStatus;

        private const string TOPIC = "/Theater/lamp";

        #endregion

        #region Properties

        public bool ConnectionStatus
        {
            get => myConnectionStatus;
            set
            {
                this.myConnectionStatus = value;
                this.OnPropertyChanged();
            }
        }

        public IMqttClient MqttClient { get; set; }

        public Uri DeviceImageUri { get; set; }

        public int Id { get; set; }

        public ICommand PublishCommand { get; set; }

        public string Topic { get; set; }

        public object Value { get; set; }
        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Constructors

        public Lamp()
        {
            this.PublishCommand = new RelayCommand(this.PublishExecute);
            this.Value = 0;
            this.Topic = Lamp.TOPIC;
            this.ConnectionStatus = false;
        }

        #endregion

        #region Methods

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void PublishExecute(object sender)
        {
            this.Publish();
        }

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

        #endregion
    }
}
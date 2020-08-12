// <copyright company="ROSEN Swiss AG">
//  Copyright (c) ROSEN Swiss AG
//  This computer program includes confidential, proprietary
//  information and is a trade secret of ROSEN. All use,
//  disclosure, or reproduction is prohibited unless authorized in
//  writing by an officer of ROSEN. All Rights Reserved.
// </copyright>

namespace TheaterControl.UI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using MQTTnet;
    using MQTTnet.Client;
    using MQTTnet.Client.Options;
    using MQTTnet.Client.Subscribing;
    using TheaterControl.UI.Annotations;
    using TheaterControl.UI.Helper;

    internal class MainViewModel : INotifyPropertyChanged
    {
        #region Fields

        private BackgroundWorker backgroundWorker;

        private const string DEVICE_CHANGED_PAYLOAD = "DeviceChanged";

        private const string DEVICE_TOPIC = "/Theater/Device";

        private BackgroundWorker deviceWorker;

        private ASCIIEncoding enc = new ASCIIEncoding();

        private IMqttClient mqttClient;

        private ObservableCollection<string> mySceneDescriptions;

        private int mySelectedScene;

        private string mySelectedSong;

        private readonly Queue<MqttApplicationMessageReceivedEventArgs> ReceivedEventArgsQueue;

        private const string REQUEST_SCENES_PAYLOAD = "RequestScenes";

        private const string SCENE_CONFIGURATION_TOPIC = "/Theater/Scene";

        private const string SCENE_CONTROL_TOPIC = "/Theater/Control";

        private const string SCENES_CHANGED_PAYLOAD = "ScenesChanged";

        private const string SELECTION_TOPIC = "/Theater/Selection";

        private const string SERVER_ADDRESS = "linvm2416";

        private const string SONG_CONTROL_TOPIC_FROM_UI = "/Theater/SongControlUI";

        private const string SONG_TOPIC = "/Theater/songs";

        private const string SONGS_CHANGED_PAYLOAD = "SongsChanged";

        #endregion

        #region Properties

        public ICommand Command { get; set; }

        public ObservableCollection<string> Devices { get; set; }

        public ICommand MusicCommand { get; set; }

        public ObservableCollection<string> SceneDescriptions
        {
            get => this.mySceneDescriptions;
            set
            {
                this.mySceneDescriptions = value;
                this.OnPropertyChanged();
            }
        }

        public int SelectedScene
        {
            get => this.mySelectedScene;
            set
            {
                this.mySelectedScene = value;
                this.OnPropertyChanged();
            }
        }

        public string SelectedSong
        {
            get => this.mySelectedSong;
            set
            {
                this.mySelectedSong = value;
                this.OnPropertyChanged();
            }
        }

        public ObservableCollection<string> Songs { get; set; }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Constructors

        public MainViewModel()
        {
            var clientSetupTask = this.SetupClient();
            this.SceneDescriptions = new ObservableCollection<string>();
            this.Devices = new ObservableCollection<string>();
            this.Songs = new ObservableCollection<string>();
            this.ReceivedEventArgsQueue = new Queue<MqttApplicationMessageReceivedEventArgs>();
            this.Command = new RelayCommand(this.ControlScenes);
            this.MusicCommand = new RelayCommand(this.ControlMusic);

            this.backgroundWorker = new BackgroundWorker();
            this.backgroundWorker.DoWork += this.GetUpdateMessage;
            this.backgroundWorker.RunWorkerCompleted += this.UpdateCollection;

            clientSetupTask.Wait();
            this.RequestAllScenes();

            Task.Run(() => this.WorkQueue(new TimeSpan(20)));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Connects the <see cref="MqttClient"/> to a serve hosted locally on port 1883.
        /// </summary>
        /// <returns></returns>
        private async Task ConnectClient()
        {
            var options = new MqttClientOptionsBuilder().WithTcpServer(MainViewModel.SERVER_ADDRESS, 1883).Build();
            await this.mqttClient.ConnectAsync(options, CancellationToken.None);
        }

        private void ControlMusic(object parameters)
        {
            this.Publish(MainViewModel.SONG_CONTROL_TOPIC_FROM_UI, (string)parameters);
        }

        /// <summary>
        /// This method publishes any command string to the topic <see cref="SCENE_CONTROL_TOPIC"></see>.
        /// </summary>
        /// <param name="parameter"></param>
        private void ControlScenes(object parameter)
        {
            var command = parameter as string;
            if (command == null)
            {
                return;
            }

            this.Publish(MainViewModel.SCENE_CONTROL_TOPIC, command);
        }

        /// <summary>
        /// Sets the argument of the <see cref="BackgroundWorker"/> as result to trigger the Completed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"> Contains the <see cref="MqttApplicationMessageReceivedEventArgs"/> dequeued from <see cref="ReceivedEventArgsQueue"/>.</param>
        private void GetUpdateMessage(object sender, DoWorkEventArgs e)
        {
            e.Result = ((MqttApplicationMessageReceivedEventArgs)e.Argument);
        }

        /// <summary>
        /// Enqueues every received message in <see cref="ReceivedEventArgsQueue"/> to handle them in order.
        /// </summary>
        /// <param name="e"></param>
        private void HandleSceneUpdates(MqttApplicationMessageReceivedEventArgs e)
        {
            this.ReceivedEventArgsQueue.Enqueue(e);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Publishes any <param name="payload">payload</param> to the specified <param name="topic">topic</param>.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="payload"></param>
        private async void Publish(string topic, string payload)
        {
            var message = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(payload).WithExactlyOnceQoS().WithRetainFlag(false).Build();

            await this.mqttClient.PublishAsync(message, CancellationToken.None);
        }

        /// <summary>
        /// Publishes the <see cref="REQUEST_SCENES_PAYLOAD"></see> to topic <see cref="SCENE_CONFIGURATION_TOPIC"/>.
        /// </summary>
        private void RequestAllScenes()
        {
            Application.Current.Dispatcher.Invoke(() => { this.Publish(MainViewModel.SCENE_CONFIGURATION_TOPIC, MainViewModel.REQUEST_SCENES_PAYLOAD); });
        }

        /// <summary>
        /// Initializes the <see cref="MqttClient"/> and tries to connect to the server asynchronously.
        /// Returns a <see cref="Task"/> to await the established connection.
        /// </summary>
        /// <returns></returns>
        private Task<IMqttClient> SetupClient()
        {
            var factory = new MqttFactory();
            this.mqttClient = factory.CreateMqttClient();

            this.mqttClient.UseConnectedHandler(
                async e =>
                {
                    await this.mqttClient.SubscribeAsync(
                        new MqttClientSubscribeOptionsBuilder().WithTopicFilter(MainViewModel.SCENE_CONFIGURATION_TOPIC).Build());
                    await this.mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(MainViewModel.DEVICE_TOPIC).Build());
                    await this.mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(MainViewModel.SONG_TOPIC).Build());
                    await this.mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(MainViewModel.SELECTION_TOPIC).Build());
                });
            this.mqttClient.UseApplicationMessageReceivedHandler(this.HandleSceneUpdates);
            return Task.Run(
                async () =>
                {
                    await this.ConnectClient();
                    return this.mqttClient;
                });
        }

        private void UpdateCollection(object sender, RunWorkerCompletedEventArgs e)
        {
            var topic = ((MqttApplicationMessageReceivedEventArgs)e.Result).ApplicationMessage.Topic;
            var payload = this.enc.GetString(((MqttApplicationMessageReceivedEventArgs)e.Result).ApplicationMessage.Payload);
            this.UpdateList(topic, payload);
        }

        private void UpdateList(string topic, string payload)
        {
            if (payload == string.Empty || payload == MainViewModel.REQUEST_SCENES_PAYLOAD)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    switch (payload)
                    {
                        case MainViewModel.SCENES_CHANGED_PAYLOAD:
                            this.SceneDescriptions.Clear();
                            return;
                        case MainViewModel.DEVICE_CHANGED_PAYLOAD:
                            this.Devices.Clear();
                            return;
                        case MainViewModel.SONGS_CHANGED_PAYLOAD:
                            this.Songs.Clear();
                            return;
                    }

                    switch (topic)
                    {
                        case MainViewModel.SCENE_CONFIGURATION_TOPIC:
                            this.SceneDescriptions.Add(payload);
                            this.OnPropertyChanged(nameof(this.SceneDescriptions));
                            break;
                        case MainViewModel.SONG_TOPIC:
                            this.Songs.Add(payload);
                            this.OnPropertyChanged(nameof(this.Songs));
                            break;
                        case MainViewModel.SELECTION_TOPIC:
                            var splitPayload = payload.Split(' ');
                            if (splitPayload[0] == "Scene")
                            {
                                this.SelectedScene = int.Parse(splitPayload[1]) - 1;
                            }
                            else if (splitPayload[0] == "Song")
                            {
                                this.SelectedSong = this.Songs.ToList().Find(x => x == splitPayload[1]);
                            }

                            break;
                        default:
                            this.Devices.Add(payload);
                            this.OnPropertyChanged(nameof(this.Devices));
                            break;
                    }
                });
        }

        private void UpdateScenes(string payload)
        {
            if (payload == string.Empty || payload == MainViewModel.REQUEST_SCENES_PAYLOAD)
            {
                return;
            }

            if (payload == MainViewModel.SCENES_CHANGED_PAYLOAD)
            {
                Application.Current.Dispatcher.Invoke(() => { this.SceneDescriptions.Clear(); });
                return;
            }

            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    this.SceneDescriptions.Add(payload);
                    this.OnPropertyChanged(nameof(SceneDescriptions));
                });
        }

        private Task WorkQueue(TimeSpan timeSpan)
        {
            while (true)
            {
                if (this.ReceivedEventArgsQueue.Count > 0 && !this.backgroundWorker.IsBusy)
                {
                    this.backgroundWorker.RunWorkerAsync(this.ReceivedEventArgsQueue.Dequeue());
                }

                Thread.Sleep(timeSpan);
            }
        }

        #endregion
    }
}
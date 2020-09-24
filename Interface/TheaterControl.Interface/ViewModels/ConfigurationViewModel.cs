// <copyright company="ROSEN Swiss AG">
//  Copyright (c) ROSEN Swiss AG
//  This computer program includes confidential, proprietary
//  information and is a trade secret of ROSEN. All use,
//  disclosure, or reproduction is prohibited unless authorized in
//  writing by an officer of ROSEN. All Rights Reserved.
// </copyright>

namespace TheaterControl.Interface.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using MQTTnet;
    using MQTTnet.Client;
    using MQTTnet.Client.Options;
    using MQTTnet.Client.Subscribing;
    using TheaterControl.Interface.Annotations;
    using TheaterControl.Interface.Constants;
    using TheaterControl.Interface.Helper;
    using TheaterControl.Interface.Models;

    internal class ConfigurationViewModel : INotifyPropertyChanged
    {
        #region Fields
        private ASCIIEncoding enc = new ASCIIEncoding();

        private const string RELATIVE_PATH_CONFIGURATION = @"\..\..\..\Configuration\";

        private const string RELATIVE_PATH_SONGS = @"\..\..\..\..\TheaterControl.MusicPlayer\Music\";

        private const double LENGTH_OF_TIME_SKIP = 5.0;

        private IMqttClient myMqttClient;

        private readonly Queue<Action> myPublishQueue;

        private string myRunningSong;

        private CancellationTokenSource mySceneDurationCancellationTokenSource;

        private ObservableCollection<Scene> myScenes;

        private Scene mySelectedScene;

        private const string SERVER_ADDRESS = "localhost";

        private const string SONG_CONTROL_TOPIC_FROM_INTERFACE = "/Theater/SongControlInterface";

        private List<string> songList;

        #endregion

        #region Properties

        public static List<Type> DeviceTypes { get; set; }

        private string RunningSong
        {
            get => this.myRunningSong;
            set
            {
                this.myRunningSong = value;
                var index = this.songList.IndexOf(this.songList.Find(x => x == value));
                this.UpdateUISelection(Payloads.SONG_SELECTION_CHANGED + index);
                this.currentlySelectedSongIndex = Payloads.SONG_SELECTION_CHANGED + index;
            }
        }

        public ObservableCollection<Scene> Scenes
        {
            get => this.myScenes;
            set
            {
                this.myScenes = value;
                this.OnPropertyChanged();
            }
        }

        public Scene SelectedScene
        {
            get => this.mySelectedScene;
            set
            {
                this.mySelectedScene = value;
                this.OnPropertyChanged();
                if (value != null)
                {
                    var payload = Payloads.SCENE_SELECTION_CHANGED + (value.Id - 1);
                    this.UpdateUISelection(payload);
                    this.currentlySelectedSceneIndex = payload;
                }
            }
        }
        private string currentlySelectedSceneIndex;
        private string currentlySelectedSongIndex;
        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Constructors

        public ConfigurationViewModel()
        {
            this.Scenes = new ObservableCollection<Scene>(FileReadHelper.ReadScenesWithDevices());
            this.myPublishQueue = new Queue<Action>();

            var clientSetupTask = this.SetupClient();

            //this.StatusReporter = StatusReporter.StartReporting(ref this.myDevices, this.MqttClient);
            //this.StatusReporter.UpdateStatus += this.UpdateStatus;

            //this.mySceneReporter = new SceneReporter(this.myMqttClient);
           
            this.myMqttClient.UseConnectedHandler(
                async e =>
                {
                    await myMqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(Topics.SCENE_CONFIGURATION_TOPIC).Build());
                    await myMqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(Topics.SCENE_CONTROL_TOPIC).Build());
                    await myMqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(Topics.SONG_CONTROL_TOPIC_FROM_UI).Build());
                    await myMqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(Topics.SELECTION_TOPIC).Build());
                });
            this.myMqttClient.UseApplicationMessageReceivedHandler(this.HandleMessage);
            this.StartMonitoring();

            clientSetupTask.Wait();
            this.UpdateUI();
            Task.Run(() => this.Publish(new TimeSpan(100)));
            this.SelectedScene = this.Scenes[0];
            this.RunningSong = this.SelectedScene.Devices.FirstOrDefault(x => x.Topic == Topics.MUSIC_TOPIC).Value.ToString() ?? string.Empty;
        }

        #endregion

        #region Methods
        private void HandleMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            var payload = this.enc.GetString(e.ApplicationMessage.Payload);
            if (payload == Payloads.SCENES_CHANGED_PAYLOAD)
            {
                return;
            }

            if (e.ApplicationMessage.Topic == Topics.SONG_CONTROL_TOPIC_FROM_UI)
            {
                if (Enum.TryParse<SongControl>(payload, out var action))
                    this.EnqueueControl(action.ToString());
                return;
            }
            if (e.ApplicationMessage.Topic == Topics.SELECTION_TOPIC)
            {
                this.EnqueueControl(SceneControl.SelectionChanged.ToString() + " " + payload);
                return;
            }

            var control = Enum.GetNames(typeof(SceneControl)).FirstOrDefault(x => x == payload);
            if (control == null)
            {
                return;
            }

            this.EnqueueControl(control);

        }
        private void EnqueueControl(string action)
        {
            if(this.SelectedScene == null)
            {
                this.SelectedScene = this.Scenes[0];
            }
            var current = this.RunningSong;
            var payload = string.Empty;
            var control = action.Split(' ');
            switch (control[0])
            {
                case nameof(SceneControl.Next):
                    this.myPublishQueue.Enqueue(this.NextScene);
                    break;
                case nameof(SceneControl.Play):
                    this.myPublishQueue.Enqueue(this.PlayScene);
                    break;
                case nameof(SceneControl.Stop):
                    this.myPublishQueue.Enqueue(this.StopMusic);
                    break;
                case nameof(SceneControl.Previous):
                    this.myPublishQueue.Enqueue(this.PreviousScene);
                    break;
                case nameof(SceneControl.RequestScenes):
                    this.myPublishQueue.Enqueue(this.UpdateUI);
                    break;
                case nameof(SceneControl.Emergency):
                    this.myPublishQueue.Enqueue(this.EmergencyStop);
                    break;
                case nameof(SceneControl.ScenesChanged):
                    this.myPublishQueue.Enqueue(this.UpdateUI);
                    break;
                case nameof(SceneControl.SelectionChanged):
                    if (control[1].StartsWith("s"))
                    {
                        this.myPublishQueue.Enqueue(() => this.ChangeSceneSelection(control[1]));
                    }
                    else
                    {
                        //SceneReporter.SendMessageToServer(Topics.SELECTION_TOPIC, "m" + value[1], this.myMqttClient);
                    }
                    break;


                case nameof(SongControl.PrevSong):
                    this.PlayDifferentSong(current, -1);
                    break;
                case nameof(SongControl.RunBack):
                    payload = $"RunBackTime {ConfigurationViewModel.LENGTH_OF_TIME_SKIP}";
                    ConfigurationViewModel.SendMessageToServer(ConfigurationViewModel.SONG_CONTROL_TOPIC_FROM_INTERFACE, payload, this.myMqttClient);
                    break;
                case nameof(SongControl.Pause):
                    payload = "TogglePause";
                    ConfigurationViewModel.SendMessageToServer(ConfigurationViewModel.SONG_CONTROL_TOPIC_FROM_INTERFACE, payload, this.myMqttClient);
                    break;
                case nameof(SongControl.RunForward):
                    payload = $"RunForwardTime {ConfigurationViewModel.LENGTH_OF_TIME_SKIP}";
                    ConfigurationViewModel.SendMessageToServer(ConfigurationViewModel.SONG_CONTROL_TOPIC_FROM_INTERFACE, payload, this.myMqttClient);
                    break;
                case nameof(SongControl.NextSong):
                    this.PlayDifferentSong(current, 1);
                    break;
            }

        }

        /////done
        //private void HandleSongControlEvent(object sender, string e)
        //{
        //    var current = this.RunningSong;

        //    var payload = string.Empty;
        //    var value = e.Split(' ');
        //    switch (value[0])
        //    {
        //        case nameof(SongControl.PrevSong):
        //            this.PlayDifferentSong(current, -1);
        //            break;
        //        case nameof(SongControl.RunBack):
        //            payload = $"RunBackTime {ConfigurationViewModel.LENGTH_OF_TIME_SKIP}";
        //            break;
        //        case nameof(SongControl.Pause):
        //            payload = "TogglePause";
        //            break;
        //        case nameof(SongControl.RunForward):
        //            payload = $"RunForwardTime {ConfigurationViewModel.LENGTH_OF_TIME_SKIP}";
        //            break;
        //        case nameof(SongControl.NextSong):
        //            this.PlayDifferentSong(current, 1);
        //            break;
        //        //case nameof(SongControl.SelectionChanged):
        //        //    if (this.currentlySelectedSongIndex != "m" + value[1])
        //        //    {
        //        //        //SceneReporter.SendMessageToServer(Topics.SELECTION_TOPIC, "m" + value[1], this.myMqttClient);
        //        //    }
        //        //    break;
        //    }

        //    if (payload == string.Empty)
        //    {
        //        return;
        //    }

        //    ConfigurationViewModel.SendMessageToServer(ConfigurationViewModel.SONG_CONTROL_TOPIC_FROM_INTERFACE, payload, this.myMqttClient);
        //}

        private void EmergencyStop()
        {
            var topics = this.Scenes.SelectMany(x => x.Devices).Select(d => d.Topic).Distinct();
            this.myPublishQueue.Clear();
            foreach (var topic in topics)
            {
                ConfigurationViewModel.SendMessageToServer(topic, 0.ToString(), this.myMqttClient);
            }
        }
        private void Publish(string topic, string payload)
        {
            ConfigurationViewModel.SendMessageToServer(topic, payload, this.myMqttClient);
        }

        public void PublishDevicesToUI(List<Scene> scenes)
        {
            var devices = scenes.SelectMany(x => x.Devices).Select(device => device.Name).Distinct();
            var message = string.Join(";;;", devices);
            this.Publish(Topics.DEVICE_TOPIC, message);
        }

        public void PublishMusicNamesToUI(List<string> songList)
        {
            var message = string.Join(";;;", songList);
            this.Publish(Topics.SONG_TOPIC, message);
        }

        public void PublishSceneNamesToUI(List<Scene> scenes)
        {
            var message = string.Join(";;;", scenes.Select(scene => scene.Name));
            this.Publish(Topics.SCENE_CONFIGURATION_TOPIC, message);
        }

        public static async void SendMessageToServer(string topic, string payload, IMqttClient client)
        {
            var message = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(payload).WithExactlyOnceQoS().WithRetainFlag(false).Build();

            await client.PublishAsync(message, CancellationToken.None);
        }

        private void StartMonitoring()
        {
            var uri = new Uri($@"{AppDomain.CurrentDomain.BaseDirectory}{RELATIVE_PATH_CONFIGURATION}");
            var fileSystemWatcherConfiguration = new FileSystemWatcher(uri.AbsolutePath);
            fileSystemWatcherConfiguration.Changed += (sender, args) => { this.EnqueueControl(SceneControl.ScenesChanged.ToString()); };
            fileSystemWatcherConfiguration.EnableRaisingEvents = true;

            //Currently not working
            //uri = new Uri($@"{AppDomain.CurrentDomain.BaseDirectory}{RELATIVE_PATH_SONGS}");
            //var fileSystemWatcherSongs = new FileSystemWatcher(uri.AbsolutePath);
            //fileSystemWatcherSongs.Changed += (sender, args) => { this.HandleSceneControlEvent(sender, Payloads.SCENES_CHANGED_PAYLOAD); };
            //fileSystemWatcherSongs.EnableRaisingEvents = true;
        }

        private async Task ConnectClient()
        {
            var options = new MqttClientOptionsBuilder().WithTcpServer(ConfigurationViewModel.SERVER_ADDRESS, 1883).Build();
            await this.myMqttClient.ConnectAsync(options, CancellationToken.None);
        }
        private void ChangeSceneSelection(string value)
        {
            if (!int.TryParse(value, out int index))
            {
                return;
            }
            if (this.Scenes.IndexOf(this.SelectedScene) == index)
            {
                return;
            }
            this.SelectedScene = this.Scenes.ElementAt(index);
        }

        private void NextScene()
        {
            // this.StopMusic();

            if (this.SelectedScene.Equals(this.Scenes.Last()))
            {
                return;
            }

            this.SelectedScene = this.Scenes.ElementAt(this.Scenes.ToList().IndexOf(this.SelectedScene) + 1);

            // this.PlayScene();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void PlayDifferentSong(string currentSong, int toSkip)
        {
            var index = this.songList.IndexOf(currentSong);
            if (index < 0)
            {
                return; //?
            }
            //if (index + toSkip > this.songList.Count || index + toSkip < 0)
            //{
            //    return;
            //}

            if ((index == 0 && toSkip < 0) || (index == this.songList.Count - 1 && toSkip > 0))
            {
                return;
            }

            this.StopMusic();

            var song = this.songList.ElementAt(this.songList.IndexOf(currentSong) + toSkip);
            ConfigurationViewModel.SendMessageToServer(Topics.MUSIC_TOPIC, song, this.myMqttClient);
            this.mySceneDurationCancellationTokenSource = new CancellationTokenSource();
            this.RunningSong = song;
        }

        private void PlayScene()
        {
            this.StopMusic();

            foreach (var device in this.SelectedScene.Devices)
            {
                if (device.Topic == Topics.MUSIC_TOPIC && !this.SelectedScene.Duration.Equals(default))
                {
                    this.StartSceneDuration();
                    ConfigurationViewModel.SendMessageToServer(device.Topic, device.Value.ToString(), this.myMqttClient);
                    this.RunningSong = device.Value.ToString();
                    continue;
                }

                ConfigurationViewModel.SendMessageToServer(device.Topic, device.Value.ToString(), this.myMqttClient);
            }
        }

        private void PreviousScene()
        {
            // this.StopMusic();

            if (this.SelectedScene.Equals(this.Scenes.First()))
            {
                return;
            }

            this.SelectedScene = this.Scenes.ElementAt(this.Scenes.ToList().IndexOf(this.SelectedScene) - 1);

            // this.PlayScene();
        }

        private void Publish(TimeSpan timeSpan)
        {
            while (true)
            {
                if (this.myPublishQueue.Count > 0)
                {
                    (this.myPublishQueue.Dequeue())();
                }

                Thread.Sleep(timeSpan);
            }
        }

        private Task SetupClient()
        {
            var factory = new MqttFactory();
            this.myMqttClient = factory.CreateMqttClient();
            return Task.Run(async () => await this.ConnectClient());
        }

        private void StartSceneDuration()
        {
            this.mySceneDurationCancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(
                () =>
                {
                    var watch = Stopwatch.StartNew();
                    var duration = this.SelectedScene.Duration;
                    while (watch.Elapsed.Seconds < duration && !this.mySceneDurationCancellationTokenSource.IsCancellationRequested)
                    {
                    }

                    watch.Stop();
                    this.mySceneDurationCancellationTokenSource = null;
                },
                this.mySceneDurationCancellationTokenSource.Token);
        }

        private void StopMusic()
        {
            this.mySceneDurationCancellationTokenSource?.Cancel();
            ConfigurationViewModel.SendMessageToServer(Topics.MUSIC_TOPIC, Payloads.MUSIC_STOP_PAYLOAD, this.myMqttClient);
            this.RunningSong = string.Empty;
        }

        private void UpdateUI()
        {
            this.Scenes = new ObservableCollection<Scene>();
            var scenes = FileReadHelper.ReadScenesWithDevices();
            this.Scenes = new ObservableCollection<Scene>(scenes);

            var songs = FileReadHelper.ReadSongNames();
            this.songList = songs;

            this.SelectedScene = this.Scenes.First();
            this.PublishMusicNamesToUI(songs);
            this.PublishSceneNamesToUI(scenes);
            this.PublishDevicesToUI(scenes);
        }

        private void UpdateUISelection(string payload)
        {
            ConfigurationViewModel.SendMessageToServer(Topics.SELECTION_TOPIC, payload, this.myMqttClient);
        }

        #endregion
    }
}
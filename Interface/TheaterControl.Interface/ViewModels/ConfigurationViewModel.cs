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
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using MQTTnet;
    using MQTTnet.Client;
    using MQTTnet.Client.Options;
    using TheaterControl.Interface.Annotations;
    using TheaterControl.Interface.Constants;
    using TheaterControl.Interface.Helper;
    using TheaterControl.Interface.Models;

    internal class ConfigurationViewModel : INotifyPropertyChanged
    {
        #region Fields

        private const double LENGTH_OF_TIME_SKIP = 5.0;

        private IMqttClient myMqttClient;

        private readonly Queue<Action> myPublishQueue;

        private string myRunningSong;

        private CancellationTokenSource mySceneDurationCancellationTokenSource;

        private readonly SceneReporter mySceneReporter;

        private ObservableCollection<Scene> myScenes;

        private Scene mySelectedScene;

        private const string SERVER_ADDRESS = "linvm2416";

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
                this.UpdateUISelection(Payloads.SONG_SELECTION_CHANGED + value);
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
                    this.UpdateUISelection(Payloads.SCENE_SELECTION_CHANGED + value.Id);
                }
            }
        }

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

            this.mySceneReporter = new SceneReporter(this.myMqttClient);
            this.mySceneReporter.SceneControlEvent += this.HandleSceneControlEvent;
            this.mySceneReporter.SongControlEvent += this.HandleSongControlEvent;

            clientSetupTask.Wait();
            this.UpdateUI();
            Task.Run(() => this.Publish(new TimeSpan(100)));
            this.SelectedScene = this.Scenes.First();
            this.RunningSong = this.SelectedScene.Devices.FirstOrDefault(x => x.Topic == Topics.MUSIC_TOPIC).Value.ToString() ?? string.Empty;
        }

        #endregion

        #region Methods

        private async Task ConnectClient()
        {
            var options = new MqttClientOptionsBuilder().WithTcpServer(ConfigurationViewModel.SERVER_ADDRESS, 1883).Build();
            await this.myMqttClient.ConnectAsync(options, CancellationToken.None);
        }

        private void EmergencyStop()
        {
            var topics = this.Scenes.SelectMany(x => x.Devices).Select(d => d.Topic).Distinct();
            this.myPublishQueue.Clear();
            foreach (var topic in topics)
            {
                SceneReporter.SendMessageToServer(topic, 0.ToString(), this.myMqttClient);
            }
        }

        private void EnqueueControl(string control)
        {
            switch (control)
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
            }
        }

        private void HandleSceneControlEvent(object sender, string e)
        {
            if (this.SelectedScene == null)
            {
                this.SelectedScene = this.Scenes[0];
            }

            var control = Enum.GetNames(typeof(SceneControl)).FirstOrDefault(x => x == e);
            if (control == null)
            {
                return;
            }

            this.EnqueueControl(control);
        }

        private void HandleSongControlEvent(object sender, string e)
        {
            var current = this.RunningSong;

            var payload = string.Empty;
            switch (e)
            {
                case nameof(SongControl.PrevSong):
                    this.PlayDifferentSong(current, -1);
                    break;
                case nameof(SongControl.RunBack):
                    payload = $"RunBackTime {ConfigurationViewModel.LENGTH_OF_TIME_SKIP}";
                    break;
                case nameof(SongControl.Pause):
                    payload = "TogglePause";
                    break;
                case nameof(SongControl.RunForward):
                    payload = $"RunForwardTime {ConfigurationViewModel.LENGTH_OF_TIME_SKIP}";
                    break;
                case nameof(SongControl.NextSong):
                    this.PlayDifferentSong(current, 1);
                    break;
            }

            if (payload == string.Empty)
            {
                return;
            }

            SceneReporter.SendMessageToServer(ConfigurationViewModel.SONG_CONTROL_TOPIC_FROM_INTERFACE, payload, this.myMqttClient);
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
                return;
            }

            if ((index == 0 && toSkip < 0) || (index == this.songList.IndexOf(this.songList.Last()) && toSkip > 0))
            {
                return;
            }

            this.StopMusic();

            var song = this.songList.ElementAt(this.songList.IndexOf(currentSong) + toSkip);
            SceneReporter.SendMessageToServer(Topics.MUSIC_TOPIC, song, this.myMqttClient);
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
                    SceneReporter.SendMessageToServer(device.Topic, device.Value.ToString(), this.myMqttClient);
                    this.RunningSong = device.Value.ToString();
                    continue;
                }

                SceneReporter.SendMessageToServer(device.Topic, device.Value.ToString(), this.myMqttClient);
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
            SceneReporter.SendMessageToServer(Topics.MUSIC_TOPIC, Payloads.MUSIC_STOP_PAYLOAD, this.myMqttClient);
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
            this.mySceneReporter.PublishMusicNamesToUI(songs);
            this.mySceneReporter.PublishSceneNamesToUI(scenes);
            this.mySceneReporter.PublishDevicesToUI(scenes);
        }

        private void UpdateUISelection(string payload)
        {
            SceneReporter.SendMessageToServer(Topics.SELECTION_TOPIC, payload, this.myMqttClient);
        }

        #endregion
    }
}
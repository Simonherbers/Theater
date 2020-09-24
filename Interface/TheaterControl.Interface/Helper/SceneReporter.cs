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
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using MQTTnet;
    using MQTTnet.Client;
    using MQTTnet.Client.Subscribing;
    using TheaterControl.Interface.Constants;
    using TheaterControl.Interface.Models;

    internal class SceneReporter
    {
        #region Fields

        private ASCIIEncoding enc = new ASCIIEncoding();

        private IMqttClient mqttClient;

        private const string RELATIVE_PATH_CONFIGURATION = @"\..\..\..\Configuration\";

        private const string RELATIVE_PATH_SONGS = @"\..\..\..\..\TheaterControl.MusicPlayer\Music\";

        #endregion

        #region Events

        public event EventHandler<string> SceneControlEvent;

        public event EventHandler<string> SongControlEvent;

        #endregion

        #region Constructors

        public SceneReporter(IMqttClient client)
        {
            this.mqttClient = client;
            this.mqttClient.UseConnectedHandler(
                async e =>
                {
                    await mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(Topics.SCENE_CONFIGURATION_TOPIC).Build());
                    await mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(Topics.SCENE_CONTROL_TOPIC).Build());
                    await mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(Topics.SONG_CONTROL_TOPIC_FROM_UI).Build());
                    await mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(Topics.SELECTION_TOPIC).Build());
                });
            this.mqttClient.UseApplicationMessageReceivedHandler(this.HandleMessage);
            this.StartMonitoring();
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
                this.SongControlEvent?.Invoke(this, payload);
                return;
            }
            if (e.ApplicationMessage.Topic == Topics.SELECTION_TOPIC && payload.StartsWith("s"))
            {
                this.SceneControlEvent?.Invoke(this, payload);
                return;
            }
            if (e.ApplicationMessage.Topic == Topics.SELECTION_TOPIC && payload.StartsWith("m"))
            {
                this.SongControlEvent?.Invoke(this, SongControl.SelectionChanged.ToString() + " " + payload.Substring(1));
                return;
            }

            this.SceneControlEvent?.Invoke(this, payload);
        }

        private void Publish(string topic, string payload)
        {
            SceneReporter.SendMessageToServer(topic, payload, this.mqttClient);
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
            var uri = new Uri($@"{AppDomain.CurrentDomain.BaseDirectory}{SceneReporter.RELATIVE_PATH_CONFIGURATION}");
            var fileSystemWatcherConfiguration = new FileSystemWatcher(uri.AbsolutePath);
            fileSystemWatcherConfiguration.Changed += (sender, args) => { this.SceneControlEvent?.Invoke(sender, Payloads.SCENES_CHANGED_PAYLOAD); };
            fileSystemWatcherConfiguration.EnableRaisingEvents = true;

            uri = new Uri($@"{AppDomain.CurrentDomain.BaseDirectory}{SceneReporter.RELATIVE_PATH_SONGS}");
            var fileSystemWatcherSongs = new FileSystemWatcher(uri.AbsolutePath);
            fileSystemWatcherSongs.Changed += (sender, args) => { this.SceneControlEvent?.Invoke(sender, Payloads.SCENES_CHANGED_PAYLOAD); };
            fileSystemWatcherSongs.EnableRaisingEvents = true;
        }

        #endregion
    }
}
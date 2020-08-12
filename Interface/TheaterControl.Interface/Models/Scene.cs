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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows.Input;
    using MQTTnet.Client;
    using TheaterControl.Interface.Annotations;
    using TheaterControl.Interface.Helper;

    internal class Scene : INotifyPropertyChanged
    {
        #region Fields

        //private readonly List<string> FILE_ENDINGS = new List<string> { ".png", ".jpg", ".svg" };

        //private const string IMAGE_RESOURCES_FOLDER_RELATIVE_PATH = "../../Resources/";

        private ObservableCollection<Device> myDevices;
        public int Id { get; set; }
        #endregion

        #region Properties

        //public ICommand AddDeviceCommand { get; set; }

        public ObservableCollection<Device> Devices
        {
            get => this.myDevices;
            set
            {
                this.myDevices = value;
                this.OnPropertyChanged();
            }
        }

        public string Name { get; set; }

        #endregion
        public double Duration { get; set; }
        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Constructors

        public Scene()
        {
            this.Devices = new ObservableCollection<Device>();
        }

        #endregion

        #region Methods

        //public void AddDeviceExecute(object sender)
        //{
        //    if (this.SelectedDeviceType == null)
        //    {
        //        return;
        //    }

        //    var deviceObject = this.CreateInstanceOfSelectedIDeviceType();
        //    this.Devices.Add(deviceObject);
        //    this.DeviceWasAddedEvent?.Invoke(this, deviceObject);
        //    StatusReporter.RequestConnectionStatus(deviceObject);
        //}

        //private IDevice CreateInstanceOfSelectedIDeviceType()
        //{
        //    var deviceObject = (IDevice)Activator.CreateInstance(this.SelectedDeviceType);
        //    deviceObject.Id = Enumerable.Range(1, this.Devices.Count(x => x.GetType() == this.SelectedDeviceType) + 1) //
        //        .TakeWhile(
        //            x => this.Devices.Where(y => y.GetType() == this.SelectedDeviceType) //
        //                .Any(y => y.Id == x)).Count() + 1;
        //    deviceObject.Topic += deviceObject.Id;
        //    deviceObject.DeviceImageUri = this.TryGetDeviceImageUri();
        //    deviceObject.MqttClient = this.MqttClient;
        //    return deviceObject;
        //}

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //private void PlaySceneExecute(object sender)
        //{
        //    foreach (var device in this.Devices)
        //    {
        //        device.PublishValue();
        //    }
        //}

        //private void RemoveDeviceExecute(object sender)
        //{
        //    var device = (IDevice)sender;
        //    this.Devices.Remove(device);
        //}

        //private Uri TryGetDeviceImageUri()
        //{
        //    var fileEnding = this.FILE_ENDINGS.Find(
        //        x => File.Exists(
        //            AppDomain.CurrentDomain.BaseDirectory
        //            + $"{Scene.IMAGE_RESOURCES_FOLDER_RELATIVE_PATH}{this.SelectedDeviceType.FullName.Split('.').Last()}{x}"));
        //    if (fileEnding != null)
        //    {
        //        return new Uri(
        //            AppDomain.CurrentDomain.BaseDirectory
        //            + $"{Scene.IMAGE_RESOURCES_FOLDER_RELATIVE_PATH}{this.SelectedDeviceType.FullName.Split('.').Last()}{fileEnding}");
        //    }

        //    return null;
        //}

        #endregion
    }
}
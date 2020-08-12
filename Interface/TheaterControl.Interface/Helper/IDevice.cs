using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheaterControl.Interface.Helper
{
    using System.Drawing;
    using System.Windows.Input;
    using MQTTnet.Client;
    using TheaterControl.Interface.Models;


    //currently only used within StatusReporter and device classes
    internal interface IDevice
    {
        string Topic { get; set; }
        object Value { get; set; }
        int Id { get; set; }
        bool ConnectionStatus { get; set; }
        IMqttClient MqttClient { get; set; }
        ICommand PublishCommand { get; set; }
        Uri DeviceImageUri { get; set; }
        void PublishValue();
        string Name { get; set; }
    }
}

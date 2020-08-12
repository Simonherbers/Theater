using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheaterControl.Interface.Helper
{
    using System.Windows.Input;
    using MQTTnet.Client;

    internal class Device
    {
        public string Topic { get; set; }
        public object Value { get; set; }
        public int Id { get; set; }
        public bool ConnectionStatus { get; set; }
        public ICommand PublishCommand { get; set; }
        public Uri DeviceImageUri { get; set; }
        public string Name { get; set; }
    }
}

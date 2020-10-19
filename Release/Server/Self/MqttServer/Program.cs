using System;

namespace MqttServer
{
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using MQTTnet;
    using MQTTnet.Protocol;
    using MQTTnet.Server;

    internal class Program
    {
        public static void Main(string[] args)
        {
            Begin();
            Console.ReadLine();
        }

        private static async Task Begin()
        {
            var options = new MqttServerOptionsBuilder().WithConnectionBacklog(100).WithDefaultEndpointPort(1883).WithConnectionValidator(c =>
            {
                Console.WriteLine("Attempt");
                if (c.ClientId.Length < 10)
                {
                    c.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
                    return;
                }
                Console.WriteLine("Connection" + c.ClientId);
                c.ReasonCode = MqttConnectReasonCode.Success;
            }).Build();
            // Start a MQTT server.
            var mqttServer = new MqttFactory().CreateMqttServer();
            await mqttServer.StartAsync(options);
            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
            await mqttServer.StopAsync();
        }
    }
}

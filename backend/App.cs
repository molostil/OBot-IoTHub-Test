using System;
using Microsoft.Azure.EventHubs;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using Microsoft.Azure.Devices;

namespace read_d2c_messages
{
    class ReadDeviceToCloudMessages
    {
        // needed for listening to events from IoTHub
        private readonly static string s_eventHubsCompatibleEndpoint = "sb://ihsuprodamres038dednamespace.servicebus.windows.net/";
        private readonly static string s_eventHubsCompatiblePath = "iothub-ehub-sweetiothu-1987978-2281450f73";
        private readonly static string s_iotHubSasKey = "zVE2nXhSCeFQMd2bP37Za74/HkrI6NCTrCSD7a00d+0=";
        private readonly static string s_iotHubSasKeyName = "service";
        private static EventHubClient s_eventHubClient;

        // needed for sending messages to IoTHub
        private static ServiceClient _serviceClient;
        private static string _deviceId = "MyDevice";
        private static string _hubConnectionString =
            "HostName=SweetIoTHub.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=zVE2nXhSCeFQMd2bP37Za74/HkrI6NCTrCSD7a00d+0=";


        private static async Task Main(string[] args)
        {
            RecieveMessages();
            
            _serviceClient = ServiceClient.CreateFromConnectionString(_hubConnectionString);
            while (true)
            {
                Console.WriteLine("Press any key to send a C2D message.");
                Console.ReadLine();
                await SendCloudToDeviceMessageAsync();
            }
        }
        
        private async static Task SendCloudToDeviceMessageAsync()
        {
            var commandMessage = new
                Message(Encoding.ASCII.GetBytes("Stop harrassing me you little, s**t!"));
            await _serviceClient.SendAsync(_deviceId, commandMessage);
        }

        private static async Task RecieveMessages()
        {
            var connectionString = new EventHubsConnectionStringBuilder(new Uri(s_eventHubsCompatibleEndpoint),
                s_eventHubsCompatiblePath, s_iotHubSasKeyName, s_iotHubSasKey);
            s_eventHubClient = EventHubClient.CreateFromConnectionString(connectionString.ToString());
            
            var runtimeInfo = await s_eventHubClient.GetRuntimeInformationAsync();
            var d2cPartitions = runtimeInfo.PartitionIds;

            CancellationTokenSource cts = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };

            var tasks = new List<Task>();
            foreach (string partition in d2cPartitions)
            {
                tasks.Add(ReceiveMessagesFromDeviceAsync(partition, cts.Token));
            }

            // Wait for all the PartitionReceivers to finsih.
            Task.WaitAll(tasks.ToArray());
        }
        
        // Asynchronously create a PartitionReceiver for a partition and then start 
        // reading any messages sent from the simulated client.
        private static async Task ReceiveMessagesFromDeviceAsync(string partition, CancellationToken ct)
        {
            // Create the receiver using the default consumer group.
            // For the purposes of this sample, read only messages sent since 
            // the time the receiver is created. Typically, you don't want to skip any messages.
            var eventHubReceiver = s_eventHubClient.CreateReceiver("$Default", partition, EventPosition.FromEnqueuedTime(DateTime.Now));
            while (true)
            {
                if (ct.IsCancellationRequested) break;
                Console.WriteLine("Listening for messages on: " + partition);
                // Check for EventData - this methods times out if there is nothing to retrieve.
                var events = await eventHubReceiver.ReceiveAsync(100);

                // If there is data in the batch, process it.
                if (events == null) continue;

                foreach(EventData eventData in events)
                { 
                  string data = Encoding.UTF8.GetString(eventData.Body.Array);
                  Console.WriteLine("Message received on partition {0}:", partition);
                  Console.WriteLine("  {0}:", data);
                  Console.WriteLine("Application properties (set by device):");
                  foreach (var prop in eventData.Properties)
                  {
                    Console.WriteLine("  {0}: {1}", prop.Key, prop.Value);
                  }
                  Console.WriteLine("System properties (set by IoT Hub):");
                  foreach (var prop in eventData.SystemProperties)
                  {
                    Console.WriteLine("  {0}: {1}", prop.Key, prop.Value);
                  }
                }
            }
        }
    }
}

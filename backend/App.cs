using System;
using Microsoft.Azure.EventHubs;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;


namespace read_d2c_messages
{
    class ReadDeviceToCloudMessages
    {
        // needed list for creating all rooms/devices
        private static List <Twin> _roomDeviceTwins;
        private static RegistryManager _registryManager;

        // needed for listening to events from IoTHubdotn
        private readonly static string s_eventHubsCompatibleEndpoint =
            "sb://ihsuprodamres038dednamespace.servicebus.windows.net/";

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
            _registryManager =  RegistryManager.CreateFromConnectionString(_hubConnectionString);
            _serviceClient = ServiceClient.CreateFromConnectionString(_hubConnectionString);
            ReceiveFeedbackAsync();
            

            await SetUp();
            
            foreach (var device in _roomDeviceTwins)
            {
                Console.WriteLine("Gerät: ");
                Console.WriteLine(device.DeviceId);
            }


            RecieveMessages();

            while (true)
            {
                Console.WriteLine("Type 'm' for Message. Type 'd' for direct method call.");
                var input = Console.ReadLine();
                if (input.Equals("m"))
                {
                    SendCloudToDeviceMessageAsync();
                }

                if (input.Equals("d"))
                {
                    SendMethodDirectly();
                }
            }
        }
        

        private async static Task SetUp()
        {    
            var query = _registryManager.CreateQuery("SELECT * FROM devices");
            Console.WriteLine("Devices: ");
            _roomDeviceTwins = new List<Twin>();
            while (query.HasMoreResults)
            {
                
                var twins = await query.GetNextAsTwinAsync();
                foreach (var twin in twins)
                {
                    _roomDeviceTwins.Add(twin);
                }

            }
        }


        private async static Task SendCloudToDeviceMessageAsync()
        {
            var commandMessage = new
                Message(Encoding.ASCII.GetBytes("Stop harrassing me you little, s**t!"));
            
            // demand feedback on the delivery of the message 
            commandMessage.Ack = DeliveryAcknowledgement.Full;
            await _serviceClient.SendAsync(_deviceId, commandMessage);

        }

        private class Payload
        {
            public string Message { get; set; }
        }
        
        private static async Task SendMethodDirectly()
        {
            var p = new Payload
            {
                Message = "Do Something!"
            };
            var json = JsonConvert.SerializeObject(p);
            var m = new CloudToDeviceMethod("myMethod");
            m.SetPayloadJson(json);
            
            var result = await _serviceClient.InvokeDeviceMethodAsync(_deviceId, m);
            
            Console.WriteLine("-------- Response --------");
            if (result.Status == 200)
            {
                Console.WriteLine("Status Code 200! Yeah!");    
            }
            else if( result.Status == 325)
            {
                Console.WriteLine("Satus Code 325! Oh No! (don't worry this is not a real error, the simulated device was written this way!!)");
            }
            else
            {
                Console.WriteLine($"Satus Code {result.Status}? Okay, this is a real error!");
            }
            Console.WriteLine($"Payload:  {result.GetPayloadAsJson()}");
            Console.WriteLine("--------------------------");
        }
        
        private static async void ReceiveFeedbackAsync()
        {
            var feedbackReceiver = _serviceClient.GetFeedbackReceiver();

            Console.WriteLine("\nReceiving c2d feedback from service");
            while (true)
            {
                var feedbackBatch = await feedbackReceiver.ReceiveAsync();
                if (feedbackBatch == null) continue;

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Received feedback: {0}",
                    string.Join(", ", feedbackBatch.Records.Select(f => f.StatusCode)));
                Console.ResetColor();

                await feedbackReceiver.CompleteAsync(feedbackBatch);
            }
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
            var eventHubReceiver =
                s_eventHubClient.CreateReceiver("$Default", partition, EventPosition.FromEnqueuedTime(DateTime.Now));
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
                    Console.WriteLine("-------- Message Recieved --------");
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
                    Console.WriteLine("----------------------------------");
                }
            }
        }
    }
}

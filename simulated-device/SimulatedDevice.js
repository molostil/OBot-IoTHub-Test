// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


// The device connection string to authenticate the device with your IoT hub.
//
// NOTE:
// For simplicity, this sample sets the connection string in code.
// In a production environment, the recommended approach is to use
// an environment variable to make it available to your application
// or use an HSM or an x509 certificate.
// https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-security
//
// Using the Azure CLI:
// az iot hub device-identity show-connection-string --hub-name {YourIoTHubName} --device-id MyNodeDevice --output table

var connectionString = 'HostName=SweetIoTHub.azure-devices.net;DeviceId=MyDevice;SharedAccessKey=G+vr93WOIYyJQxMf39QEg5dKGZWwIiqP907KtODGyAs=';

// Using the Node.js Device SDK for IoT Hub:
//   https://github.com/Azure/azure-iot-sdk-node
// The sample connects to a device-specific MQTT endpoint on your IoT Hub.
var Mqtt = require('azure-iot-device-mqtt').Mqtt;
var DeviceClient = require('azure-iot-device').Client
var Message = require('azure-iot-device').Message;
var client = DeviceClient.fromConnectionString(connectionString, Mqtt);

registerListener()
registerMethodListener()
setMessageInterval()

// listen to incoming messages from the cloud
function registerListener(err) {
  if (err) {
    console.log('Could not connect: ' + err);
  } else {
    console.log('Client connected');
    client.on('message', function (msg) {
      console.log("-------- Message recieved --------");
      console.log('Id: ' + msg.messageId + ' Body: ' + msg.data);
      client.complete(msg, console.log('completed'));
      console.log("----------------------------------");
    });
  }
}

function registerMethodListener()
{
  client.onDeviceMethod('myMethod', (request, response) => {
    console.log("-------- myMethod --------");
    console.log('myMethod was called!')
    console.log(request)
    console.log("--------------------------");
    const r = Math.random()
    if(r > 0.5)
    {
      response.send(200, {Cool: "Awesome", Yeah: "Sweet"})
    } else {
      response.send(325, {OhNo: "WhaAAaat", Damn: "So sad!"})
    }
  })
}


  // send a message to the cloud every 2 seconds
function setMessageInterval(){
  setInterval(function(){
    var temperature = 20 + (Math.random() * 15);
    var humidity = 60 + (Math.random() * 20);
    var data = JSON.stringify({ deviceId: 'MyDevice', message: 'Hi Cloud, how are you?'});
    var message = new Message(data);
    message.properties.add('CustomProperty', 'true');
    console.log("Sending message: " + message.getData());
    client.sendEvent(message, function (err) {
      if (err) {
        console.error('send error: ' + err.toString());
      } else {}
    });
  }, 10000);
}

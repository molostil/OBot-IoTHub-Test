# OBot-IoTHub-Test

Minimal Project showing two-way communication of a Device and the Cloud of an Azure IoTHub.
To see the action, just start the .Net project in backend and simultaneously run the Node project.

On Node side a message and a method listener is registered while in a setInterval loop messages are sent to the IoTHub every ten seconds.

The .Net 'backend' also registers an event listener.
Additionally a thread is started, waiting for the user to press a key. When the user type "m" (+return) a message is sent to the device. When the user types "d" (+return) a [direct method](https://docs.microsoft.com/de-de/azure/iot-hub/iot-hub-devguide-direct-methods) is called on the device and a response is sent back to the cloud/backend.

The messages and responses are displayed in the consoles of the respective program.

# OBot-IoTHub-Test

Minimal Project showing two-way communication of a Device and the Cloud of an Azure IoTHub.
To see the action, just start the .Net project in backend and simultaneously run the Node project.
On Node side a message listener is registered while in a setInterval loop messages are sent to the IoTHub every two seconds.
The .Net 'backend' also registers an event listener. Also a thread is started,
waiting for the user to press a key, sending a message to the device.
The messages are displayed in the consoles of the respective program.

# MqttTimerFunction
 
This is a simple serverless Azure Function running 24/7 on the cloud like Windows Service in the old days. It has two timers triggered by specific times using CRON expressions. The function sends MQTT messages to a HiveMQ broker and collaborates with another project of mine that utilizes Raspberry Pi PicoW:

https://github.com/DIY-able/MqttTinyController  

In a real-life scenario, I am using this Azure Function to:

- Turn On a device (GPIO 16) on Rraspberry Pi PicoW at 8:30 AM
- Turn Off a device (GPIO 16) on Rraspberry Pi PicoW at 20:30 PM
- Turn On garden lighting (GPIO 17) on Rraspberry Pi PicoW at 17:00 PM
- Turn Off garden lighting (GPIO 17) on Rraspberry Pi PicoW at 23:30 PM
 
The code is based on sample code from MQTTnet:

https://github.com/dotnet/MQTTnet

- IDE:  Visual Studio 2022 Professional (You can use VS Code)
- MQTTnet: v4.3.3
- Runtime .NET 8

# MqttTimerFunction
 
This sample code is a serverless Azure Function running 24/7 on the cloud similar to traditional Windows Service on VM. It has 4 timers triggered by specific times using CRON expressions, the function sends MQTT messages to a HiveMQ broker. This collaborates with another project of mine "MqttTinyController" that utilizes Raspberry Pi PicoW to control relay for light switches and other devices:

https://github.com/DIY-able/MqttTinyController  

In a real-life scenario, I am using this Azure Function to:

- Turn On a device (GPIO 16) on Rraspberry Pi PicoW at 8:30 AM
- Turn Off a device (GPIO 16) on Rraspberry Pi PicoW at 20:30 PM
- Turn On garden lighting (GPIO 17) on Rraspberry Pi PicoW at 17:00 PM
- Turn Off garden lighting (GPIO 17) on Rraspberry Pi PicoW at 23:30 PM
- Turn On a device (GPIO 26) when Android App "Tasker" sends a HTTP Response /api/Http1Send?code=secret_key 
- Turn On a device (GPIO 27) when Android App "Tasker" sends a HTTP Response /api/Http2Send?code=secret_key 
Note: For Http Trigger, secret_key is defined in Function Key section. Alternatively, use x-functions-key:secret_key in HTTP Header

With MFA support (TOTP) on latest code in MqttTinyController, we are sending e.g. {"MFA":473510} 6 digit TOTP based (UTC time) one time password using the totp_key defined in config. MFA is sent before the GPIO value change request {"GP17": 0}, this can prevent unauthorized access even MQTT broker account got compromised. Also, the specific key running on Azure Function is configured to have access on GPIO 16 and GPIO 17 only (controlled by the PicoW). Even someone has access to the key, they cannot access other GPIOs. 


The code is based on sample code from MQTTnet:

https://github.com/dotnet/MQTTnet

- IDE:  Visual Studio 2022 Professional (You can use VS Code)
- MQTTnet: v4.3.3
- Runtime .NET 8


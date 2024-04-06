# MqttTimerFunction
 
This sample code is a serverless Azure Function running 24/7 on the cloud similar to traditional Windows Service on VM. It has 4 timers triggered by specific times using CRON expressions, the function sends MQTT messages to a HiveMQ broker. This collaborates with another project of mine "MqttTinyController" that utilizes Raspberry Pi PicoW to control relay for light switches and other devices:

https://github.com/DIY-able/MqttTinyController  

In a real-life scenario, I am using this Azure Function to:

- Turn On a device (GPIO 16) on Rraspberry Pi PicoW at 8:30 AM
- Turn Off a device (GPIO 16) on Rraspberry Pi PicoW at 20:30 PM
- Turn On garden lighting (GPIO 17) on Rraspberry Pi PicoW at 17:00 PM
- Turn Off garden lighting (GPIO 17) on Rraspberry Pi PicoW at 23:30 PM
- Turn On/Off 2 relays (GPIO 26 and 27) when Android App "Tasker" sends a HTTP Response /api/Http1Send?code=secret_key 
- Turn Off a device (GPIO 16) when Android App "Tasker" sends a HTTP Response /api/Http2Send?code=secret_key

# Notes
- For Http Trigger, secret_key is defined in Function Key section. Alternatively, use x-functions-key:secret_key in HTTP Header. With MFA support (TOTP) on latest code in MqttTinyController, we are sending e.g. {"MFA":473510} 6 digit TOTP based (UTC time) one time password using the totp_key defined in config. MFA is sent before the GPIO value change request {"GP17": 0}, this can prevent unauthorized access even MQTT broker account got compromised. Also, the specific key running on Azure Function is configured to have access on GPIO 16 and GPIO 17 only (controlled by the PicoW). Even someone has access to the key, they cannot access other GPIOs. 

- MqttTimerFunction is NOT an Azure Durable Function and it is STATELESS. The constructor is called EVERY TIME when there is a trigger (either Timer or Http). It is by design from Microsoft. If you need to pass variable between calls, you need to re-write the code as STATEFUL Durable Function.

# Cost

Running Azure Function with such low volume doesn't cost a lot since the basic tier includes 1 million requests. With "Pay as you go" plan, all you need is to pay for Azure Blob Storage. Running a MQTT Timer function like this costs me around $0.02 US a day (or $0.6 US a month).

# MQTTnet

The MQTT part of the code is based on sample from MQTTnet:

# Compile and Runtime

https://github.com/dotnet/MQTTnet

- IDE:  Visual Studio 2022 Professional (You can use VS Code)
- MQTTnet: v4.3.3
- Runtime .NET 8

#  Azure Portal deployment notes:

1. Create Function App (Consumption plan)
2. Create Storage Account (only Blog and File are needed, Table and Query are not needed)
3. In Function App > "Configuration" or "Environment variables" section
4. Create the custom values above
5. Create a variable to set the time zone, otherwise CRON is in UTC
    - WEBSITE_TIME_ZONE : Eastern Standard Time
6. Get the ConnectionString from Storage Account > Access Keys, create variables:
    - AzureWebJobsStorage: DefaultEndpointsProtocol=https;AccountName=xxxxxx
    - WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: DefaultEndpointsProtocol=https;AccountName=xxxxxx
    - WEBSITE_CONTENTSHARE: storage123456  (make up a name)
    - Note: AzureWebJobsStorage and WEBSITE_CONTENTAZUREFILECONNECTIONSTRING have the same value.
7. Function App > Get Publishing Profile (download it)
8. Deploy from Visual Studio using the profile
9. Start Function App
10. storage123456 will get created in Storage account > File shares:
    - azure-webjobs-hosts will get created in Storage account > Containers
11. Security: Function App > Networking, disable public access, and add your HOME IP only.
    - Storage account > Networking, leave public access opened (unfortunately). 
    - Ideally, for Enterprise we should connect to Function App to Storage Account by virtual network.
    - However, Consumption plan, it doesn't support, even adding all Function App outbound IPs to Storage account doesn't always work. 
12. Notes:
    - HTTP Trigger doesn't need Storage Account
    - Timer Trigger needs "AzureWebJobsStorage" parameter and you will get a warning. But everything still works okay.
    - Proper way is to setup using all 3 settings: AzureWebJobsStorage, WEBSITE_CONTENTSHARE and WEBSITE_CONTENTAZUREFILECONNECTIONSTRING
    - If you get error:
        - 1 functions found (Custom) 
        - 0 functions loaded.
    - It's not working. Delete the storage account and do it again step by step. Reset the Publishing Profile and Deploy from VS again. 





using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using System.Text;
using MQTTnet.Diagnostics;
using System.Security.Authentication;
using Azure;
using Google.Protobuf;

namespace MqttTimerFunction
{
    public class MqttTimerFunction
    {

        private readonly ILogger _logger;

        private static string brokerURL = "";
        private static string clientID = "";
        private static string topic = "";
        private static string username = "";
        private static string password = "";

        private static string timer1GpioName = "";
        private static string timer2GpioName = "";

        public MqttTimerFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MqttTimerFunction>();
            try
            {
                brokerURL = Environment.GetEnvironmentVariable("MqttBrokerURL") ?? "";
                clientID = Environment.GetEnvironmentVariable("MqttBrokerClientID") ?? "";
                topic = Environment.GetEnvironmentVariable("MqttBrokerTopic") ?? "";
                username = Environment.GetEnvironmentVariable("MqttBrokerUsername") ?? "";
                password = Environment.GetEnvironmentVariable("MqttBrokerPassword") ?? "";

                timer1GpioName = Environment.GetEnvironmentVariable("Timer1GpioName") ?? "";
                timer2GpioName = Environment.GetEnvironmentVariable("Timer2GpioName") ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }

        [Function("Timer1Starts")]
        public void Timer1Starts([TimerTrigger("%Timer1CronON%", RunOnStartup = false)] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            if (myTimer.ScheduleStatus is not null)
            {                
                Task.Run(() => Publish_Topic("{\"" + timer1GpioName + "\": 1}"));
            }
        } // run

        [Function("Timer1Ends")]
        public void Timer1Ends([TimerTrigger("%Timer1CronOFF%", RunOnStartup = false)] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            if (myTimer.ScheduleStatus is not null)
            {
                Task.Run(() => Publish_Topic("{\"" + timer1GpioName + "\": 0}"));
            }
        } // run


        [Function("Timer2Starts")]
        public void Timer2Starts([TimerTrigger("%Timer2CronON%", RunOnStartup = false)] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            if (myTimer.ScheduleStatus is not null)
            {
                Task.Run(() => Publish_Topic("{\"" + timer2GpioName + "\": 1}"));
            }
        } // run

        [Function("Timer2Ends")]
        public void Timer2Ends([TimerTrigger("%Timer2CronOFF%", RunOnStartup = false)] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            if (myTimer.ScheduleStatus is not null)
            {
                Task.Run(() => Publish_Topic("{\"" + timer2GpioName + "\": 0}"));
            }
        } // run

        public static async Task Connect_Client()
        {
            /*
             * This sample creates a simple MQTT client and connects to a public broker.
             *
             * Always dispose the client when it is no longer used.
             * The default version of MQTT is 3.1.1.
             */

            var mqttFactory = new MqttFactory();

            using (var mqttClient = mqttFactory.CreateMqttClient())
            {
                // Use builder classes where possible in this project.
                var mqttClientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer(brokerURL, 8883)
                    .WithCredentials(username, password)
                    .WithTlsOptions(new MqttClientTlsOptions()
                    {
                        UseTls = true, // Is set by default to true, I guess...
                        SslProtocol = SslProtocols.Tls12, // TLS downgrade
                        AllowUntrustedCertificates = true, // Not sure if this is really needed...
                        IgnoreCertificateChainErrors = true, // Not sure if this is really needed...
                        IgnoreCertificateRevocationErrors = true, // Not sure if this is really needed...
                        CertificateValidationHandler = (w) => true // Not sure if this is really needed...
                    })
                    .WithCleanSession()
                    .Build();
                mqttClientOptions.ClientId = clientID;

                // This will throw an exception if the server is not available.
                // The result from this message returns additional data which was sent 
                // from the server. Please refer to the MQTT protocol specification for details.
                var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                Console.WriteLine("The MQTT client is connected.");

                //response.DumpToConsole();
                Console.WriteLine(response.ToString());

                // Send a clean disconnect to the server by calling _DisconnectAsync_. Without this the TCP connection
                // gets dropped and the server will handle this as a non clean disconnect (see MQTT spec for details).
                var mqttClientDisconnectOptions = mqttFactory.CreateClientDisconnectOptionsBuilder().Build();

                await mqttClient.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);
            }
        }


        public static async Task Publish_Topic(string payload)
        {
            ///*
            // * This sample pushes a simple application message including a topic and a payload.
            // *
            // * Always use builders where they exist. Builders (in this project) are designed to be
            // * backward compatible. Creating an _MqttApplicationMessage_ via its constructor is also
            // * supported but the class might change often in future releases where the builder does not
            // * or at least provides backward compatibility where possible.
            // */

            var mqttEventLogger = new MqttNetEventLogger("MyCustomLogger");

            mqttEventLogger.LogMessagePublished += (sender, args) =>
            {
                var output = new StringBuilder();
                output.AppendLine($">> [{args.LogMessage.Timestamp:O}] [{args.LogMessage.ThreadId}] [{args.LogMessage.Source}] [{args.LogMessage.Level}]: {args.LogMessage.Message}");
                if (args.LogMessage.Exception != null)
                {
                    output.AppendLine(args.LogMessage.Exception.ToString());
                }

                Console.Write(output);
            };

            var mqttFactory = new MqttFactory(mqttEventLogger);

            using (var mqttClient = mqttFactory.CreateMqttClient())
            {
                var mqttClientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer(brokerURL, 8883)
                    .WithCredentials(username, password)
                    .WithTlsOptions(new MqttClientTlsOptions()
                    {
                        UseTls = true, // Is set by default to true, I guess...
                        SslProtocol = SslProtocols.Tls12, // TLS downgrade
                        AllowUntrustedCertificates = true, // Not sure if this is really needed...
                        IgnoreCertificateChainErrors = true, // Not sure if this is really needed...
                        IgnoreCertificateRevocationErrors = true, // Not sure if this is really needed...
                        CertificateValidationHandler = (w) => true // Not sure if this is really needed...
                    })
                    .WithCleanSession()
                    .Build();
                mqttClientOptions.ClientId = clientID;
                mqttClient.InspectPacketAsync += OnInspectPackage;

                // This will throw an exception if the server is not available.
                // The result from this message returns additional data which was sent 
                // from the server. Please refer to the MQTT protocol specification for details.
                var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
                // response.DumpToConsole();
                Console.WriteLine(response.ToString());

                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(false)
                    .Build();

                await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
                await mqttClient.DisconnectAsync();
                Console.WriteLine("MQTT application message is published.");
            }
        }


        public static Task OnInspectPackage(InspectMqttPacketEventArgs eventArgs)
        {
            if (eventArgs.Direction == MqttPacketFlowDirection.Inbound)
            {
                Console.WriteLine($"IN: {Convert.ToBase64String(eventArgs.Buffer)}");
            }
            else
            {
                Console.WriteLine($"OUT: {Convert.ToBase64String(eventArgs.Buffer)}");
            }

           return Task.CompletedTask;
        }

        public static async Task Subscribe_Topic()
        {
            ///*
            // * This sample subscribes to a topic and processes the received message.
            // */

            var mqttFactory = new MqttFactory();

            using (var mqttClient = mqttFactory.CreateMqttClient())
            {
                var mqttClientOptions = new MqttClientOptionsBuilder()
                                  .WithTcpServer(brokerURL, 8883)
                                  .WithCredentials(username, password)
                                  .WithTlsOptions(new MqttClientTlsOptions()
                                  {
                                      UseTls = true, // Is set by default to true, I guess...
                                      SslProtocol = SslProtocols.Tls12, // TLS downgrade
                                      AllowUntrustedCertificates = true, // Not sure if this is really needed...
                                      IgnoreCertificateChainErrors = true, // Not sure if this is really needed...
                                      IgnoreCertificateRevocationErrors = true, // Not sure if this is really needed...
                                      CertificateValidationHandler = (w) => true // Not sure if this is really needed...
                                  })
                                  .WithCleanSession()
                                  .Build();
                mqttClientOptions.ClientId = clientID;

                // Setup message handling before connecting so that queued messages
                // are also handled properly. When there is no event handler attached all
                // received messages get lost.
                mqttClient.ApplicationMessageReceivedAsync += e =>
                {
                    Console.WriteLine("Received application message.");
                    //   e.DumpToConsole();
                    Console.WriteLine(e.ToString());

                    return Task.CompletedTask;
                };

                await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(
                        f =>
                        {
                            f.WithTopic(topic);
                        })
                    .Build();

                await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

                Console.WriteLine("MQTT client subscribed to topic.");

                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();
            }
        }


    } //class
} // ns

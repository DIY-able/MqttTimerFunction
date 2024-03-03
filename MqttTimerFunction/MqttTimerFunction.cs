using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using MQTTnet.Protocol;
using System.Text;
using MQTTnet.Diagnostics;
using System.Security.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


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
        private static string http1GpioName = "";
        private static string http2GpioName = "";
        private static string totpKey = "";
        

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
                http1GpioName = Environment.GetEnvironmentVariable("Http1GpioName") ?? "";
                http2GpioName = Environment.GetEnvironmentVariable("Http2GpioName") ?? "";

                totpKey = Environment.GetEnvironmentVariable("TotpKey") ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }


        public static int GetTotpCode()
        {
            TotpAuthenticationService totp = new TotpAuthenticationService(6, 30);   // 30 seconds
            int code = totp.GenerateCode(Encoding.ASCII.GetBytes(Base32Decode(totpKey)));

            return code;
        }

        [Function("Timer1Starts")]
        public void Timer1Starts([TimerTrigger("%Timer1CronON%", RunOnStartup = false)] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer1Starts trigger function executed at: {DateTime.Now}");

            if (myTimer.ScheduleStatus is not null)
            {                
                Task.Run(() => Publish_Topic("{\"MFA\":" + GetTotpCode() + "}", "{\"" + timer1GpioName + "\": 1}"));
            }
        } // run

        [Function("Timer1Ends")]
        public void Timer1Ends([TimerTrigger("%Timer1CronOFF%", RunOnStartup = false)] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer1Ends trigger function executed at: {DateTime.Now}");

            if (myTimer.ScheduleStatus is not null)
            {
                Task.Run(() => Publish_Topic("{\"MFA\":" + GetTotpCode() + "}", "{\"" + timer1GpioName + "\": 0}"));
            }
        } // run


        [Function("Timer2Starts")]
        public void Timer2Starts([TimerTrigger("%Timer2CronON%", RunOnStartup = false)] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer2Starts trigger function executed at: {DateTime.Now}");

            if (myTimer.ScheduleStatus is not null)
            {
                Task.Run(() => Publish_Topic("{\"MFA\":" + GetTotpCode() + "}", "{\"" + timer2GpioName + "\": 1}"));
            }
        } // run

        [Function("Timer2Ends")]
        public void Timer2Ends([TimerTrigger("%Timer2CronOFF%", RunOnStartup = false)] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer2Ends trigger function executed at: {DateTime.Now}");

            if (myTimer.ScheduleStatus is not null)
            {
                Task.Run(() => Publish_Topic("{\"MFA\":" + GetTotpCode() + "}", "{\"" + timer2GpioName + "\": 0}"));
            }
        } // run

        [Function("Http1Send")]
        public IActionResult Http1Send([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            _logger.LogInformation("C# Http1Send function processed a request.");

            Task.Run(() => Publish_Topic("{\"MFA\":" + GetTotpCode() + "}", "{\"" + http1GpioName + "\": 0}"));

            return new OkObjectResult("Send Mqtt triggered");
        }

        [Function("Http2Send")]
        public IActionResult Http2Send([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            _logger.LogInformation("C# Http2Send function processed a request.");

            Task.Run(() => Publish_Topic("{\"MFA\":" + GetTotpCode() + "}", "{\"" + http2GpioName + "\": 0}"));

            return new OkObjectResult("Send Mqtt triggered");
        }


        // -----------------------------------------------------------------------------------


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

        /// <summary>
        /// Publish to MQTT broker
        /// </summary>
        /// <param name="mfa"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public static async Task Publish_Topic(string mfa, string payload)
        {
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
                Console.WriteLine(response.ToString());

                // MFA message 
                var mfaMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(mfa)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(false)
                    .Build();

                // GPXX message
                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(false)
                    .Build();

                // Publish both messages with 5 seconds apart
                await mqttClient.PublishAsync(mfaMessage, CancellationToken.None);
                await Task.Delay(5000);
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


        /// <summary>
        ///  Decode base32 string
        /// </summary>
        /// <param name="base32String"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string Base32Decode(string base32String)
        {
            const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"; // Standard Base32 alphabet
            const int bitsPerChar = 5;

            var output = new byte[base32String.Length * bitsPerChar / 8];
            int outputIndex = 0;
            int bits = 0;
            int bitsRemaining = 0;

            foreach (char c in base32String)
            {
                int value = base32Chars.IndexOf(c);
                if (value < 0)
                    throw new ArgumentException($"Invalid character in Base32 string: '{c}'");

                bits |= value;
                bitsRemaining += bitsPerChar;
                if (bitsRemaining >= 8)
                {
                    output[outputIndex++] = (byte)(bits >> (bitsRemaining - 8));
                    bits &= (0xFF >> (8 - (bitsRemaining - 8)));
                    bitsRemaining -= 8;
                }
                bits <<= bitsPerChar;
            }

            return Encoding.UTF8.GetString(output);
        }


    } //class
} // ns

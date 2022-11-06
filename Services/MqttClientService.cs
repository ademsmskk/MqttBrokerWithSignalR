using Microsoft.AspNetCore.SignalR;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mqtt.Client.AspNetCore.Services
{
    public class MqttClientService : IMqttClientService
    {
        private IMqttClient mqttClient;
        private IMqttClientOptions options;
        private IHubContext<PayloadPublishHub> _hubContext;

        public MqttClientService(IMqttClientOptions options, IHubContext<PayloadPublishHub> hubContext)
        {
            this.options = options;
            mqttClient = new MqttFactory().CreateMqttClient();
            ConfigureMqttClient();
            _hubContext = hubContext;
        }

        private void ConfigureMqttClient()
        {
            mqttClient.ConnectedHandler = this;
            mqttClient.DisconnectedHandler = this;
            mqttClient.ApplicationMessageReceivedHandler = this;
        }

        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
           
          
            mqttClient.UseApplicationMessageReceivedHandler ( e => 
                {
                    var payload = e.ApplicationMessage?.Payload == null ? null : Encoding.UTF8.GetString(e.ApplicationMessage?.Payload);
                
                    Console.WriteLine($"Received application message:{payload}");

                    _hubContext.Clients.All.SendAsync("receivePayloadMessage",payload);

                    return Task.CompletedTask;
                });
            return Task.Delay(20);
        }

        List<string> topiclist = new List<string>();
        public  void addTopics(string topic)
        {
           
           topiclist.Add(topic);
            
        }
        public  List<string> getTopics()
        {
            return topiclist;

        }

        public async Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            System.Console.WriteLine("connected");
            // for subscribe multiple topics
            // birden fazla topic kullanmak için dinamik bir yapı olmalı . Db ye kaydedip liste ye atayıp foreach ile dönülebilir. 

            await mqttClient.SubscribeAsync("/pla-4jx_e204fc31-77d9-4613-8dbf-212c162a809b/deviceTemplateAsJson/W1004"); 
            
            addTopics("a");
            addTopics("b");
            addTopics("c");
            foreach (var item in topiclist)
            {
                await mqttClient.SubscribeAsync($"{item}");
            }

            //await mqttClient.SubscribeAsync("a");
            //await mqttClient.SubscribeAsync("b");

        }

        public Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            throw new System.NotImplementedException();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await mqttClient.ConnectAsync(options);
            if (!mqttClient.IsConnected)
            {
                await mqttClient.ReconnectAsync();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                var disconnectOption = new MqttClientDisconnectOptions
                {
                    ReasonCode = MqttClientDisconnectReason.NormalDisconnection,
                    ReasonString = "NormalDiconnection"
                };
                await mqttClient.DisconnectAsync(disconnectOption, cancellationToken);
            }
            await mqttClient.DisconnectAsync();
        }
    }
}

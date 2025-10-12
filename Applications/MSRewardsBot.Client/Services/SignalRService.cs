using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using MSRewardsBot.Common.DataEntities.Interfaces;

namespace MSRewardsBot.Client.Services
{
    public class SignalRService : IBotAPI
    {
        private HubConnection _connection { get; set; }

        public async Task ConnectAsync()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:10500/cmdhub")
                .WithAutomaticReconnect()
                .Build();

            await _connection.StartAsync();
            _connection.On<string>(nameof(IBotAPI.SendTestMessage), SendTestMessage);
        }

        public async Task SendTestMessage(string m)
        {
            Debug.WriteLine($"Command received: {m}");
            await _connection.InvokeAsync(nameof(IBotAPI.SendTestMessage), "THIS IS A TEST MESSAGE");
        }


    }
}

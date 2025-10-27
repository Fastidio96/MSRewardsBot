using System;
using System.Collections.Generic;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Network
{
    public interface IConnectionManager
    {
        event EventHandler<ClientArgs> ClientConnected;
        event EventHandler<ClientArgs> ClientDisconnected;
        void AddConnection(ClientInfo client);
        ClientInfo GetConnection(string connectionId);
        void UpdateConnection(string connectionId, ClientInfo updatedClientInfo);
        void RemoveConnection(string connectionId);
        IReadOnlyCollection<ClientInfo> GetClients();
    }
}

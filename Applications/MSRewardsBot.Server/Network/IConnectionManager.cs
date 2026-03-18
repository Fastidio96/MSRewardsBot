using System;
using System.Collections.Generic;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Network
{
    public interface IConnectionManager
    {
        event EventHandler<ClientArgs> ClientConnected;
        event EventHandler<ClientArgs> ClientDisconnected;
        event EventHandler<ClientArgs> ClientUpdateVersion;
        void AddConnection(ClientInfo client);
        ClientInfo GetConnection(string connectionId);
        ClientInfo GetConnection(int userId);
        void UpdateConnection(string connectionId, ClientInfo updatedClientInfo);
        void UpdateClientVersion(string connectionId, Version version);
        void RemoveConnection(string connectionId);
        IReadOnlyCollection<ClientInfo> GetClients();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using MSRewardsBot.Server.DataEntities;

namespace MSRewardsBot.Server.Network
{
    public class ConnectionManager : IConnectionManager
    {
        public event EventHandler<ClientArgs> ClientConnected;
        public event EventHandler<ClientArgs> ClientDisconnected;

        private readonly HashSet<ClientInfo> _connections = [];

        public void AddConnection(ClientInfo clientInfo)
        {
            lock (_connections)
            {
                _connections.Add(clientInfo);
                ClientConnected?.Invoke(this, new ClientArgs(clientInfo.ConnectionId));
            }
        }

        public void UpdateConnection(string connectionId, ClientInfo updatedClientInfo)
        {
            lock (_connections)
            {
                _connections.RemoveWhere(c => c.ConnectionId == connectionId);
                _connections.Add(updatedClientInfo);
            }
        }

        public void RemoveConnection(string connectionId)
        {
            lock (_connections)
            {
                ClientInfo client = _connections.FirstOrDefault(c => c.ConnectionId == connectionId);
                if(client == null)
                {
                    return;
                }

                _connections.Remove(client);
                ClientDisconnected?.Invoke(this, new ClientArgs(connectionId));
            }
        }

        public IReadOnlyCollection<ClientInfo> GetClients()
        {
            lock (_connections)
            {
                return _connections.ToList().AsReadOnly();
            }
        }
    }

    public class ClientArgs : EventArgs
    {
        public string ConnectionId { get; set; }

        public ClientArgs(string connectionId)
        {
            ConnectionId = connectionId;
        }
    }
}

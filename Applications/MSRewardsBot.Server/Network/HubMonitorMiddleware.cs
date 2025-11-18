using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Interfaces;
using MSRewardsBot.Server.Core;
using MSRewardsBot.Server.DataEntities;
using MSRewardsBot.Server.DataEntities.Attributes;

namespace MSRewardsBot.Server.Network
{
    public class HubMonitorMiddleware : IHubFilter
    {
        private readonly ILogger _logger;
        private readonly IConnectionManager _connection;
        private readonly CommandHubProxy _commandHub;
        private readonly BusinessLayer _business;

        public HubMonitorMiddleware(ILogger<HubMonitorMiddleware> logger, CommandHubProxy commandHub, IConnectionManager connection, BusinessLayer bl)
        {
            _logger = logger;
            _commandHub = commandHub;
            _connection = connection;
            _business = bl;
        }

        public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext context, Func<HubInvocationContext, ValueTask<object?>> next)
        {
            string connectionId = context.Context.ConnectionId;

            // Before call
            _logger.Log(LogLevel.Debug, "Incoming call on {MethodName} by {ConnectionId}",
                context.HubMethodName, connectionId);

            try
            {
                object result = await next(context); // call the actual hub method

                MethodInfo? methodInfo = context.HubMethod;
                if (methodInfo == null)
                {
                    throw new Exception("Method info is null!");
                }

                bool hasLoggedOnAttr = methodInfo.GetCustomAttribute<LoggedOnAttribute>() != null;
                if (hasLoggedOnAttr)
                {
                    ParameterInfo[] pars = methodInfo.GetParameters();
                    foreach (ParameterInfo pInfo in pars)
                    {
                        if (pInfo.ParameterType == typeof(Guid) && pInfo.Name == "token")
                        {
                            string val = context.HubMethodArguments[pInfo.Position]?.ToString();
                            if (Guid.TryParse(val, out Guid token))
                            {
                                if (methodInfo.Name == nameof(IBotAPI.Logout))
                                {
                                    RemoveUserFromConnection(token, connectionId);
                                }
                                else
                                {
                                    UpdateConnectionInfo(token, connectionId);

                                }
                            }
                        }
                    }
                }

                bool hasRestrictedAttribute = methodInfo.GetCustomAttribute<RequiredPrivilegeAttribute>() != null;
                if (hasRestrictedAttribute)
                {
                    if (!hasLoggedOnAttr)
                    {
                        throw new Exception($"The method {context.HubMethodName} has the restricted attribute but doesn't require the log in!");
                    }


                }

                // After successful call
                _logger.Log(LogLevel.Trace, "Completed call on {MethodName} by {ConnectionId}",
                    context.HubMethodName, context.Context.ConnectionId);

                return result;
            }
            catch (Exception ex)
            {
                // Handle exceptions
                _logger.Log(LogLevel.Error, "Error in {MethodName} by {ConnectionId}: {ExMessage}",
                    context.HubMethodName, context.Context.ConnectionId, ex.Message);
                throw;
            }
        }

        public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
        {
            string ip = GetIp(context);
            _logger.LogInformation("Client connected with ip [{ip}]: {ConnectionId}", ip, context.Context.ConnectionId);
            _connection.AddConnection(new ClientInfo()
            {
                ConnectionId = context.Context.ConnectionId,
                IP = ip
            });

            return next(context);
        }

        public Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
        {
            string connectionid = context.Context.ConnectionId;
            if (exception != null)
            {
                _logger.Log(LogLevel.Error, "Client disconnected with an exception: {Error}", exception.Message);
            }

            ClientInfo info = _connection.GetConnection(connectionid);
            UnsubscribeMsAccounts(info);

            _connection.RemoveConnection(connectionid);

            _logger.Log(LogLevel.Information, "Client disconnected with ip [{ip}]: {ConnectionId}", info.IP, connectionid);
            return next(context, exception);
        }

        private string GetIp(HubLifetimeContext context)
        {
            HttpContext ctx = context.Context.GetHttpContext();
            return
                ctx?.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                ?? ctx?.Connection.RemoteIpAddress?.ToString();
        }

        private void UpdateConnectionInfo(Guid token, string connectionId)
        {
            if (token != Guid.Empty)
            {
                ClientInfo info = _connection.GetConnection(connectionId);

                User user = _business.GetUserInfo(token);
                if (user == null)
                {
                    return;
                }

                info.User = user;

                _connection.UpdateConnection(connectionId, info);

                foreach (MSAccount acc in info.User.MSAccounts)
                {
                    acc.Stats.PropertyChanged += Stats_PropertyChanged;
                }
            }
        }

        private async void Stats_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is ClientInfo info)
            {
                await _commandHub.SendMSAccountsInfo(info);
            }
        }

        private void UnsubscribeMsAccounts(ClientInfo info)
        {
            if (info.User != null)
            {
                foreach (MSAccount acc in info.User.MSAccounts)
                {
                    acc.Stats.PropertyChanged -= Stats_PropertyChanged;
                }
            }
        }

        private void RemoveUserFromConnection(Guid token, string connectionId)
        {
            if (token != Guid.Empty)
            {
                ClientInfo info = _connection.GetConnection(connectionId);

                User user = _business.GetUserInfo(token);
                if (user == null)
                {
                    return;
                }

                UnsubscribeMsAccounts(info);
                info.User = null;

                _connection.UpdateConnection(connectionId, info);
            }
        }
    }
}

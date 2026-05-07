using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common.DataEntities.Accounting;
using MSRewardsBot.Common.DataEntities.Interfaces;
using MSRewardsBot.Server.Core.Factories;
using MSRewardsBot.Server.DataEntities;
using MSRewardsBot.Server.DataEntities.Attributes;

namespace MSRewardsBot.Server.Network
{
    public class HubMonitorMiddleware : IHubFilter
    {
        private readonly ILogger _logger;
        private readonly IConnectionManager _connection;
        private readonly BusinessFactory _businessFactory;
        private static readonly ConcurrentDictionary<string, (int count, DateTime windowStart)> _attempts = new ConcurrentDictionary<string, (int count, DateTime windowStart)>();

        public HubMonitorMiddleware(ILogger<HubMonitorMiddleware> logger, IConnectionManager connection, BusinessFactory businessFactory)
        {
            _logger = logger;
            _connection = connection;
            _businessFactory = businessFactory;
        }

        public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext context, Func<HubInvocationContext, ValueTask<object?>> next)
        {
            string connectionId = context.Context.ConnectionId;

            try
            {
                MethodInfo? methodInfo = context.HubMethod;
                if (methodInfo == null)
                {
                    throw new Exception("Method info is null");
                }

                if (context.HubMethodName is nameof(IBotAPI.Login) or nameof(IBotAPI.Register))
                {
                    string ip = GetIp(context.Context.GetHttpContext()) ?? "unknown";
                    var now = DateTime.UtcNow;
                    var entry = _attempts.AddOrUpdate(ip,
                        _ => (1, now),
                        (_, old) => (now - old.windowStart > TimeSpan.FromMinutes(1)) ? (1, now) : (old.count + 1, old.windowStart));
                    if (entry.count > 5)
                    {
                        throw new HubException("Too many attempts. Try again later.");
                    }
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
                                UpdateConnectionInfo(token, connectionId);
                                break;
                            }
                        }
                    }

                    if (GetUserByConnectionId(connectionId) == null)
                    {
                        throw new Exception($"{connectionId} [{GetIp(context.Context.GetHttpContext())}] tried to execute a logged operation ({context.HubMethodName}) while not logged.");
                    }
                }

                bool hasRestrictedAttribute = methodInfo.GetCustomAttribute<RequiredPrivilegeAttribute>() != null;
                if (hasRestrictedAttribute)
                {
                    if (!hasLoggedOnAttr)
                    {
                        throw new Exception($"The method {context.HubMethodName} has the restricted attribute but doesn't require the log in!");
                    }

                    User user = GetUserByConnectionId(connectionId);
                    if (user == null || !user.IsAdmin)
                    {
                        throw new Exception($"{connectionId} [{GetIp(context.Context.GetHttpContext())}] tried to execute an admin operation ({context.HubMethodName}) while not logged as admin.");
                    }
                }

                object result = await next(context); // call the actual hub method

                // After successful call
                _logger.Log(LogLevel.Trace, "Completed call on {MethodName} by {ConnectionId}",
                    context.HubMethodName, context.Context.ConnectionId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Error in {MethodName} by {ConnectionId}: {ExMessage}",
                    context.HubMethodName, context.Context.ConnectionId, ex.Message);
                throw;
            }
        }

        public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
        {
            string ip = GetIp(context.Context.GetHttpContext());
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
            string connectionId = context.Context.ConnectionId;
            ClientInfo info = _connection.GetConnection(connectionId);

            if (exception != null)
            {
                _logger.Log(LogLevel.Error, "Client disconnected with ip [{ip}] with an exception: {Error}", info.IP, exception.Message);
            }

            _connection.RemoveConnection(connectionId);

            _logger.Log(LogLevel.Information, "Client disconnected with ip [{ip}]: {ConnectionId}", info.IP, connectionId);
            return next(context, exception);
        }

        private User GetUserByConnectionId(string connectionId)
        {
            return _connection.GetConnection(connectionId)?.User;
        }

        private void UpdateConnectionInfo(Guid token, string connectionId)
        {
            ClientInfo info = _connection.GetConnection(connectionId);

            using (ScopedBusiness scope = _businessFactory.Create())
            {
                info.User = token != Guid.Empty ?
                    scope.Business.GetUserInfo(token) : null;
            }

            _connection.UpdateConnection(connectionId, info);
        }

        private static string GetIp(HttpContext ctx)
        {
            return
                ctx?.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                ?? ctx?.Connection.RemoteIpAddress?.ToString();
        }
    }
}

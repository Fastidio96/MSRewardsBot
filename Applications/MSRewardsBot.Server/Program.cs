using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Server.Automation;
using MSRewardsBot.Server.Core;
using MSRewardsBot.Server.Network;

namespace MSRewardsBot.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            // Add services to the container.
            builder.Services.AddAuthorization();
            builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();
            builder.Services.AddSingleton<Core.Server>();
            builder.Services.AddSingleton<CommandHubProxy>();
            builder.Services.AddSingleton<BusinessLayer>();
            builder.Services.AddSingleton<BrowserManager>();
            builder.Services.AddSignalR()
                .AddHubOptions<CommandHub>(options =>
                {
                    options.AddFilter<HubMonitorMiddleware>();
                });

            builder.Services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    ["application/octet-stream"]);
            });

            WebApplication app = builder.Build();
            app.UseResponseCompression();
            app.Use(async (context, next) =>
            {
                try
                {
                    await next.Invoke();
                }
                catch (Exception ex)
                {
                    app.Logger.LogError("Error: {ErrMessage}", ex.Message);
                }
            });

            Core.Server server = app.Services.GetRequiredService<Core.Server>();
            BrowserManager browser = app.Services.GetRequiredService<BrowserManager>();
            app.MapHub<CommandHub>("/cmdhub");

            // Configure the HTTP request pipeline.

            //app.UseHttpsRedirection();

            app.UseAuthorization();

            server.Start();
            app.Logger.LogInformation("MS Rewards bot server started");

            app.Lifetime.ApplicationStopping.Register(() =>
            {
                server.Dispose();
                browser.Dispose();
            });

            app.Run();
        }
    }
}

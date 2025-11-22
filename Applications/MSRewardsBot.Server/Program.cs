using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MSRewardsBot.Common;
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
            builder.Services.AddLogging(logbuilder =>
            {
                logbuilder.ClearProviders();
                logbuilder.AddConsole(options =>
                {
                    options.FormatterName = nameof(CustomConsoleFormatter);
                });

                logbuilder.AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleOptions>(cco =>
                {
                    cco.UseColors = true;
                    cco.WriteOnFile = true;
                    cco.GroupedCategories = true;
                });
            });

            builder.WebHost.UseUrls(Env.GetServerConnection());

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
#if DEBUG
                    options.EnableDetailedErrors = true;
#endif
                    options.AddFilter<HubMonitorMiddleware>();
                    //options.ClientTimeoutInterval = new TimeSpan(0, 0, 10);
                    //options.KeepAliveInterval = new TimeSpan(0, 0, 5);
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

            app.MapHub<CommandHub>($"/{Env.SERVER_HUB_NAME}");

            // Configure the HTTP request pipeline.

//            if (Env.IS_HTTPS_ENABLED)
//            {
//#pragma warning disable CS0162 // Unreachable code detected
//                app.UseHttpsRedirection();
//#pragma warning restore CS0162 // Unreachable code detected
//            }

            app.UseAuthorization();

            server.Start();

            app.Lifetime.ApplicationStopping.Register(() =>
            {
                server.Dispose();
                browser.Dispose();
            });

            app.Run();
        }
    }
}

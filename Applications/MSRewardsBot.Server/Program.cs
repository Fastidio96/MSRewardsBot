using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            bool startedAsWinService = OperatingSystem.IsWindows() && !Environment.UserInteractive;

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            if (!startedAsWinService)
            {
                Utils.EnableConsoleANSI();
            }

            builder.Services.AddLogging(logbuilder =>
            {
                logbuilder.ClearProviders();
                logbuilder.AddConsole(options =>
                {
                    options.FormatterName = nameof(CustomConsoleFormatter);
                });

                logbuilder.AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleOptions>(cco =>
                {
                    cco.UseColors = !startedAsWinService;
                    cco.WriteOnFile = true;
                    cco.GroupedCategories = true;
                });

                logbuilder.SetMinimumLevel(LogLevel.Debug);
            });

            builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Information);
            builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Information);

            // Enable Windows Service only on Windows
            if (startedAsWinService)
            {
                builder.Services.AddWindowsService();
            }

            builder.WebHost.UseUrls(Env.GetServerConnection());

            // Add services to the container.
            builder.Services.AddAuthorization();
            builder.Services.AddSingleton<RealTimeData>();
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

            ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();

            app.Use(async (context, next) =>
            {
                try
                {
                    await next.Invoke();
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Critical, ex, "Error");
                }
            });

            Core.Server server = app.Services.GetRequiredService<Core.Server>();
            BrowserManager browser = app.Services.GetRequiredService<BrowserManager>();

            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs e)
            {
                Exception ex = (Exception)e.ExceptionObject;
                logger.Log(LogLevel.Critical, ex, "Unhandled exception on CurrentDomain | IsTerminating: {IsTerminating}", e.IsTerminating);
                logger.Log(LogLevel.Critical, "Source: {source}", ex.Source);
                logger.Log(LogLevel.Critical, "StackTrace: {stack}", ex.StackTrace);
            };

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

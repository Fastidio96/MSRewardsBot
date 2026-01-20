using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MSRewardsBot.Common.Utilities;
using MSRewardsBot.Server.Automation;
using MSRewardsBot.Server.Core;
using MSRewardsBot.Server.Core.Factories;
using MSRewardsBot.Server.DB;
using MSRewardsBot.Server.Helpers;
using MSRewardsBot.Server.Network;
using TaskScheduler = MSRewardsBot.Server.Core.TaskScheduler;

namespace MSRewardsBot.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.Services.Configure<Settings>(builder.Configuration);

            if (!RuntimeEnvironment.IsWindowsService())
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
                    cco.UseColors = !RuntimeEnvironment.IsWindowsService();
                    cco.WriteOnFile = true;
                    cco.GroupedCategories = true;
                });

                logbuilder.SetMinimumLevel(LogLevel.Debug);
            });

            builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Information);
            builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Information);

            // Enable Windows Service only on Windows
            if (RuntimeEnvironment.IsWindowsService())
            {
                builder.Services.AddWindowsService();
            }

            Settings settings = builder.Configuration.Get<Settings>();
            builder.WebHost.UseUrls(NetworkUtilities.GetConnectionString(
                settings.IsHttpsEnabled, settings.ServerHost, settings.ServerPort
            ));

            // Add services to the container.
            builder.Services.AddAuthorization();
            builder.Services.AddSingleton<RealTimeData>();
            builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();
            builder.Services.AddSingleton<Core.Server>();
            builder.Services.AddSingleton<TaskScheduler>();
            builder.Services.AddTransient<CommandHubProxy>();
            builder.Services.AddSingleton<BusinessFactory>();
            builder.Services.AddScoped<BusinessLayer>();
            builder.Services.AddScoped<DataLayer>();
            builder.Services.AddDbContext<MSRBContext>();
            builder.Services.AddSingleton<BrowserManager>();
            builder.Services.AddSignalR()
                .AddHubOptions<CommandHub>(options =>
                {
#if DEBUG
                    options.EnableDetailedErrors = true;
#endif
                    options.AddFilter<HubMonitorMiddleware>();
                });

            builder.Services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    ["application/octet-stream"]);
            });

            WebApplication app = builder.Build();
            app.UseResponseCompression();

            TaskScheduler taskScheduler = app.Services.GetRequiredService<TaskScheduler>();
            Core.Server server = app.Services.GetRequiredService<Core.Server>();
            BrowserManager browser = app.Services.GetRequiredService<BrowserManager>();
            settings = app.Services.GetService<IOptions<Settings>>().Value;

            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs e)
            {
                Exception ex = (Exception)e.ExceptionObject;
                CustomConsoleFormatter.WriteOnFile("Unhandled exception caused app to crash");
                CustomConsoleFormatter.WriteOnFile(ex.Message);
                CustomConsoleFormatter.WriteOnFile(ex.Source);
                CustomConsoleFormatter.WriteOnFile(ex.StackTrace);

                Console.Error.WriteLine(e.ExceptionObject);
            };

            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            app.MapHub<CommandHub>("/cmdhub");

            // Configure the HTTP request pipeline.
            if (settings.IsHttpsEnabled)
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthorization();

            server.Start();

            app.Lifetime.ApplicationStopping.Register(() =>
            {
                System.Threading.Tasks.TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;

                taskScheduler.Dispose();
                server.Dispose();
                browser.Dispose();
            });

            app.Run();
        }

        private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                string text = $"[{DateTime.Now:O}] UNOBSERVED TASK\n" + e.Exception.ToString();
                CustomConsoleFormatter.WriteOnFile(text + Environment.NewLine);

                e.SetObserved();
            }
            catch { }
        }
    }
}

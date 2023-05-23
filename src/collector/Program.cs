using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Collector
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
            .ConfigureLogging(loggingContext =>
            {
                loggingContext.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                });
            })
            .ConfigureHostConfiguration(hostConfig =>
            {
                hostConfig.AddEnvironmentVariables();
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHttpClient();
                services.AddSingleton<CloudflareKVClient>();
                services.AddSingleton<YoLinkCollector>();
                services.AddCronJob<MeasurementSyncronizer, MeasurementSyncronizer.Options>(c =>
                {
                    c.TimeZoneInfo = TimeZoneInfo.Local;
                    c.CronExpression = "0 * * * *";
                    c.DeviceIds = new[] { "d88b4c010005b59d", "d88b4c0100041f81" };
                });

                services.Configure<CloudflareKVClient.Options>(hostContext.Configuration.GetSection("CF"));
                services.Configure<YoLinkCollector.Options>(hostContext.Configuration.GetSection("YO"));
            })
            .Build()
            .RunAsync();
        }
    }
}
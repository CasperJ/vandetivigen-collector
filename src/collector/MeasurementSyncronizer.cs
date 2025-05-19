using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Collector
{
    public class MeasurementSyncronizer: CronJobService
    {
        public class Options : IScheduleConfig<MeasurementSyncronizer>
        {
            public string CronExpression { get ; set; } = "";
            public TimeZoneInfo TimeZoneInfo { get; set; } = TimeZoneInfo.Local;
            public string[] DeviceIds { get; set; } = new string[]{};
        }
        private readonly YoLinkCollector _yoLinkCollector;
        private readonly CloudflareKVClient _kVClient;
        private readonly Options _options;
        private readonly ILogger<MeasurementSyncronizer> _log;

        private const string KV_ENTRY = "sensor_data";

        public MeasurementSyncronizer(Options config, 
        YoLinkCollector yoLinkCollector, CloudflareKVClient kVClient, ILogger<MeasurementSyncronizer> log)
        : base(config.CronExpression, config.TimeZoneInfo)
        {
            _options = config;
            _yoLinkCollector = yoLinkCollector;
            _kVClient = kVClient;
            _log = log;
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            _log.LogInformation("Getting current data set");

            var dataString = await _kVClient.GetKVValue(KV_ENTRY);

            var dataPoints = new List<DataAggregate>();
            if (dataString != null)
            {
                dataPoints = JsonSerializer.Deserialize<List<DataAggregate>>(dataString);
            }
            _log.LogInformation("Got current data set");

            decimal? temp1 = null, temp2 = null;
            DateTime? date = null;

            _log.LogInformation("Getting sensor data");
            await foreach (var dev in _yoLinkCollector.CollectAsync(_options.DeviceIds))
            {
                if (dev.deviceId == "d88b4c0100092689")
                {
                    _log.LogInformation("Got temperature value {dev.temperature} from device {dev.deviceId}", dev.temperature, dev.deviceId);
                    temp1 = dev.temperature;
                }
                if (dev.deviceId == "d88b4c0100041f81")
                {
                    _log.LogInformation("Got temperature value {dev.temperature} from device {dev.deviceId}", dev.temperature, dev.deviceId);
                    temp2 = dev.temperature;
                }
                if (date == null || date < dev.reportedAt)
                    date = dev.reportedAt;
            }
            if (date != null)
                dataPoints.Add(new DataAggregate(temp1, temp2, date.Value));

            _log.LogInformation("Added sensor data to data set");

            dataPoints = dataPoints.OrderByDescending(d => d.recordedOn).Take(120).ToList();

            _log.LogInformation("Uploading data set");
            dataString = JsonSerializer.Serialize(dataPoints);
            await _kVClient.SetKVValue(KV_ENTRY, dataString);
            _log.LogInformation("Uploading data set");
        }
    }
}

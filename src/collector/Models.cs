using System.Text.Json.Serialization;

namespace Collector
{
    public record DeviceData(string deviceId, decimal? temperature, decimal? batteryLevel, DateTime reportedAt);
    public record DataAggregate(
        [property:JsonPropertyName("t1")]decimal? temperature, 
        [property:JsonPropertyName("t2")]decimal? temperature2, 
        [property:JsonPropertyName("r")]DateTime recordedOn);
}
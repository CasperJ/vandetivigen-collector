using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Collector
{
    public class YoLinkCollector
    {
        public class Options
        {
            public string ClientId { get; set; } = String.Empty;
            public string ClientSecret { get; set; } = String.Empty;
        }

        private HttpClient _client;
        private Options _options;
        private readonly ILogger<YoLinkCollector> _log;

        public YoLinkCollector(IHttpClientFactory httpClientFactory, IOptions<Options> options, ILogger<YoLinkCollector> log)
        {
            _client = httpClientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://api.yosmart.com/open/yolink/");
            _options = options.Value;
            _log = log;
        }

        public async IAsyncEnumerable<DeviceData> CollectAsync(string[] deviceIds)
        {
            var accessToken = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            await foreach (var device in GetDeviceListAsync())
            {
                if (deviceIds.Contains(device.deviceId))
                {
                    DeviceData? data = null;
                    try{
                        data = await GetDeviceInfoAsync(device.deviceId, device.token);
                    }
                    catch(Exception ex){
                        _log.LogError(ex, "Error getting device state");
                    }
                    if ( data != null)
                        yield return data;

                }else{
                    _log.LogInformation($"Skipped device {device} as it's not in the list for devices to query");
                }
            }

        }

        private async Task<DeviceData> GetDeviceInfoAsync(string deviceId, string token)
        {
            var response = await _client.PostAsJsonAsync<dynamic>("v2/api", new
            {
                method = "THSensor.getState",
                targetDevice = deviceId,
                token = token
            });

            if (response != null && response.IsSuccessStatusCode && response.Content != null)
            {
                var data = JsonSerializer.Deserialize<JsonNode>(response.Content.ReadAsStream());
                var temperature = data?["data"]?["state"]?["temperature"]?.GetValue<decimal>();
                var battery = data?["data"]?["state"]?["battery"]?.GetValue<decimal>();
                var reportAt = data?["data"]?["reportAt"]?.GetValue<DateTime>();
                if (reportAt != null)
                {
                    return new DeviceData(deviceId, temperature, battery, reportAt.Value);
                }
                else
                    throw new Exception($"Cannot parse date for device: {deviceId}");
            }

            throw new Exception($"Cannotget state for device: {deviceId}");
        }

        private async IAsyncEnumerable<(string deviceId, string token)> GetDeviceListAsync()
        {
            var response = await _client.PostAsJsonAsync<dynamic>("v2/api", new { method = "Home.getDeviceList" });

            if (response != null && response.IsSuccessStatusCode && response.Content != null)
            {
                var data = JsonSerializer.Deserialize<JsonNode>(response.Content.ReadAsStream());
                var devices = data?["data"]?["devices"];
                if (devices != null)
                {
                    foreach (var device in devices.AsArray())
                    {
                        var id = device?["deviceId"]?.GetValue<string>();
                        var token = device?["token"]?.GetValue<string>();
                        if (id != null && token != null)
                            yield return (id, token);
                    }
                }
            }

        }

        private async Task<string?> GetAccessTokenAsync()
        {

            var loginResponse = await _client.PostAsync("token", new FormUrlEncodedContent(new[]{
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _options.ClientId),
            new KeyValuePair<string, string>("client_secret", _options.ClientSecret)
            }));

            if (loginResponse != null && loginResponse.IsSuccessStatusCode && loginResponse.Content != null)
            {
                var data = JsonSerializer.Deserialize<JsonNode>(loginResponse.Content.ReadAsStream());
                return data?["access_token"]?.GetValue<string>();
            }
            throw new Exception("");
        }
    }
}
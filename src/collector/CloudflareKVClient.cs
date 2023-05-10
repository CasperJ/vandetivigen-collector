using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Collector
{
    public class CloudflareKVClient
    {
        public class Options
        {
            public string ApiKey { get; set; } = String.Empty;
            public string AccountIdentifier { get; set; } = String.Empty;
            public string NamespaceIdentifier { get; set; } = String.Empty;
        }

        private HttpClient _client;
        private Options _options;
        private readonly ILogger<CloudflareKVClient> _log;

        public CloudflareKVClient(IHttpClientFactory httpClientFactory, IOptions<Options> options, ILogger<CloudflareKVClient> log)
        {
            _client = httpClientFactory.CreateClient();
            _options = options.Value;
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);
            _log = log;
        }


        public async Task<string?> GetKVValue(string keyName)
        {
            var uri = $"https://api.cloudflare.com/client/v4/accounts/{_options.AccountIdentifier}/storage/kv/namespaces/{_options.NamespaceIdentifier}/values/{keyName}";
            try
            {
                var value = await _client.GetStringAsync(uri);
                return value;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            { 
                return null;
            }
        }
        public async Task SetKVValue(string keyName, string value)
        {
            var uri = $"https://api.cloudflare.com/client/v4/accounts/{_options.AccountIdentifier}/storage/kv/namespaces/{_options.NamespaceIdentifier}/bulk";

            var response = await _client.PutAsJsonAsync<dynamic[]>(uri, new[]{new {
                base64 = false,
                key = keyName,
                value = value
            }});
        }
    }
}
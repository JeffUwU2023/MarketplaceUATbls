using MarketplaceUAT.App.Config;
using System.Net.Http.Headers;

namespace MarketplaceUAT.App.Services
{
    public class ApiClient
    {
        private readonly HttpClient _client;

        public ApiClient()
        {
            var handler = new SocketsHttpHandler
            {
                Proxy = null,
                UseProxy = false
            };

            _client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            _client.DefaultRequestHeaders.ConnectionClose = true;

            if (!string.IsNullOrWhiteSpace(SesionUsuario.Token))
            {
                _client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        SesionUsuario.Token);
            }
        }

        public HttpClient Client => _client;
    }
}
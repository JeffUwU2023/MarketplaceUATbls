using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using System.Net.Http.Json;
using System.Threading;

namespace MarketplaceUAT.App.Services
{
    public class ApiClient
    {
        private readonly HttpClient _client;
        private readonly HttpClient _refreshClient;

        private readonly SemaphoreSlim _refreshLock = new(1, 1);

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

            var refreshHandler = new SocketsHttpHandler
            {
                Proxy = null,
                UseProxy = false
            };

            _refreshClient = new HttpClient(refreshHandler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            _refreshClient.DefaultRequestHeaders.ConnectionClose = true;
        }

        private async Task CerrarSesionPorExpiracionAsync()
        {
            await SesionUsuario.CerrarSesionAsync();

            if (Shell.Current is AppShell appShell)
            {
                await appShell.MostrarLoginAsync();
            }
        }

        public HttpClient Client => _client;

        public void ConfigurarAutorizacion()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            if (!string.IsNullOrWhiteSpace(SesionUsuario.Token))
            {
                _client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        SesionUsuario.Token);
            }
        }

        private async Task<bool> RenovarTokenAsync(
    string tokenAnterior,
    CancellationToken cancellationToken = default)
        {
            await _refreshLock.WaitAsync(cancellationToken);

            try
            {
                // Otra petición pudo haber renovado el token.
                if (SesionUsuario.Token != tokenAnterior)
                {
                    return !string.IsNullOrWhiteSpace(SesionUsuario.Token);
                }

                string? refreshToken = SesionUsuario.RefreshToken;

                if (string.IsNullOrWhiteSpace(refreshToken))
                    return false;

                string url =
                    $"{ApiConfig.BaseUrl}/api/Usuarios/Refresh";

                var datos = new RefreshTokenRequest
                {
                    RefreshToken = refreshToken
                };

                using var response = await _refreshClient.PostAsJsonAsync(
                url,
                datos,
                cancellationToken);

                // El servidor rechazó el refresh token.
                if (response.StatusCode ==
                        System.Net.HttpStatusCode.BadRequest ||
                    response.StatusCode ==
                        System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode ==
                        System.Net.HttpStatusCode.Forbidden)
                {
                    return false;
                }

                // Otros errores HTTP no significan necesariamente
                // que el refresh token sea inválido.
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"Error al renovar la sesión. HTTP: " +
                        $"{(int)response.StatusCode}");
                }

                var resultado =
    await response.Content.ReadFromJsonAsync<RefreshTokenResponse>(
        cancellationToken: cancellationToken);

                if (resultado == null ||
                    string.IsNullOrWhiteSpace(resultado.Token) ||
                    string.IsNullOrWhiteSpace(resultado.RefreshToken))
                {
                    throw new InvalidOperationException(
                        "La API no devolvió tokens válidos.");
                }

                await SesionUsuario.ActualizarTokensAsync(
                    resultado.Token,
                    resultado.RefreshToken);

                return true;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        public async Task<HttpResponseMessage> EnviarAutenticadoAsync(
    Func<HttpRequestMessage> crearPeticion,
    CancellationToken cancellationToken = default)
        {
            string? tokenActual = SesionUsuario.Token;

            if (string.IsNullOrWhiteSpace(tokenActual))
            {
                throw new InvalidOperationException(
                    "No existe un token de autenticación.");
            }

            using var peticion = crearPeticion();

            peticion.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    tokenActual);

            var respuesta = await _client.SendAsync(
    peticion,
    cancellationToken);

            if (respuesta.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            {
                return respuesta;
            }

            respuesta.Dispose();

            bool renovado = await RenovarTokenAsync(
    tokenActual,
    cancellationToken);

            if (!renovado)
            {
                await CerrarSesionPorExpiracionAsync();

                throw new UnauthorizedAccessException(
                    "La sesión ha expirado. Inicia sesión nuevamente.");
            }

            using var nuevaPeticion = crearPeticion();

            nuevaPeticion.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    SesionUsuario.Token);

            var segundaRespuesta = await _client.SendAsync(
    nuevaPeticion,
    cancellationToken);

            if (segundaRespuesta.StatusCode ==
                System.Net.HttpStatusCode.Unauthorized)
            {
                segundaRespuesta.Dispose();

                await CerrarSesionPorExpiracionAsync();

                throw new UnauthorizedAccessException(
                    "La sesión no pudo validarse después de renovarla. " +
                    "Inicia sesión nuevamente.");
            }

            return segundaRespuesta;
        }

        public async Task<HttpResponseMessage> EnviarAutenticadoAsync(
    Func<Task<HttpRequestMessage>> crearPeticion,
    CancellationToken cancellationToken = default)
        {
            string? tokenActual = SesionUsuario.Token;

            if (string.IsNullOrWhiteSpace(tokenActual))
            {
                throw new InvalidOperationException(
                    "No existe un token de autenticación.");
            }

            using var peticion = await crearPeticion();

            peticion.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    tokenActual);

            var respuesta = await _client.SendAsync(
    peticion,
    cancellationToken);

            if (respuesta.StatusCode !=
                System.Net.HttpStatusCode.Unauthorized)
            {
                return respuesta;
            }

            respuesta.Dispose();

            bool renovado = await RenovarTokenAsync(
    tokenActual,
    cancellationToken);

            if (!renovado)
            {
                await CerrarSesionPorExpiracionAsync();

                throw new UnauthorizedAccessException(
                    "La sesión ha expirado. Inicia sesión nuevamente.");
            }

            using var nuevaPeticion = await crearPeticion();

            nuevaPeticion.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    SesionUsuario.Token);

            var segundaRespuesta = await _client.SendAsync(
    nuevaPeticion,
    cancellationToken);

            if (segundaRespuesta.StatusCode ==
                System.Net.HttpStatusCode.Unauthorized)
            {
                segundaRespuesta.Dispose();

                await CerrarSesionPorExpiracionAsync();

                throw new UnauthorizedAccessException(
                    "La sesión no pudo validarse después de renovarla. " +
                    "Inicia sesión nuevamente.");
            }

            return segundaRespuesta;
        }
    }
}

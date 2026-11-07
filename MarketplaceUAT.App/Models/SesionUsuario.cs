using MarketplaceUAT.App.Models;
using System.Text.Json;
using System.Text;

namespace MarketplaceUAT.App
{
    public static class SesionUsuario
    {
        private const string ClaveUsuario = "SesionUsuario";
        private const string ClaveToken = "SesionToken";
        private const string ClaveRefreshToken = "SesionRefreshToken";

        public static Usuario? UsuarioActual { get; private set; }

        public static string? Token { get; private set; }
        public static string? RefreshToken { get; private set; }

        private static bool TokenVigente(string token)
        {
            try
            {
                string[] partes = token.Split('.');

                if (partes.Length != 3)
                    return false;

                string payload = partes[1]
                    .Replace('-', '+')
                    .Replace('_', '/');

                payload = payload.PadRight(
                    payload.Length + (4 - payload.Length % 4) % 4,
                    '=');

                byte[] bytes = Convert.FromBase64String(payload);

                using JsonDocument documento =
                    JsonDocument.Parse(bytes);

                JsonElement raiz = documento.RootElement;

                if (!raiz.TryGetProperty("exp", out JsonElement exp))
                    return false;

                if (!exp.TryGetInt64(out long expiracion))
                    return false;

                DateTimeOffset fechaExpiracion =
                    DateTimeOffset.FromUnixTimeSeconds(expiracion);

                return fechaExpiracion > DateTimeOffset.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        public static bool EstaLogueado =>
            UsuarioActual != null &&
            !string.IsNullOrWhiteSpace(Token);


        public static async Task IniciarSesionAsync(
            Usuario usuario,
            string? token = null,
            string? refreshToken = null)
        {
            UsuarioActual = usuario;
            Token = token;
            RefreshToken = refreshToken;

            if (string.IsNullOrWhiteSpace(Token) ||
                string.IsNullOrWhiteSpace(RefreshToken))
            {
                await CerrarSesionAsync();
                return;
            }

            try
            {
                string usuarioJson =
                    JsonSerializer.Serialize(usuario);

                await SecureStorage.Default.SetAsync(
                    ClaveUsuario,
                    usuarioJson);

                await SecureStorage.Default.SetAsync(
                    ClaveToken,
                    Token);

                await SecureStorage.Default.SetAsync(
                    ClaveRefreshToken,
                    RefreshToken);
            }
            catch
            {
                UsuarioActual = null;
                Token = null;
                RefreshToken = null;

                SecureStorage.Default.Remove(ClaveUsuario);
                SecureStorage.Default.Remove(ClaveToken);
                SecureStorage.Default.Remove(ClaveRefreshToken);

                throw;
            }
        }

        public static async Task ActualizarTokensAsync(
        string nuevoToken,
        string nuevoRefreshToken)
        {
            if (string.IsNullOrWhiteSpace(nuevoToken) ||
                string.IsNullOrWhiteSpace(nuevoRefreshToken))
            {
                throw new ArgumentException(
                    "Los tokens no pueden estar vacíos.");
            }

            if (UsuarioActual == null)
            {
                throw new InvalidOperationException(
                    "No existe una sesión de usuario activa.");
            }

            await SecureStorage.Default.SetAsync(
                ClaveToken,
                nuevoToken);

            await SecureStorage.Default.SetAsync(
                ClaveRefreshToken,
                nuevoRefreshToken);

            Token = nuevoToken;
            RefreshToken = nuevoRefreshToken;
        }

        public static async Task<bool> RestaurarSesionAsync()
        {
            try
            {
                string? usuarioJson =
                    await SecureStorage.Default.GetAsync(ClaveUsuario);

                string? tokenGuardado =
                    await SecureStorage.Default.GetAsync(ClaveToken);

                string? refreshTokenGuardado =
                    await SecureStorage.Default.GetAsync(ClaveRefreshToken);

                if (string.IsNullOrWhiteSpace(usuarioJson) ||
                    string.IsNullOrWhiteSpace(tokenGuardado) ||
                    string.IsNullOrWhiteSpace(refreshTokenGuardado))
                {
                    await CerrarSesionAsync();
                    return false;
                }

                Usuario? usuario =
                    JsonSerializer.Deserialize<Usuario>(usuarioJson);

                if (usuario == null)
                {
                    await CerrarSesionAsync();
                    return false;
                }

                UsuarioActual = usuario;
                Token = tokenGuardado;
                RefreshToken = refreshTokenGuardado;

                return true;
            }
            catch
            {
                await CerrarSesionAsync();
                return false;
            }
        }

        public static async Task CerrarSesionAsync()
        {
            UsuarioActual = null;
            Token = null;
            RefreshToken = null;

            try
            {
                SecureStorage.Default.Remove(ClaveUsuario);
                SecureStorage.Default.Remove(ClaveToken);
                SecureStorage.Default.Remove(ClaveRefreshToken);
            }
            catch
            {
                // La sesión en memoria ya se ha cerrado.
            }

            await Task.CompletedTask;
        }

        // Conservamos temporalmente este método para no romper
        // las llamadas existentes en otras páginas.
        public static void CerrarSesion()
        {
            UsuarioActual = null;
            Token = null;
            RefreshToken = null;

            try
            {
                SecureStorage.Default.Remove(ClaveUsuario);
                SecureStorage.Default.Remove(ClaveToken);
                SecureStorage.Default.Remove(ClaveRefreshToken);
            }
            catch
            {
                // La sesión en memoria ya se ha cerrado.
            }
        }
    }
}

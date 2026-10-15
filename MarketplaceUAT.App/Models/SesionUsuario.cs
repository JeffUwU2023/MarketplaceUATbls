using MarketplaceUAT.App.Models;

namespace MarketplaceUAT.App
{
    public static class SesionUsuario
    {
        public static Usuario? UsuarioActual { get; private set; }

        public static string? Token { get; private set; }

        public static bool EstaLogueado =>
            UsuarioActual != null && !string.IsNullOrWhiteSpace(Token);

        public static void IniciarSesion(
            Usuario usuario,
            string? token = null)
        {
            UsuarioActual = usuario;
            Token = token;
        }

        public static void CerrarSesion()
        {
            UsuarioActual = null;
            Token = null;
        }
    }
}
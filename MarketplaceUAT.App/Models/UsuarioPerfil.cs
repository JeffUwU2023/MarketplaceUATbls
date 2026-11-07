namespace MarketplaceUAT.App.Models
{
    public class UsuarioPerfil
    {
        public int IdUsuario { get; set; }

        public string Nombres { get; set; } = string.Empty;

        public string Apellidos { get; set; } = string.Empty;

        public string TipoUsuario { get; set; } = string.Empty;

        public string? Correo { get; set; }

        public string? UrlFotoPerfil { get; set; }

        public DateTime FechaRegistro { get; set; }
    }
}

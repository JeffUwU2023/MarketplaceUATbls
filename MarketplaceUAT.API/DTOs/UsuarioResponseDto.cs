namespace MarketplaceUAT.API.DTOs
{
    public class UsuarioResponseDto
    {
        public int IdUsuario { get; set; }

        public string Nombres { get; set; } = string.Empty;

        public string Apellidos { get; set; } = string.Empty;

        public string TipoUsuario { get; set; } = string.Empty;

        public string? Matricula { get; set; }

        public string Correo { get; set; } = string.Empty;

        public string? UrlFotoPerfil { get; set; }

        public DateTime FechaRegistro { get; set; }

        public bool Activo { get; set; }
    }
}

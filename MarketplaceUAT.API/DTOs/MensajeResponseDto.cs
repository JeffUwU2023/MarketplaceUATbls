namespace MarketplaceUAT.API.DTOs
{
    public class MensajeResponseDto
    {
        public int IdMensaje { get; set; }
        public int IdConversacion { get; set; }
        public int IdUsuario { get; set; }
        public string? Contenido { get; set; }
        public string? UrlImagen { get; set; }
        public List<string> Imagenes { get; set; } = [];
        public DateTime FechaEnvio { get; set; }
        public bool Visto { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.Data.Models
{
    public class Mensaje
    {
        [Key]
        public int IdMensaje { get; set; }

        public int IdConversacion { get; set; }

        public int IdUsuario { get; set; }

        public string? Contenido { get; set; }

        public string? UrlImagen { get; set; }

        public DateTime FechaEnvio { get; set; }

        public bool Visto { get; set; }

        public bool Activo { get; set; }
    }
}
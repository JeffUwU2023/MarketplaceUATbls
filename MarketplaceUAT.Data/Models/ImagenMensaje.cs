using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.Data.Models
{
    public class ImagenMensaje
    {
        [Key]
        public int IdImagenMensaje { get; set; }

        public int IdMensaje { get; set; }

        public string UrlImagen { get; set; } = string.Empty;

        public bool Activo { get; set; }
    }
}
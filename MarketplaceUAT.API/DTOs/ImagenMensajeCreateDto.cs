using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.API.DTOs
{
    public class ImagenMensajeCreateDto
    {
        [Required(ErrorMessage = "El mensaje es obligatorio.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "El mensaje no es válido.")]
        public int IdMensaje { get; set; }

        [Required(ErrorMessage = "La imagen es obligatoria.")]
        [StringLength(500,
            ErrorMessage = "La URL de la imagen no puede superar los 500 caracteres.")]
        public string UrlImagen { get; set; } = string.Empty;
    }
}
using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.API.DTOs
{
    public class MensajeCreateDto
    {
        [Required(ErrorMessage = "La conversación es obligatoria.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "La conversación no es válida.")]
        public int IdConversacion { get; set; }

        [StringLength(1000,
        ErrorMessage = "El mensaje no puede superar los 1000 caracteres.")]
        public string? Contenido { get; set; }

        public string? UrlImagen { get; set; }

        public bool TieneImagenes { get; set; }
    }
}
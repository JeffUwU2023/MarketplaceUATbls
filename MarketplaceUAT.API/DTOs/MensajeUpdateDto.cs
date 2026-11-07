using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.API.DTOs
{
    public class MensajeUpdateDto
    {
        [Required(ErrorMessage = "El texto del mensaje es obligatorio.")]
        [StringLength(
            1000,
            MinimumLength = 1,
            ErrorMessage = "El mensaje no puede superar los 1000 caracteres.")]
        public string Contenido { get; set; } = string.Empty;
    }
}

using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.API.DTOs
{
    public class ConversacionCreateDto
    {
        [Required(ErrorMessage = "El producto es obligatorio.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "El producto no es válido.")]
        public int IdProducto { get; set; }
    }
}
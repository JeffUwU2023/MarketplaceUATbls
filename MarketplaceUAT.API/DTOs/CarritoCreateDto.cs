using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.API.DTOs
{
    public class CarritoCreateDto
    {
        [Required]
        public int IdProducto { get; set; }

        [Range(1, int.MaxValue)]
        public int Cantidad { get; set; } = 1;
    }
}

using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.API.DTOs
{
    public class FavoritoUpdateDto
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "El producto no es válido.")]
        public int IdProducto { get; set; }
    }
}

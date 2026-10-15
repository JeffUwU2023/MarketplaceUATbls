using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.API.DTOs
{
    public class FavoritoCreateDto
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "El producto no es válido.")]
        public int IdProducto { get; set; }
    }
}


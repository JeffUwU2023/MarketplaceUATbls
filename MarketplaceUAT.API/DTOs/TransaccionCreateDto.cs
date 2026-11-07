using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.API.DTOs
{
    public class TransaccionCreateDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "El producto debe tener un identificador válido.")]
        public int IdProducto { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El comprador debe tener un identificador válido.")]
        public int IdUsuarioComprador { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser de al menos una unidad.")]
        public int Cantidad { get; set; } = 1;
    }
}

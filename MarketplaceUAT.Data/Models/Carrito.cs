using System;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.Data.Models
{
    public class Carrito
    {
        [Key]
        public int IdCarrito { get; set; }

        public int IdUsuario { get; set; }

        public int IdProducto { get; set; }

        [Range(1, int.MaxValue)]
        public int Cantidad { get; set; } = 1;

        public DateTime FechaAgregado { get; set; } = DateTime.Now;
    }
}

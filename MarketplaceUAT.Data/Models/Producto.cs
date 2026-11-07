using System;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.Data.Models
{
    public class Producto
    {
        public int IdProducto { get; set; }

        public bool EsProductoUnico { get; set; } = true;

        public int CantidadDisponible { get; set; } = 1;

        public int IdUsuario { get; set; }

        public int IdCategoria { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public decimal Precio { get; set; }

        public string Condicion { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public DateTime FechaPublicacion { get; set; }

        public bool Activo { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}

using System;

namespace MarketplaceUAT.API.DTOs
{
    public class CarritoResponseDto
    {
        public int IdCarrito { get; set; }

        public int IdProducto { get; set; }

        public int IdUsuarioVendedor { get; set; }

        public string NombreVendedor { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public decimal Precio { get; set; }

        public string Condicion { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public bool Activo { get; set; }

        public bool EsProductoUnico { get; set; }

        public int CantidadDisponible { get; set; }

        public int Cantidad { get; set; }

        public DateTime FechaAgregado { get; set; }
    }
}

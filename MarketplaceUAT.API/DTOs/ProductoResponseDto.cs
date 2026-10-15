namespace MarketplaceUAT.API.DTOs
{
    public class ProductoResponseDto
    {
        public int IdProducto { get; set; }

        public int IdUsuario { get; set; }

        public int IdCategoria { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public decimal Precio { get; set; }

        public string Condicion { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public DateTime FechaPublicacion { get; set; }

        public bool Activo { get; set; }
    }
}

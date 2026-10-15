
namespace MarketplaceUAT.App.Models
{
    public class FavoritoProducto
    {
        public int IdFavorito { get; set; }

        public int IdProducto { get; set; }

        public DateTime FechaAgregado { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public decimal Precio { get; set; }

        public string Condicion { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public string? ImagenUrl { get; set; }
    }
}

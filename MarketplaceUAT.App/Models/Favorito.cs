namespace MarketplaceUAT.App.Models
{
    public class Favorito
    {
        public int IdFavorito { get; set; }
        public int IdUsuario { get; set; }
        public int IdProducto { get; set; }
        public DateTime FechaAgregado { get; set; }
    }
}

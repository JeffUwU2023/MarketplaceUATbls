namespace MarketplaceUAT.API.DTOs
{
    public class FavoritoResponseDto
    {
        public int IdFavorito { get; set; }

        public int IdUsuario { get; set; }

        public int IdProducto { get; set; }

        public DateTime FechaAgregado { get; set; }
    }
}
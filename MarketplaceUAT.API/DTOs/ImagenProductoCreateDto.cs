namespace MarketplaceUAT.API.DTOs
{
    public class ImagenProductoCreateDto
    {
        public int IdProducto { get; set; }

        public string UrlImagen { get; set; } = string.Empty;

        public bool EsPrincipal { get; set; }
    }
}
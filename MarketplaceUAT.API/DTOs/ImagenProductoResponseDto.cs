namespace MarketplaceUAT.API.DTOs
{
    public class ImagenProductoResponseDto
    {
        public int IdImagen { get; set; }

        public int IdProducto { get; set; }

        public string UrlImagen { get; set; } = string.Empty;

        public bool EsPrincipal { get; set; }

        public bool Activo { get; set; }
    }
}

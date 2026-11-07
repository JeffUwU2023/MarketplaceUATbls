using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.API.DTOs
{
    public class ImagenProductoUpdateDto
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "El ID del producto no es válido.")]
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "La ruta de la imagen es obligatoria.")]
        [StringLength(300,
            ErrorMessage = "La ruta de la imagen es demasiado larga.")]
        public string UrlImagen { get; set; } = string.Empty;

        public bool EsPrincipal { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.API.DTOs
{
    public class CategoriaCreateDto
    {
        [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
        [StringLength(30, MinimumLength = 3,
            ErrorMessage = "El nombre debe tener entre 3 y 30 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200,
            ErrorMessage = "La descripción no puede superar los 200 caracteres.")]
        public string? Descripcion { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.API.DTOs
{
    public class ProductoUpdateDto
    {
        [Required(ErrorMessage = "El usuario es obligatorio.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "El usuario no es válido.")]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "La categoría es obligatoria.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "La categoría no es válida.")]
        public int IdCategoria { get; set; }

        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        [StringLength(100, MinimumLength = 3,
            ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500,
            ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
        public string? Descripcion { get; set; }

        [Range(0.01, double.MaxValue,
            ErrorMessage = "El precio debe ser mayor que 0.")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "La condición es obligatoria.")]
        [StringLength(30, MinimumLength = 3,
                  ErrorMessage = "La condición debe tener entre 3 y 30 caracteres.")]
        [RegularExpression(
    "^(Nuevo|Usado)$",
                  ErrorMessage = "La condición debe ser Nuevo o Usado.")]
        public string Condicion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El estado es obligatorio.")]
        [StringLength(20, MinimumLength = 3,
                  ErrorMessage = "El estado debe tener entre 3 y 20 caracteres.")]
        [RegularExpression(
    "^(Disponible|Reservado|Vendido)$",
                  ErrorMessage = "El estado debe ser Disponible, Reservado o Vendido.")]
        public string Estado { get; set; } = string.Empty;
    }
}
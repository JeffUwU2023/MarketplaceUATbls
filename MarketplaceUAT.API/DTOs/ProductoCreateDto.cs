using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.API.DTOs
{
    public class ProductoCreateDto
    {
        [Required(ErrorMessage = "La categoría es obligatoria.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "La categoría no es válida.")]
        public int IdCategoria { get; set; }

        public bool EsProductoUnico { get; set; } = true;

        [Range(1, int.MaxValue,
            ErrorMessage = "La cantidad disponible debe ser al menos 1.")]
        public int CantidadDisponible { get; set; } = 1;

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
        "^(?i:Nuevo|Usado)$",
        ErrorMessage = "La condición debe ser Nuevo o Usado.")]
        public string Condicion { get; set; } = string.Empty;

    }
}
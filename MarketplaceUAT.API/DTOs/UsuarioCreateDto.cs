using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.API.DTOs
{
    public class UsuarioCreateDto
    {
        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Los nombres deben tener entre 2 y 100 caracteres.")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Los apellidos deben tener entre 2 y 100 caracteres.")]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de usuario es obligatorio.")]
        [RegularExpression("^(Alumno|Docente)$",
            ErrorMessage = "El tipo de usuario debe ser Alumno o Docente.")]
        public string TipoUsuario { get; set; } = string.Empty;

        [StringLength(10,
            ErrorMessage = "La matrícula no puede superar los 10 dígitos.")]
        [RegularExpression("^[0-9]{10}$",
            ErrorMessage = "La matrícula debe contener exactamente 10 dígitos.")]
        public string? Matricula { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [StringLength(150,
            ErrorMessage = "El correo no puede superar los 150 caracteres.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(20, MinimumLength = 8,
            ErrorMessage = "La contraseña debe tener entre 8 y 20 caracteres.")]
        public string Password { get; set; } = string.Empty;
    }
}

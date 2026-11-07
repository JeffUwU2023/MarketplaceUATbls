using System;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.Data.Models
{
    public class NotificacionTransaccion
    {
        [Key]
        public int IdNotificacion { get; set; }

        public int IdUsuario { get; set; }

        public int IdTransaccion { get; set; }

        [Required]
        [MaxLength(30)]
        public string Tipo { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string Mensaje { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public bool Leida { get; set; } = false;
    }
}

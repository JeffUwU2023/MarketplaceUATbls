using System;

namespace MarketplaceUAT.App.Models
{
    public class NotificacionTransaccionDto
    {
        public int IdNotificacion { get; set; }

        public int IdTransaccion { get; set; }

        public string Tipo { get; set; } = string.Empty;

        public string Mensaje { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; }

        public bool Leida { get; set; }
    }
}
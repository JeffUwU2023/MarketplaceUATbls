using System;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.Data.Models
{
    public class Transaccion
    {
        [Key]
        public int IdTransaccion { get; set; }

        public int IdProducto { get; set; }

        public int IdUsuarioComprador { get; set; }

        public int IdUsuarioVendedor { get; set; }

        public decimal PrecioVenta { get; set; }

        public int Cantidad { get; set; } = 1;

        public DateTime FechaTransaccion { get; set; }

        public string EstadoTransaccion { get; set; } = "Registrada";

        public bool ConfirmacionComprador { get; set; } = false;

        public bool ConfirmacionVendedor { get; set; } = false;

        public bool SolicitudCancelacionComprador { get; set; } = false;

        public bool SolicitudCancelacionVendedor { get; set; } = false;

        public bool AceptacionCancelacionComprador { get; set; } = false;

        public bool AceptacionCancelacionVendedor { get; set; } = false;

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}


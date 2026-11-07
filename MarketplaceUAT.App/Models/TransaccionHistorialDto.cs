namespace MarketplaceUAT.App.Models
{
    public class TransaccionHistorialDto
    {
        public int IdTransaccion { get; set; }

        public int IdProducto { get; set; }

        public string NombreProducto { get; set; } =
            "Producto no disponible";

        public int IdUsuarioComprador { get; set; }

        public int IdUsuarioVendedor { get; set; }

        public string NombreComprador { get; set; } = string.Empty;

        public string NombreVendedor { get; set; } = string.Empty;

        public decimal PrecioVenta { get; set; }

        public DateTime FechaTransaccion { get; set; }

        public string EstadoTransaccion { get; set; } = "Registrada";

        public bool ConfirmacionComprador { get; set; }

        public bool ConfirmacionVendedor { get; set; }

        public bool SolicitudCancelacionComprador { get; set; }

        public bool SolicitudCancelacionVendedor { get; set; }

        public bool AceptacionCancelacionComprador { get; set; }

        public bool AceptacionCancelacionVendedor { get; set; }

        public bool PuedeConfirmarCompra =>
            EstadoTransaccion == "Registrada" &&
            !SolicitudCancelacionComprador &&
            !SolicitudCancelacionVendedor;

        public bool PuedeSolicitarCancelacion =>
            EstadoTransaccion == "Registrada" &&
            !SolicitudCancelacionComprador &&
            !SolicitudCancelacionVendedor;


        public bool PuedeConfirmarVenta =>
            EstadoTransaccion == "Registrada" &&
            !SolicitudCancelacionComprador &&
            !SolicitudCancelacionVendedor;

        public bool PuedeSolicitarCancelacionVenta =>
            EstadoTransaccion == "Registrada" &&
            !SolicitudCancelacionComprador &&
            !SolicitudCancelacionVendedor;

        public bool PuedeAceptarCancelacion =>
            EstadoTransaccion == "Registrada" &&
            SolicitudCancelacionComprador &&
            !AceptacionCancelacionVendedor;

        public bool PuedeAceptarCancelacionCompra =>
            EstadoTransaccion == "Registrada" &&
            SolicitudCancelacionVendedor &&
            !AceptacionCancelacionComprador;

    }
}

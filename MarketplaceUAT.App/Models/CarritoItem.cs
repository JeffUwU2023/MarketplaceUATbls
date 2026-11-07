namespace MarketplaceUAT.App.Models
{
    public class CarritoItem
    {
        public int IdCarrito { get; set; }
        public int IdProducto { get; set; }
        public int IdUsuarioVendedor { get; set; }
        public string NombreVendedor { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public string Condicion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public bool EsProductoUnico { get; set; }
        public int CantidadDisponible { get; set; }
        public int Cantidad { get; set; } = 1;
        public DateTime FechaAgregado { get; set; }

        // Imagen del producto.
        public string ImagenUrl { get; set; } = string.Empty;

        // Subtotal según la cantidad seleccionada.
        public decimal Subtotal => Precio * Cantidad;

        public bool PermiteCambiarCantidad => !EsProductoUnico;

        // Detecta productos que ya no se pueden comprar.
        public bool EstaNoDisponible =>
            !Activo ||
            Estado != "Disponible" ||
            CantidadDisponible <= 0;

        // Detecta si la cantidad supera las existencias actuales.
        public bool ExcedeExistencias =>
            !EstaNoDisponible &&
            Cantidad > CantidadDisponible;

        // Mensaje que se mostrará al usuario.
        public string MensajeDisponibilidad
        {
            get
            {
                if (!Activo || Estado == "Vendido")
                    return "⚠️ Esta publicación ya no está disponible.";

                if (CantidadDisponible <= 0)
                    return "⚠️ Producto agotado.";

                if (Estado != "Disponible")
                    return $"⚠️ Estado actual: {Estado}.";

                if (Cantidad > CantidadDisponible)
                    return $"⚠️ Solo quedan {CantidadDisponible} unidades.";

                return string.Empty;
            }
        }

        public bool MostrarAdvertencia =>
            EstaNoDisponible || ExcedeExistencias;
    }
}
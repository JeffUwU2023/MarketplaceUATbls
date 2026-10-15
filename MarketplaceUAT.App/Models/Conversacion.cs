namespace MarketplaceUAT.App.Models
{
    public class Conversacion
    {
        public int IdConversacion { get; set; }

        public int IdProducto { get; set; }

        public string NombreProducto { get; set; } = string.Empty;
        public string NombreOtroUsuario { get; set; } = string.Empty;

        public int IdUsuarioComprador { get; set; }

        public int IdUsuarioVendedor { get; set; }

        public DateTime FechaCreacion { get; set; }

        public DateTime UltimaActividad { get; set; }

        public bool Archivada { get; set; }

        public bool Activa { get; set; }
    }
}
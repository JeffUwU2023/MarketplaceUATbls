using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.Data.Models
{
    public class Conversacion
    {
        [Key]
        public int IdConversacion { get; set; }

        public int IdProducto { get; set; }

        public int IdUsuarioComprador { get; set; }

        public int IdUsuarioVendedor { get; set; }

        public DateTime FechaCreacion { get; set; }

        public DateTime UltimaActividad { get; set; }

        public bool Archivada { get; set; }

        public bool Activa { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceUAT.Data.Models
{
    public class Favorito
    {
        [Key]
        public int IdFavorito { get; set; }

        public int IdUsuario { get; set; }

        public int IdProducto { get; set; }

        public DateTime FechaAgregado { get; set; }
    }
}
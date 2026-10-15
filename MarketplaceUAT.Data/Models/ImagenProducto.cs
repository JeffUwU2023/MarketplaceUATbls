using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceUAT.Data.Models
{
    public class ImagenProducto
    {
        [Key]
        public int IdImagen { get; set; }

        public int IdProducto { get; set; }

        public string UrlImagen { get; set; } = string.Empty;

        public bool EsPrincipal { get; set; }

        public bool Activo { get; set; }
    }
}
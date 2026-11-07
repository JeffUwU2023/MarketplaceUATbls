using System.Collections.ObjectModel;
using System.Linq;

namespace MarketplaceUAT.App.Models
{
    public class GrupoCarritoVendedor
    {
        public int IdUsuarioVendedor { get; set; }

        public string NombreVendedor { get; set; } = string.Empty;

        public ObservableCollection<CarritoItem> Productos { get; set; } = new();

        public decimal Subtotal => Productos.Sum(p => p.Subtotal);
    }
}
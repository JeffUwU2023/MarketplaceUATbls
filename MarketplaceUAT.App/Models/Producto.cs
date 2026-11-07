using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MarketplaceUAT.App.Models
{
    public class Producto : INotifyPropertyChanged
    {
        public int IdProducto { get; set; }
        public int IdUsuario { get; set; }
        public int IdCategoria { get; set; }
        public bool EsProductoUnico { get; set; } = true;
        public int CantidadDisponible { get; set; } = 1;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public string Condicion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string EstadoTexto
        {
            get
            {
                return Estado switch
                {
                    "Disponible" => "🟢 Disponible",
                    "Reservado" => "🟡 Reservado",
                    "Vendido" => "🔴 Vendido",
                    _ => Estado
                };
            }
        }

        public DateTime FechaPublicacion { get; set; }
        public string FechaPublicacionTexto =>
               ObtenerTiempoTranscurrido(FechaPublicacion);

        private static string ObtenerTiempoTranscurrido(DateTime fecha)
        {
            DateTime ahora = DateTime.Now;

            if (fecha > ahora)
                return "Publicado hace unos segundos";

            TimeSpan diferencia = ahora - fecha;

            if (diferencia.TotalSeconds < 60)
                return "Publicado hace unos segundos";

            if (diferencia.TotalMinutes < 60)
            {
                int minutos = (int)diferencia.TotalMinutes;

                return minutos == 1
                    ? "Publicado hace 1 minuto"
                    : $"Publicado hace {minutos} minutos";
            }

            if (diferencia.TotalHours < 24)
            {
                int horas = (int)diferencia.TotalHours;

                return horas == 1
                    ? "Publicado hace 1 hora"
                    : $"Publicado hace {horas} horas";
            }

            if (diferencia.TotalDays < 7)
            {
                int dias = (int)diferencia.TotalDays;

                return dias == 1
                    ? "Publicado hace 1 día"
                    : $"Publicado hace {dias} días";
            }

            if (diferencia.TotalDays < 30)
            {
                int semanas = (int)(diferencia.TotalDays / 7);

                return semanas == 1
                    ? "Publicado hace 1 semana"
                    : $"Publicado hace {semanas} semanas";
            }

            int meses = 0;
            DateTime fechaCalculada = fecha;

            while (fechaCalculada.AddMonths(1) <= ahora)
            {
                fechaCalculada = fechaCalculada.AddMonths(1);
                meses++;
            }

            if (meses < 12)
            {
                return meses == 1
                    ? "Publicado hace 1 mes"
                    : $"Publicado hace {meses} meses";
            }

            int años = 0;
            fechaCalculada = fecha;

            while (fechaCalculada.AddYears(1) <= ahora)
            {
                fechaCalculada = fechaCalculada.AddYears(1);
                años++;
            }

            return años == 1
                ? "Publicado hace 1 año"
                : $"Publicado hace {años} años";
        }

        public bool Activo { get; set; }

        public string? ImagenUrl { get; set; }

        private bool _esFavorito;

        public bool EsFavorito
        {
            get => _esFavorito;
            set
            {
                if (_esFavorito == value)
                    return;

                _esFavorito = value;
                OnPropertyChanged();
            }
        }

        private bool _estaEnCarrito;

        public bool EstaEnCarrito
        {
            get => _estaEnCarrito;
            set
            {
                if (_estaEnCarrito == value)
                    return;

                _estaEnCarrito = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? nombrePropiedad = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nombrePropiedad));
        }
    }
}


using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MarketplaceUAT.App.Models
{
    public class Mensaje : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string? _contenido;

        private bool _mostrarAcciones;

        public bool MostrarAcciones
        {
            get => _mostrarAcciones;
            set
            {
                if (_mostrarAcciones != value)
                {
                    _mostrarAcciones = value;
                    OnPropertyChanged();
                }
            }
        }

        public int IdMensaje { get; set; }

        public int IdConversacion { get; set; }

        public int IdUsuario { get; set; }

        public string? Contenido
        {
            get => _contenido;
            set
            {
                if (_contenido != value)
                {
                    _contenido = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TieneTexto));
                }
            }
        }

        public bool TieneTexto =>
            !string.IsNullOrWhiteSpace(Contenido);

        public string? UrlImagen { get; set; }

        public List<string> Imagenes { get; set; } = [];

        public DateTime FechaEnvio { get; set; }

        public bool Visto { get; set; }

        public bool EsMio { get; set; }

        public bool EsOtro => !EsMio;

        public string EstadoLectura =>
            Visto ? "✓ Visto" : "✓ Enviado";

        public bool TieneImagen =>
            !string.IsNullOrWhiteSpace(UrlImagen);

        public bool TieneImagenes =>
            Imagenes.Count > 0;

        private void OnPropertyChanged(
            [CallerMemberName] string? nombre = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nombre));
        }
    }
}

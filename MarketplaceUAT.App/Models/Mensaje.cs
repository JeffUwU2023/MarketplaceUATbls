using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MarketplaceUAT.App.Models
{
    public class Mensaje : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string? _contenido;
        private bool _mostrarAcciones;
        public bool MostrarBotonEditar => MostrarAcciones && TieneTexto;
        private bool _visto;

        public int IdMensaje { get; set; }

        public int IdConversacion { get; set; }

        public int IdUsuario { get; set; }

        // Texto del mensaje
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
                    OnPropertyChanged(nameof(MostrarBotonEditar));
                }
            }
        }

        // Indica si el mensaje contiene texto
        public bool TieneTexto =>
            !string.IsNullOrWhiteSpace(Contenido);

        // Imagen individual, si existe
        public string? UrlImagen { get; set; }

        // Lista de imágenes asociadas al mensaje
        public List<string> Imagenes { get; set; } = [];

        // Rutas locales de las imágenes descargadas con autenticación
        public List<string> ImagenesLocales { get; set; } = new();

        // Fecha y hora de envío
        public DateTime FechaEnvio { get; set; }

        // Estado de lectura con notificación automática
        public bool Visto
        {
            get => _visto;
            set
            {
                if (_visto != value)
                {
                    _visto = value;

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EstadoLectura));
                }
            }
        }

        // Indica si el mensaje pertenece al usuario actual
        public bool EsMio { get; set; }

        // Indica si el mensaje pertenece a la otra persona
        public bool EsOtro => !EsMio;

        // URL de la foto de perfil del remitente
        public string? UrlFotoPerfil { get; set; }

        // Indica si el remitente tiene una foto de perfil
        public bool TieneFotoPerfil =>
            !string.IsNullOrWhiteSpace(UrlFotoPerfil);

        // Texto del estado de lectura
        public string EstadoLectura =>
            Visto ? "✓ Visto" : "✓ Enviado";

        // Indica si hay una imagen individual
        public bool TieneImagen =>
            !string.IsNullOrWhiteSpace(UrlImagen);

        // Indica si hay imágenes en la colección
        public bool TieneImagenes =>
            Imagenes != null && Imagenes.Count > 0;

        // Controla la visibilidad de Editar y Eliminar
        public bool MostrarAcciones
        {
            get => _mostrarAcciones;
            set
            {
                if (_mostrarAcciones != value)
                {
                    _mostrarAcciones = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MostrarBotonEditar));
                }
            }
        }

        // Notifica los cambios a la interfaz
        private void OnPropertyChanged(
            [CallerMemberName] string? nombre = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nombre));
        }
    }
}


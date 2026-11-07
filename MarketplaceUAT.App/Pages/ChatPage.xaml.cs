using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MarketplaceUAT.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MarketplaceUAT.App.Pages
{
    [QueryProperty(nameof(IdConversacion), "idConversacion")]
    [QueryProperty(nameof(IdProducto), "idProducto")]
    [QueryProperty(nameof(IdUsuarioVendedor), "idUsuarioVendedor")]
    public partial class ChatPage : ContentPage
    {
        private ApiClient Api =>
    Application.Current!.Handler.MauiContext!.Services
        .GetRequiredService<ApiClient>();
        private readonly ObservableCollection<Mensaje> _mensajes = new();
        private readonly ObservableCollection<FileResult> _imagenesSeleccionadas = new();
        private Guid? _idSolicitudPendiente;
        private int? _idConversacionSolicitudPendiente;
        private CancellationTokenSource? _cargaCts;
        private bool _inicializando;
        private bool _conversacionArchivada;
        private bool _conversacionVerificada;
        private bool _estaAlFinal = true;
        private string? _urlFotoUsuarioActual;
        private string? _urlFotoOtroUsuario;
        private int? _mensajeSeleccionadoId;
        public Command<Mensaje> MostrarAccionesCommand { get; private set; }
        private string _idConversacion = string.Empty;
        private string _idProducto = string.Empty;
        private string _idUsuarioVendedor = string.Empty;

        private static readonly JsonSerializerOptions OpcionesJson = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public string IdConversacion
        {
            get => _idConversacion;
            set => _idConversacion = value ?? string.Empty;
        }

        public string IdProducto
        {
            get => _idProducto;
            set => _idProducto = value ?? string.Empty;
        }

        public string IdUsuarioVendedor
        {
            get => _idUsuarioVendedor;
            set => _idUsuarioVendedor = value ?? string.Empty;
        }

        public ChatPage()
        {
            InitializeComponent();

            MostrarAccionesCommand = new Command<Mensaje>(async mensaje =>
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (mensaje == null ||
                        !mensaje.EsMio ||
                        _conversacionArchivada)
                    {
                        return;
                    }

                    // Comprobar si se volvió a pulsar el mismo mensaje.
                    bool ocultar =
                        _mensajeSeleccionadoId == mensaje.IdMensaje &&
                        mensaje.MostrarAcciones;

                    // Ocultar las opciones de todos los mensajes.
                    foreach (var item in _mensajes)
                    {
                        item.MostrarAcciones = false;
                    }

                    if (ocultar)
                    {
                        // Segunda pulsación: ocultar las opciones.
                        _mensajeSeleccionadoId = null;
                    }
                    else
                    {
                        // Primera pulsación: mostrar las opciones.
                        mensaje.MostrarAcciones = true;
                        _mensajeSeleccionadoId = mensaje.IdMensaje;
                    }
                });
            });

            MensajesCollectionView.ItemsSource = _mensajes;
            ImagenesPreviewCollectionView.ItemsSource = _imagenesSeleccionadas;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_inicializando)
                return;

            _inicializando = true;

            _cargaCts?.Cancel();
            _cargaCts?.Dispose();
            _cargaCts = new CancellationTokenSource();

            try
            {
                await InicializarChatAsync(_cargaCts.Token);
            }
            finally
            {
                _inicializando = false;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _cargaCts?.Cancel();

            _imagenesSeleccionadas.Clear();
            ImagenPreviewBorder.IsVisible = false;
        }

        private string CrearUrl(string ruta)
        {
            return $"{ApiConfig.BaseUrl.TrimEnd('/')}/{ruta.TrimStart('/')}";
        }

        private async Task InicializarChatAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                IndicadorCarga.IsVisible = true;
                IndicadorCarga.IsRunning = true;
                CargandoMensajesLabel.IsVisible = true;

                if (!SesionUsuario.EstaLogueado)
                {
                    await DisplayAlertAsync(
                        "Cuenta requerida",
                        "Debes iniciar sesión para utilizar el chat.",
                        "OK");

                    await Shell.Current.GoToAsync("..");
                    return;
                }

                _conversacionArchivada = false;
                _conversacionVerificada = false;

                ConversacionArchivadaBorder.IsVisible = false;
                AreaEnvioGrid.IsVisible = true;
                ImagenPreviewBorder.IsVisible = false;

                if (int.TryParse(IdConversacion, out int idExistente) &&
                    idExistente > 0)
                {
                    bool encontrada =
                        await CargarDatosConversacionAsync(idExistente);

                    if (!encontrada)
                    {
                        _conversacionArchivada = true;
                        ConfigurarModoArchivado();

                        await DisplayAlertAsync(
                            "Conversación no disponible",
                            "No se pudo verificar el estado de esta conversación. " +
                            "Por seguridad, el envío permanecerá deshabilitado.",
                            "OK");

                        return;
                    }

                    await CargarMensajesAsync(cancellationToken);
                    return;
                }

                if (!int.TryParse(IdProducto, out int idProducto) ||
                    idProducto <= 0)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "No se recibió un identificador válido de conversación o producto.",
                        "OK");

                    return;
                }

                TituloChatLabel.Text = "Nueva conversación";
                SubtituloChatLabel.Text = "Contactar al vendedor";

                string urlProducto =
                    CrearUrl($"api/Conversaciones/Producto/{idProducto}");

                using var respuestaProducto =
    await Api.EnviarAutenticadoAsync(
        () => new HttpRequestMessage(
            HttpMethod.Get,
            urlProducto),
        cancellationToken);

                if (respuestaProducto.StatusCode ==
                    System.Net.HttpStatusCode.NoContent)
                {

                }
                else if (respuestaProducto.IsSuccessStatusCode)
                {
                    string json =
                        await respuestaProducto.Content.ReadAsStringAsync();

                    var conversacion =
                        JsonSerializer.Deserialize<Conversacion>(
                            json,
                            OpcionesJson);

                    if (conversacion == null ||
                        conversacion.IdConversacion <= 0)
                    {
                        await DisplayAlertAsync(
                            "Respuesta inválida",
                            "La API respondió correctamente, pero no devolvió " +
                            "una conversación válida.",
                            "OK");

                        return;
                    }

                    IdConversacion =
                        conversacion.IdConversacion.ToString();

                    bool encontrada =
                        await CargarDatosConversacionAsync(
                            conversacion.IdConversacion);

                    if (!encontrada)
                    {
                        _conversacionArchivada = true;
                        ConfigurarModoArchivado();

                        await DisplayAlertAsync(
                            "Aviso",
                            "No se pudo verificar el estado de la conversación.",
                            "OK");

                        return;
                    }

                    await CargarMensajesAsync(cancellationToken);
                    return;
                }
                else
                {
                    string error =
                        await respuestaProducto.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error",
                        "No se pudo consultar la conversación existente.\n\n" +
                        $"Código HTTP: {(int)respuestaProducto.StatusCode}\n{error}",
                        "OK");

                    return;
                }

                IdConversacion = string.Empty;
                _conversacionVerificada = true;

                _mensajes.Clear();
                MensajesCollectionView.ItemsSource = _mensajes;

                ConfigurarModoActivo();
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error de conexión",
                    $"No se pudo inicializar el chat.\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                IndicadorCarga.IsRunning = false;
                IndicadorCarga.IsVisible = false;
                CargandoMensajesLabel.IsVisible = false;
            }
        }

        private async Task<bool> CargarDatosConversacionAsync(
            int idConversacion)
        {
            try
            {
                string url = CrearUrl(
                    $"api/Conversaciones/{idConversacion}");

                using var respuesta =
    await Api.EnviarAutenticadoAsync(
        () => new HttpRequestMessage(
            HttpMethod.Get,
            url));

                if (!respuesta.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Error al consultar conversación: HTTP {(int)respuesta.StatusCode}");

                    return false;
                }

                string json = await respuesta.Content.ReadAsStringAsync();

                var conversacion =
                    JsonSerializer.Deserialize<Conversacion>(
                        json,
                        OpcionesJson);

                if (conversacion == null ||
                    conversacion.IdConversacion != idConversacion)
                {
                    return false;
                }

                IdConversacion = conversacion.IdConversacion.ToString();

                TituloChatLabel.Text =
                    string.IsNullOrWhiteSpace(conversacion.NombreProducto)
                        ? "Conversación"
                        : conversacion.NombreProducto;

                SubtituloChatLabel.Text =
                    string.IsNullOrWhiteSpace(conversacion.NombreOtroUsuario)
                        ? "Chat de Marketplace"
                        : $"Con {conversacion.NombreOtroUsuario}";

                _conversacionArchivada =
                    conversacion.Archivada || !conversacion.Activa;

                int idUsuarioActual =
    SesionUsuario.UsuarioActual?.IdUsuario ?? 0;

                int idOtroUsuario;

                if (idUsuarioActual == conversacion.IdUsuarioComprador)
                {
                    idOtroUsuario = conversacion.IdUsuarioVendedor;
                }
                else if (idUsuarioActual == conversacion.IdUsuarioVendedor)
                {
                    idOtroUsuario = conversacion.IdUsuarioComprador;
                }
                else
                {
                    _conversacionVerificada = false;
                    return false;
                }

                _conversacionVerificada = true;

                _urlFotoUsuarioActual =
                    await ObtenerFotoPerfilAsync(idUsuarioActual);

                _urlFotoOtroUsuario =
                    await ObtenerFotoPerfilAsync(idOtroUsuario);

                if (_conversacionArchivada)
                    ConfigurarModoArchivado();
                else
                    ConfigurarModoActivo();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error al cargar datos de la conversación: {ex.Message}");

                return false;
            }
        }

        private void ConfigurarModoArchivado()
        {
            _conversacionArchivada = true;

            ConversacionArchivadaBorder.IsVisible = true;
            EliminarConversacionButton.IsVisible =
                _conversacionVerificada &&
                int.TryParse(IdConversacion, out int id) &&
                id > 0;

            AreaEnvioGrid.IsVisible = false;
            ImagenPreviewBorder.IsVisible = false;

            _imagenesSeleccionadas.Clear();

            foreach (var mensaje in _mensajes)
            {
                mensaje.MostrarAcciones = false;
            }
        }

        private void ConfigurarModoActivo()
        {
            _conversacionArchivada = false;

            ConversacionArchivadaBorder.IsVisible = false;
            EliminarConversacionButton.IsVisible = false;

            AreaEnvioGrid.IsVisible = true;

            SeleccionarImagenButton.IsEnabled = true;
            MensajeEntry.IsEnabled = true;
            EnviarMensajeButton.IsEnabled = true;
        }

        private async Task CargarMensajesAsync(
            CancellationToken cancellationToken)
        {
            if (!int.TryParse(IdConversacion, out int idConversacion) ||
                idConversacion <= 0)
            {
                return;
            }

            try
            {
                CargandoMensajesLabel.IsVisible = true;

                string url =
                    CrearUrl($"api/Mensajes/Conversacion/{idConversacion}");

                using var respuesta =
    await Api.EnviarAutenticadoAsync(
        () => new HttpRequestMessage(
            HttpMethod.Get,
            url),
        cancellationToken);

                if (!respuesta.IsSuccessStatusCode)
                {
                    string error =
                        await respuesta.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error",
                        "No se pudieron cargar los mensajes.\n\n" +
                        $"Código HTTP: {(int)respuesta.StatusCode}\n{error}",
                        "OK");

                    return;
                }

                string json =
                    await respuesta.Content.ReadAsStringAsync();

                var mensajes = JsonSerializer.Deserialize<List<Mensaje>>(
                    json,
                    OpcionesJson);

                if (mensajes == null)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "La API no devolvió una lista válida de mensajes.",
                        "OK");

                    return;
                }

                int idUsuarioActual =
                    SesionUsuario.UsuarioActual?.IdUsuario ?? 0;

                _mensajes.Clear();

                foreach (var mensaje in mensajes.OrderBy(m => m.FechaEnvio))
                {
                    mensaje.EsMio = mensaje.IdUsuario == idUsuarioActual;
                    mensaje.UrlFotoPerfil = mensaje.EsMio
                          ? _urlFotoUsuarioActual
                          : _urlFotoOtroUsuario;

                    var imagenesNormalizadas = new List<string>();

                    if (mensaje.Imagenes != null)
                    {
                        foreach (var imagen in mensaje.Imagenes)
                        {
                            if (!string.IsNullOrWhiteSpace(imagen))
                            {
                                string urlImagen = ObtenerUrlImagen(imagen);

                                if (!imagenesNormalizadas.Contains(
                                    urlImagen,
                                    StringComparer.OrdinalIgnoreCase))
                                {
                                    imagenesNormalizadas.Add(urlImagen);
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(mensaje.UrlImagen))
                    {
                        string urlImagen =
                            ObtenerUrlImagen(mensaje.UrlImagen);

                        if (!imagenesNormalizadas.Contains(
                            urlImagen,
                            StringComparer.OrdinalIgnoreCase))
                        {
                            imagenesNormalizadas.Add(urlImagen);
                        }
                    }

                    // Conservar las URL originales recibidas de la API.  
                    mensaje.Imagenes = imagenesNormalizadas;

                    // Preparar las rutas locales de las imágenes descargadas.  
                    mensaje.ImagenesLocales = new List<string>();

                    foreach (string urlImagen in mensaje.Imagenes)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        string? rutaLocal = await DescargarImagenChatAsync(
                            urlImagen,
                            cancellationToken);

                        if (!string.IsNullOrWhiteSpace(rutaLocal))
                        {
                            mensaje.ImagenesLocales.Add(rutaLocal);
                        }
                    }

                    mensaje.MostrarAcciones = false;

                    _mensajes.Add(mensaje);
                }

                NuevosMensajesButton.IsVisible = false;

                if (_mensajes.Count > 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Task.Delay(100, cancellationToken);

                        if (_mensajes.Count > 0)
                        {
                            MensajesCollectionView.ScrollTo(
                                _mensajes[^1],
                                position: ScrollToPosition.End,
                                animate: false);
                        }
                    });
                }

                if (!_conversacionArchivada)
                    _ = MarcarMensajesPendientesComoLeidosAsync();
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudieron cargar los mensajes.\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                CargandoMensajesLabel.IsVisible = false;
            }
        }

        private string ObtenerUrlImagen(string ruta)
        {
            if (Uri.TryCreate(ruta, UriKind.Absolute, out var uriAbsoluta))
                return uriAbsoluta.ToString();

            var baseUri = new Uri(
                ApiConfig.BaseUrl.TrimEnd('/') + "/");

            return new Uri(baseUri, ruta.TrimStart('/')).ToString();
        }

        private async Task MarcarMensajesPendientesComoLeidosAsync()
        {
            if (_conversacionArchivada ||
                !SesionUsuario.EstaLogueado)
            {
                return;
            }

            var pendientes = _mensajes
                .Where(m => !m.EsMio && !m.Visto)
                .Select(m => m.IdMensaje)
                .ToList();

            foreach (int idMensaje in pendientes)
            {
                try
                {
                    string url =
                        CrearUrl($"api/Mensajes/{idMensaje}/Leer");

                    using var respuesta =
    await Api.EnviarAutenticadoAsync(
        () => new HttpRequestMessage(
            HttpMethod.Put,
            url));

                    if (respuesta.IsSuccessStatusCode)
                    {
                        var mensaje = _mensajes.FirstOrDefault(
                            m => m.IdMensaje == idMensaje);

                        if (mensaje != null)
                            mensaje.Visto = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"No se pudo marcar el mensaje {idMensaje} como leído: " +
                        ex.Message);
                }
            }
        }

        private void OnMensajesScrolled(
            object sender,
            ItemsViewScrolledEventArgs e)
        {
            if (_mensajes.Count == 0)
            {
                _estaAlFinal = true;
                NuevosMensajesButton.IsVisible = false;
                return;
            }

            _estaAlFinal =
                e.LastVisibleItemIndex >= _mensajes.Count - 1;

            NuevosMensajesButton.IsVisible = !_estaAlFinal;
        }
        private async void OnNuevosMensajesClicked(
            object sender,
            EventArgs e)
        {
            if (_mensajes.Count == 0)
                return;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Task.Delay(100);

                if (_mensajes.Count > 0)
                {
                    MensajesCollectionView.ScrollTo(
                        _mensajes[^1],
                        position: ScrollToPosition.End,
                        animate: true);
                }
            });
        }

        private async void OnSeleccionarImagenClicked(
            object sender,
            EventArgs e)
        {
            if (_conversacionArchivada)
                return;

            try
            {
                var resultados = await MediaPicker.PickPhotosAsync(
                    new MediaPickerOptions
                    {
                        Title = "Selecciona imágenes",
                        SelectionLimit = 5
                    });

                if (resultados == null)
                    return;

                var nuevasImagenes = resultados.ToList();

                int disponibles = 5 - _imagenesSeleccionadas.Count;

                if (nuevasImagenes.Count > disponibles)
                {
                    nuevasImagenes = nuevasImagenes
                        .Take(Math.Max(0, disponibles))
                        .ToList();

                    await DisplayAlertAsync(
                        "Límite de imágenes",
                        "Puedes enviar un máximo de cinco imágenes por mensaje.",
                        "OK");
                }

                foreach (var imagen in nuevasImagenes)
                {
                    if (imagen != null &&
                        !_imagenesSeleccionadas.Contains(imagen))
                    {
                        _imagenesSeleccionadas.Add(imagen);
                    }
                }

                ImagenPreviewBorder.IsVisible =
                    _imagenesSeleccionadas.Count > 0;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudieron seleccionar las imágenes.\n\n{ex.Message}",
                    "OK");
            }
        }

        private void OnEliminarImagenSeleccionadaClicked(
            object sender,
            EventArgs e)
        {
            if (sender is Button boton &&
                boton.CommandParameter is FileResult imagen)
            {
                _imagenesSeleccionadas.Remove(imagen);

                ImagenPreviewBorder.IsVisible =
                    _imagenesSeleccionadas.Count > 0;
            }
        }

        private async Task<bool> AsegurarConversacionAsync()
        {
            if (int.TryParse(IdConversacion, out int idActual) &&
    idActual > 0)
            {
                bool conversacionVerificada =
     await CargarDatosConversacionAsync(idActual);

                if (!conversacionVerificada)
                {
                    _conversacionVerificada = false;

                    await DisplayAlertAsync(
                        "Aviso",
                        "No se pudo verificar que tengas acceso a esta conversación.",
                        "OK");

                    return false;
                }

                if (_conversacionArchivada)
                {
                    await DisplayAlertAsync(
                        "Conversación archivada",
                        "Esta conversación no admite mensajes nuevos.",
                        "OK");

                    return false;
                }

                return true;
            }

            if (!int.TryParse(IdProducto, out int idProducto) ||
                idProducto <= 0)
            {
                await DisplayAlertAsync(
                    "Error",
                    "No se pudo identificar el producto.",
                    "OK");

                return false;
            }

            var datos = new
            {
                IdProducto = idProducto
            };

            string url = CrearUrl("api/Conversaciones");

            using var respuesta =
                await Api.EnviarAutenticadoAsync(
                    () =>
                    {
                        string json = JsonSerializer.Serialize(datos);

                        var contenido = new StringContent(
                            json,
                            Encoding.UTF8,
                            "application/json");

                        return new HttpRequestMessage(
                            HttpMethod.Post,
                            url)
                        {
                            Content = contenido
                        };
                    });
            if (!respuesta.IsSuccessStatusCode)
            {
                string error =
                    await respuesta.Content.ReadAsStringAsync();

                await DisplayAlertAsync(
                    "Error",
                    "No se pudo crear o recuperar la conversación.\n\n" +
                    $"Código HTTP: {(int)respuesta.StatusCode}\n{error}",
                    "OK");

                return false;
            }

            string respuestaJson =
                await respuesta.Content.ReadAsStringAsync();

            var conversacion =
                JsonSerializer.Deserialize<Conversacion>(
                    respuestaJson,
                    OpcionesJson);

            if (conversacion == null ||
                conversacion.IdConversacion <= 0)
            {
                await DisplayAlertAsync(
                    "Error",
                    "La API no devolvió un identificador válido de conversación.",
                    "OK");

                return false;
            }

            IdConversacion = conversacion.IdConversacion.ToString();

            bool encontrada =
                await CargarDatosConversacionAsync(
                    conversacion.IdConversacion);

            if (!encontrada)
            {
                await DisplayAlertAsync(
                    "Aviso",
                    "Se obtuvo el identificador, pero no se pudo verificar " +
                    "el estado de la conversación.",
                    "OK");

                return false;
            }

            if (_conversacionArchivada)
            {
                await DisplayAlertAsync(
                    "Conversación archivada",
                    "Esta conversación no admite mensajes nuevos.",
                    "OK");

                return false;
            }

            return true;
        }

        private sealed class RespuestaSubidaImagen
        {
            public string? UrlImagen { get; set; }
        }

        private async Task<string?> SubirImagenMensajeAsync(
    FileResult imagen,
    int idConversacion)
        {
            string extension =
                Path.GetExtension(imagen.FileName).ToLowerInvariant();

            string tipoContenido = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            string url = CrearUrl(
                $"api/ImagenesMensaje/Subir?idConversacion={idConversacion}");

            using var respuesta =
                await Api.EnviarAutenticadoAsync(
                    async () =>
                    {
                        var stream = await imagen.OpenReadAsync();

                        var archivoContenido = new StreamContent(stream);

                        archivoContenido.Headers.ContentType =
                            new MediaTypeHeaderValue(tipoContenido);

                        var contenido = new MultipartFormDataContent();

                        contenido.Add(
                            archivoContenido,
                            "archivo",
                            imagen.FileName);

                        return new HttpRequestMessage(
                            HttpMethod.Post,
                            url)
                        {
                            Content = contenido
                        };
                    });

            if (!respuesta.IsSuccessStatusCode)
            {
                string error =
                    await respuesta.Content.ReadAsStringAsync();

                throw new Exception(
                    $"No se pudo subir {imagen.FileName}. " +
                    $"HTTP {(int)respuesta.StatusCode}: {error}");
            }

            string json =
                await respuesta.Content.ReadAsStringAsync();

            var resultado =
                JsonSerializer.Deserialize<RespuestaSubidaImagen>(
                    json,
                    OpcionesJson);

            if (string.IsNullOrWhiteSpace(resultado?.UrlImagen))
            {
                throw new Exception(
                    "La API no devolvió la URL de la imagen subida.");
            }

            return resultado.UrlImagen;
        }

        private async Task<bool> RegistrarImagenMensajeAsync(
            int idMensaje,
            string urlImagen)
        {
            var datos = new
            {
                IdMensaje = idMensaje,
                UrlImagen = urlImagen
            };

            string url = CrearUrl("api/ImagenesMensaje");

            using var respuesta =
                await Api.EnviarAutenticadoAsync(
                    () =>
                    {
                        string json = JsonSerializer.Serialize(datos);

                        var contenido = new StringContent(
                            json,
                            Encoding.UTF8,
                            "application/json");

                        return new HttpRequestMessage(
                            HttpMethod.Post,
                            url)
                        {
                            Content = contenido
                        };
                    });

            if (respuesta.IsSuccessStatusCode)
                return true;

            string error = await respuesta.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(
                $"Error al registrar imagen: HTTP {(int)respuesta.StatusCode}: {error}");

            return false;
        }

        private async Task<bool> IntentarEliminarMensajeIncompletoAsync(
    int idMensaje)
        {
            try
            {
                string url = CrearUrl($"api/Mensajes/{idMensaje}");

                using var respuesta =
                    await Api.EnviarAutenticadoAsync(
                        () => new HttpRequestMessage(
                            HttpMethod.Delete,
                            url));

                return respuesta.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error al limpiar mensaje incompleto: {ex.Message}");

                return false;
            }
        }

        private async void OnEnviarMensajeClicked(
            object sender,
            EventArgs e)
        {
            if (_conversacionArchivada)
                return;

            string texto = MensajeEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(texto) &&
                _imagenesSeleccionadas.Count == 0)
            {
                return;
            }

            if (!SesionUsuario.EstaLogueado)
            {
                await DisplayAlertAsync(
                    "Cuenta requerida",
                    "Debes iniciar sesión para enviar mensajes.",
                    "OK");

                return;
            }

            EnviarMensajeButton.IsEnabled = false;
            SeleccionarImagenButton.IsEnabled = false;
            MensajeEntry.IsEnabled = false;

            try
            {

                bool conversacionLista =
                    await AsegurarConversacionAsync();

                if (!conversacionLista)
                    return;

                if (_conversacionArchivada ||
                    !int.TryParse(IdConversacion, out int idConversacion) ||
                    idConversacion <= 0)
                {
                    return;
                }

                var imagenesPorEnviar =
                    _imagenesSeleccionadas.ToList();

                if (_idConversacionSolicitudPendiente != idConversacion)
                {
                    _idSolicitudPendiente = null;
                    _idConversacionSolicitudPendiente = idConversacion;
                }

                _idSolicitudPendiente ??= Guid.NewGuid();

                Guid idSolicitud = _idSolicitudPendiente.Value;

                var datosMensaje = new
                {
                    IdConversacion = idConversacion,
                    IdSolicitud = idSolicitud,
                    Contenido = string.IsNullOrWhiteSpace(texto)
                        ? null
                        : texto,
                    UrlImagen = (string?)null,
                    TieneImagenes = imagenesPorEnviar.Count > 0
                };

                string urlMensaje = CrearUrl("api/Mensajes");

                using var respuesta =
                    await Api.EnviarAutenticadoAsync(
                        () =>
                        {
                            string json = JsonSerializer.Serialize(datosMensaje);

                            var contenido = new StringContent(
                                json,
                                Encoding.UTF8,
                                "application/json");

                            return new HttpRequestMessage(
                                HttpMethod.Post,
                                urlMensaje)
                            {
                                Content = contenido
                            };
                        });

                if (!respuesta.IsSuccessStatusCode)
                {
                    string error =
                        await respuesta.Content.ReadAsStringAsync();

                    if (respuesta.StatusCode ==
                        System.Net.HttpStatusCode.Gone)
                    {
                        _idSolicitudPendiente = null;

                        await DisplayAlertAsync(
                            "Reintento de envío",
                            "El mensaje anterior fue eliminado y ya no puede reutilizarse. " +
                            "Se generará un nuevo identificador cuando vuelvas a enviar.",
                            "OK");

                        return;
                    }

                    await DisplayAlertAsync(
                        "Error",
                        "No se pudo enviar el mensaje.\n\n" +
                        $"Código HTTP: {(int)respuesta.StatusCode}\n{error}",
                        "OK");

                    return;
                }

                string respuestaJson =
                    await respuesta.Content.ReadAsStringAsync();

                var mensajeCreado =
                    JsonSerializer.Deserialize<Mensaje>(
                        respuestaJson,
                        OpcionesJson);

                if (mensajeCreado == null ||
                    mensajeCreado.IdMensaje <= 0)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "El servidor respondió, pero no devolvió un identificador válido del mensaje.",
                        "OK");

                    return;
                }

                var urlsImagenesSubidas = new List<string>();

                try
                {
                    foreach (var imagen in imagenesPorEnviar)
                    {
                        string? urlImagen = await SubirImagenMensajeAsync(
                            imagen,
                            idConversacion);

                        if (string.IsNullOrWhiteSpace(urlImagen))
                        {
                            throw new Exception(
                                $"No se obtuvo la URL de {imagen.FileName}.");
                        }

                        bool registrada =
                            await RegistrarImagenMensajeAsync(
                                mensajeCreado.IdMensaje,
                                urlImagen);

                        if (!registrada)
                        {
                            throw new Exception(
                                $"La imagen {imagen.FileName} se subió, " +
                                "pero no se pudo registrar en el mensaje.");
                        }

                        urlsImagenesSubidas.Add(
                            ObtenerUrlImagen(urlImagen));
                    }
                }
                catch (Exception exImagen)
                {
                    bool limpiado =
                        await IntentarEliminarMensajeIncompletoAsync(
                            mensajeCreado.IdMensaje);

                    if (limpiado)
                    {
                        _idSolicitudPendiente = null;
                        _idConversacionSolicitudPendiente = null;
                    }

                    await DisplayAlertAsync(
                        "Envío incompleto",
                        $"{exImagen.Message}\n\n" +
                        (limpiado
                            ? "Se retiró el mensaje incompleto de la conversación."
                            : "No se pudo confirmar la eliminación del mensaje incompleto.") +
                        "\n\nEl texto y las imágenes seleccionadas se conservaron para que puedas intentarlo de nuevo.",
                        "OK");

                    await CargarMensajesAsync(
                        CancellationToken.None);

                    return;
                }

                MensajeEntry.Text = string.Empty;
                _imagenesSeleccionadas.Clear();
                ImagenPreviewBorder.IsVisible = false;

                await MostrarMensajeEnviadoAsync(
                    mensajeCreado,
                    urlsImagenesSubidas);

                _idSolicitudPendiente = null;
                _idConversacionSolicitudPendiente = null;

            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error de conexión",
                    $"No se pudo completar el envío.\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                bool permitirEdicion =
                    !_conversacionArchivada && _conversacionVerificada;

                EnviarMensajeButton.IsEnabled = permitirEdicion;
                SeleccionarImagenButton.IsEnabled = permitirEdicion;
                MensajeEntry.IsEnabled = permitirEdicion;
            }
        }

        private async void OnEditarMensajeClicked(
            object sender,
            EventArgs e)
        {
            if (_conversacionArchivada ||
                sender is not Button boton ||
                boton.CommandParameter is not Mensaje mensaje ||
                !mensaje.EsMio ||
                string.IsNullOrWhiteSpace(mensaje.Contenido))
            {
                return;
            }

            string? nuevoContenido = await DisplayPromptAsync(
                "Editar mensaje",
                "Modifica el texto de tu mensaje:",
                accept: "Guardar",
                cancel: "Cancelar",
                initialValue: mensaje.Contenido,
                maxLength: 1000);

            if (nuevoContenido == null)
                return;

            nuevoContenido = nuevoContenido.Trim();

            if (string.IsNullOrWhiteSpace(nuevoContenido))
            {
                await DisplayAlertAsync(
                    "Aviso",
                    "El mensaje no puede quedar vacío.",
                    "OK");

                return;
            }

            try
            {

                var datos = new
                {
                    Contenido = nuevoContenido
                };

                string url =
    CrearUrl($"api/Mensajes/{mensaje.IdMensaje}");

                using var respuesta =
                    await Api.EnviarAutenticadoAsync(
                        () =>
                        {
                            string json = JsonSerializer.Serialize(datos);

                            var contenido = new StringContent(
                                json,
                                Encoding.UTF8,
                                "application/json");

                            return new HttpRequestMessage(
                                HttpMethod.Put,
                                url)
                            {
                                Content = contenido
                            };
                        });

                if (!respuesta.IsSuccessStatusCode)
                {
                    string error =
                        await respuesta.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error",
                        "No se pudo editar el mensaje.\n\n" +
                        $"Código HTTP: {(int)respuesta.StatusCode}\n{error}",
                        "OK");

                    return;
                }

                mensaje.Contenido = nuevoContenido;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudo editar el mensaje.\n\n{ex.Message}",
                    "OK");
            }
        }

        private async void OnEliminarMensajeClicked(
            object sender,
            EventArgs e)
        {
            if (_conversacionArchivada ||
                sender is not Button boton ||
                boton.CommandParameter is not Mensaje mensaje ||
                !mensaje.EsMio)
            {
                return;
            }

            bool confirmar = await DisplayAlertAsync(
                "Eliminar mensaje",
                "¿Quieres eliminar este mensaje?",
                "Sí",
                "No");

            if (!confirmar)
                return;

            try
            {
                string url =
                    CrearUrl($"api/Mensajes/{mensaje.IdMensaje}");

                using var respuesta =
    await Api.EnviarAutenticadoAsync(
        () => new HttpRequestMessage(
            HttpMethod.Delete,
            url));

                if (!respuesta.IsSuccessStatusCode)
                {
                    string error =
                        await respuesta.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error",
                        "No se pudo eliminar el mensaje.\n\n" +
                        $"Código HTTP: {(int)respuesta.StatusCode}\n{error}",
                        "OK");

                    return;
                }

                _mensajes.Remove(mensaje);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudo eliminar el mensaje.\n\n{ex.Message}",
                    "OK");
            }
        }

        private async Task<string?> DescargarImagenChatAsync(
    string urlImagen,
    CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(urlImagen))
                    return null;

                if (!Uri.TryCreate(
                    urlImagen,
                    UriKind.Absolute,
                    out var uriImagen))
                {
                    return null;
                }

                // Obtener el nombre del archivo de la URL.  
                string nombreArchivo =
                    Path.GetFileName(uriImagen.LocalPath);

                if (string.IsNullOrWhiteSpace(nombreArchivo))
                    return null;

                // Crear una carpeta temporal para las imágenes del chat.  
                string carpetaImagenes = Path.Combine(
                    FileSystem.CacheDirectory,
                    "ChatImages");

                Directory.CreateDirectory(carpetaImagenes);

                string rutaLocal = Path.Combine(
                    carpetaImagenes,
                    nombreArchivo);

                // Si la imagen ya está descargada, reutilizarla.  
                if (File.Exists(rutaLocal))
                    return rutaLocal;

                // Descargar la imagen usando el cliente compartido.
                // Esta URL corresponde a una imagen pública.
                using var respuesta = await Api.EnviarAutenticadoAsync(
                () => new HttpRequestMessage(HttpMethod.Get, uriImagen),
                cancellationToken);

                if (!respuesta.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"No se pudo descargar la imagen. HTTP: " +
                        $"{(int)respuesta.StatusCode}");

                    return null;
                }

                byte[] bytes = await respuesta.Content
                    .ReadAsByteArrayAsync(cancellationToken);

                if (bytes.Length == 0)
                    return null;

                await File.WriteAllBytesAsync(
                    rutaLocal,
                    bytes,
                    cancellationToken);

                return rutaLocal;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error al descargar imagen del chat: {ex.Message}");

                return null;
            }
        }

        private async Task MostrarMensajeEnviadoAsync(
            Mensaje mensaje,
            List<string> urlsImagenes)
        {
            int idUsuarioActual =
            SesionUsuario.UsuarioActual?.IdUsuario ?? 0;

            mensaje.EsMio = mensaje.IdUsuario == idUsuarioActual;

            mensaje.UrlFotoPerfil = mensaje.EsMio
                ? _urlFotoUsuarioActual
                : _urlFotoOtroUsuario;

            mensaje.MostrarAcciones = false;

            mensaje.Imagenes = urlsImagenes
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            mensaje.UrlImagen = mensaje.Imagenes.FirstOrDefault();

            // Preparar las rutas locales para mostrar las imágenes inmediatamente.  
            mensaje.ImagenesLocales = new List<string>();

            foreach (string urlImagen in mensaje.Imagenes)
            {
                string? rutaLocal = await DescargarImagenChatAsync(
                    urlImagen,
                    CancellationToken.None);

                if (!string.IsNullOrWhiteSpace(rutaLocal))
                {
                    mensaje.ImagenesLocales.Add(rutaLocal);
                }
            }

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Evita agregar dos veces el mismo mensaje.  
                if (_mensajes.Any(m => m.IdMensaje == mensaje.IdMensaje))
                    return;

                _mensajes.Add(mensaje);

                NuevosMensajesButton.IsVisible = false;

                await Task.Yield();

                MensajesCollectionView.ScrollTo(
                    mensaje,
                    position: ScrollToPosition.End,
                    animate: false);
            });
        }

        private async void OnEliminarConversacionClicked(
            object sender,
            EventArgs e)
        {
            if (!_conversacionArchivada ||
                !_conversacionVerificada ||
                !int.TryParse(IdConversacion, out int idConversacion) ||
                idConversacion <= 0)
            {
                return;
            }

            bool confirmar = await DisplayAlertAsync(
                "Eliminar conversación",
                "Se eliminarán esta conversación, sus mensajes y las imágenes registradas. " +
                "Esta acción no se puede deshacer. La transacción de venta y la publicación " +
                "se conservarán. ¿Deseas continuar?",
                "Sí, eliminar",
                "Cancelar");

            if (!confirmar)
                return;

            try
            {
                EliminarConversacionButton.IsEnabled = false;

                string url = CrearUrl(
                    $"api/Conversaciones/{idConversacion}");

                using var respuesta =
    await Api.EnviarAutenticadoAsync(
        () => new HttpRequestMessage(
            HttpMethod.Delete,
            url));

                if (!respuesta.IsSuccessStatusCode)
                {
                    string error =
                        await respuesta.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error",
                        "No se pudo eliminar la conversación.\n\n" +
                        $"Código HTTP: {(int)respuesta.StatusCode}\n{error}",
                        "OK");

                    return;
                }

                await DisplayAlertAsync(
                    "Conversación eliminada",
                    "La conversación y sus mensajes se eliminaron correctamente.",
                    "OK");

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error de conexión",
                    $"No se pudo eliminar la conversación.\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                EliminarConversacionButton.IsEnabled = true;
            }
        }

        private async void OnVerPerfilClicked(object sender, EventArgs e)
        {
            try
            {
                if (!SesionUsuario.EstaLogueado ||
                    SesionUsuario.UsuarioActual == null)
                {
                    await DisplayAlertAsync(
                        "Cuenta requerida",
                        "Inicia sesión para consultar este perfil.",
                        "Aceptar");

                    return;
                }

                if (!int.TryParse(IdConversacion, out int idConversacion) ||
                    idConversacion <= 0)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "No se pudo identificar la conversación.",
                        "Aceptar");

                    return;
                }

                // Obtener la conversación para identificar al otro participante.
                string url = CrearUrl(
                    $"api/Conversaciones/{idConversacion}");

                using var respuesta =
                    await Api.EnviarAutenticadoAsync(
                        () => new HttpRequestMessage(
                            HttpMethod.Get,
                            url));

                if (!respuesta.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "No se pudo obtener la información de la conversación.",
                        "Aceptar");

                    return;
                }

                var conversacion = await respuesta.Content
                    .ReadFromJsonAsync<Conversacion>();

                if (conversacion == null)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "No se encontraron los datos de la conversación.",
                        "Aceptar");

                    return;
                }

                int idUsuarioActual = SesionUsuario.UsuarioActual.IdUsuario;

                int idOtroUsuario;

                if (idUsuarioActual == conversacion.IdUsuarioComprador)
                {
                    idOtroUsuario = conversacion.IdUsuarioVendedor;
                }
                else if (idUsuarioActual == conversacion.IdUsuarioVendedor)
                {
                    idOtroUsuario = conversacion.IdUsuarioComprador;
                }
                else
                {
                    await DisplayAlertAsync(
                        "Acceso denegado",
                        "No formas parte de esta conversación.",
                        "Aceptar");

                    return;
                }

                await Shell.Current.GoToAsync(
                nameof(PerfilPublicoPage),
                new Dictionary<string, object>
                {
                    ["IdUsuario"] = idOtroUsuario.ToString()
                });
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudo abrir el perfil: {ex.Message}",
                    "Aceptar");
            }
        }
        private async Task<string?> ObtenerFotoPerfilAsync(int idUsuario)
        {
            try
            {
                string url = CrearUrl($"api/Usuarios/{idUsuario}");

                using var respuesta =
    await Api.EnviarAutenticadoAsync(
        () => new HttpRequestMessage(
            HttpMethod.Get,
            url));

                if (!respuesta.IsSuccessStatusCode)
                    return null;

                string json = await respuesta.Content.ReadAsStringAsync();

                var usuario = JsonSerializer.Deserialize<UsuarioPerfil>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                System.Diagnostics.Debug.WriteLine(
                $"Perfil {idUsuario}: " +
                $"Usuario encontrado = {usuario != null}, " +
                $"Foto recibida = {usuario?.UrlFotoPerfil ?? "(sin foto)"}");
                if (usuario == null ||
                    string.IsNullOrWhiteSpace(usuario.UrlFotoPerfil))
                {
                    return null;
                }

                return $"{ApiConfig.BaseUrl.TrimEnd('/')}/{usuario.UrlFotoPerfil.TrimStart('/')}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error al obtener la foto del usuario {idUsuario}: {ex.Message}");

                return null;
            }
        }
    }
}
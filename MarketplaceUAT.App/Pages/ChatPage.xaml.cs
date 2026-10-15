using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.ObjectModel;

namespace MarketplaceUAT.App.Pages
{
    [QueryProperty(nameof(IdConversacion), "idConversacion")]
    [QueryProperty(nameof(IdProducto), "idProducto")]
    [QueryProperty(nameof(IdUsuarioVendedor), "idUsuarioVendedor")]
    public partial class ChatPage : ContentPage
    {
        private ObservableCollection<Mensaje> mensajesActuales = new();
        private string idConversacion = string.Empty;
        private string idProducto = string.Empty;
        private string idUsuarioVendedor = string.Empty;
        private bool omitirCargaAutomatica = false;
        private bool EsCargaInicial = true;
        private bool EstaAlFinal = true;
        private int? mensajeSeleccionadoId;
        private readonly Command<Mensaje> mostrarAccionesCommand;
        public Command<Mensaje> MostrarAccionesCommand =>
        mostrarAccionesCommand;

        public string IdProducto
        {
            get => idProducto;
            set => idProducto = value;
        }

        public string IdUsuarioVendedor
        {
            get => idUsuarioVendedor;
            set => idUsuarioVendedor = value;
        }
        private readonly List<FileResult> imagenesSeleccionadas = [];

        private readonly HttpClient clienteSubidaImagen = new HttpClient(
        new SocketsHttpHandler
        {
            Proxy = null,
            UseProxy = false
        })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        private readonly HttpClient clienteConversacion = new HttpClient(
        new SocketsHttpHandler
        {
            Proxy = null,
            UseProxy = false
        })
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public string IdConversacion
        {
            get => idConversacion;
            set
            {
                idConversacion = value;

                if (!omitirCargaAutomatica)
                {
                    _ = CargarConversacionAsync();
                }
            }
        }

        public ChatPage()
        {
            mostrarAccionesCommand =
                new Command<Mensaje>(MostrarAccionesMensaje);

            InitializeComponent();

            ImagenesPreviewCollectionView.ItemsSource =
                imagenesSeleccionadas;
        }

        private async Task CargarConversacionAsync()
        {
            try
            {
                CargandoMensajesLabel.IsVisible = true;

                if (string.IsNullOrWhiteSpace(IdConversacion))
                {
                    MensajesCollectionView.ItemsSource = new List<Mensaje>();
                    CargandoMensajesLabel.IsVisible = false;
                    return;
                }

                clienteConversacion.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
               "Bearer",
                SesionUsuario.Token);

                string url =
                    $"{ApiConfig.BaseUrl}/api/Mensajes/Conversacion/{IdConversacion}";

                var cronometro = System.Diagnostics.Stopwatch.StartNew();
                var response = await clienteConversacion.GetAsync(url);
                cronometro.Stop();

                System.Diagnostics.Debug.WriteLine(
                    $"[CHAT] Respuesta de la API: {cronometro.ElapsedMilliseconds} ms");

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync(
                        "Error",
                        $"No se pudieron obtener los mensajes.\nCódigo: {(int)response.StatusCode}",
                        "OK");

                    return;
                }

                string json = await response.Content.ReadAsStringAsync();

                var mensajes = JsonSerializer.Deserialize<List<Mensaje>>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                System.Diagnostics.Debug.WriteLine(
    $"[CHAT] Mensajes recibidos: {mensajes?.Count ?? 0}");

                if (mensajes == null)
                {
                    return;
                }

                int idUsuarioActual =
                    SesionUsuario.UsuarioActual?.IdUsuario ?? 0;

                foreach (var mensaje in mensajes)
                {
                    mensaje.EsMio =
                        mensaje.IdUsuario == idUsuarioActual;

                }

                var idsPendientes = mensajes
                    .Where(m => !m.EsMio && !m.Visto)
                    .Select(m => m.IdMensaje)
                    .ToList();

                if (idsPendientes.Count > 0)
                {
                    var idsPendientesSet = idsPendientes.ToHashSet();

                    foreach (var mensaje in mensajes)
                    {
                        if (idsPendientesSet.Contains(mensaje.IdMensaje))
                        {
                            mensaje.Visto = true;
                        }
                    }
                }
                var cronometroInterfaz = System.Diagnostics.Stopwatch.StartNew();
                mensajesActuales = new ObservableCollection<Mensaje>(mensajes);

                MensajesCollectionView.ItemsSource = mensajesActuales;
                CargandoMensajesLabel.IsVisible = false;
                cronometroInterfaz.Stop();

                System.Diagnostics.Debug.WriteLine(
                    $"[CHAT] Preparación y asignación de mensajes: " +
                    $"{cronometroInterfaz.ElapsedMilliseconds} ms");
                if (idsPendientes.Count > 0)
                {
                    _ = MarcarVariosComoLeidosAsync(idsPendientes);
                }

                if (EsCargaInicial && mensajes.Count > 0)
                {
                    EsCargaInicial = false;

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Task.Delay(100);

                        MensajesCollectionView.ScrollTo(
                            mensajes[^1],
                            position: ScrollToPosition.End,
                            animate: false);
                    });
                }

            }
            catch (Exception ex)
            {
                CargandoMensajesLabel.IsVisible = false;

                await DisplayAlertAsync(
                    "Error de conexión",
                    ex.Message,
                    "OK");
            }
        }
        private void DesplazarAlUltimoMensaje()
        {
            if (MensajesCollectionView.ItemsSource is IList<Mensaje> mensajes &&
            mensajes.Count > 0)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MensajesCollectionView.ScrollTo(
                        mensajes[^1],
                        position: ScrollToPosition.End,
                        animate: false);
                });
            }
        }
        private void OnMensajesScrolled(object sender, ItemsViewScrolledEventArgs e)
        {
            int totalMensajes = mensajesActuales.Count;

            if (totalMensajes > 0)
            {
                EstaAlFinal = e.LastVisibleItemIndex >= totalMensajes - 1;
            }
            else
            {
                EstaAlFinal = true;
            }

            NuevosMensajesButton.IsVisible = !EstaAlFinal;
        }

        private void OnNuevosMensajesClicked(object sender, EventArgs e)
        {
            if (MensajesCollectionView.ItemsSource is List<Mensaje> mensajes &&
                mensajes.Count > 0)
            {
                MensajesCollectionView.ScrollTo(
                    mensajes[^1],
                    position: ScrollToPosition.End,
                    animate: true);
            }
        }

        private async Task MarcarMensajeComoVistoAsync(int idMensaje)
        {
            try
            {
                if (!SesionUsuario.EstaLogueado)
                {
                    return;
                }

                using var handler = new SocketsHttpHandler
                {
                    Proxy = null,
                    UseProxy = false
                };

                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                client.DefaultRequestHeaders.ConnectionClose = true;

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        SesionUsuario.Token);

                string url =
                    $"{ApiConfig.BaseUrl}/api/Mensajes/{idMensaje}/Leer";

                var response = await client.PutAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"No se pudo marcar el mensaje {idMensaje} como visto. " +
                        $"Código HTTP: {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error al marcar el mensaje como visto: {ex.Message}");
            }
        }

        private async Task MarcarVariosComoLeidosAsync(
            List<int> idsMensajes)
        {
            try
            {
                if (!SesionUsuario.EstaLogueado ||
                    idsMensajes == null ||
                    idsMensajes.Count == 0)
                {
                    return;
                }

                using var handler = new SocketsHttpHandler
                {
                    Proxy = null,
                    UseProxy = false
                };

                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                client.DefaultRequestHeaders.ConnectionClose = true;

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        SesionUsuario.Token);

                string json =
                    JsonSerializer.Serialize(idsMensajes);

                using var contenido = new StringContent(
                    json,
                    System.Text.Encoding.UTF8,
                    "application/json");

                string url =
                    $"{ApiConfig.BaseUrl}/api/Mensajes/Leer";

                var response = await client.PutAsync(url, contenido);

                if (!response.IsSuccessStatusCode)
                {
                    string error =
                        await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine(
                        $"No se pudieron marcar los mensajes como leídos. " +
                        $"HTTP {(int)response.StatusCode}: {error}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error al marcar varios mensajes como leídos: {ex.Message}");
            }
        }

        private async void OnSeleccionarImagenClicked(object sender, EventArgs e)
        {
            try
            {
                var resultados = await MediaPicker.PickPhotosAsync(
                new MediaPickerOptions
                {
                   Title = "Selecciona imágenes",
                   SelectionLimit = 5
                });
                if (resultados == null)
                {
                    return;
                }

                var nuevasImagenes = resultados.ToList();

                if (nuevasImagenes.Count == 0)
                {
                    return;
                }

                if (imagenesSeleccionadas.Count + nuevasImagenes.Count > 5)
                {
                    int disponibles = 5 - imagenesSeleccionadas.Count;

                    if (disponibles <= 0)
                    {
                        await DisplayAlertAsync(
                            "Límite de imágenes",
                            "Un mensaje puede tener máximo 5 imágenes.",
                            "OK");

                        return;
                    }

                    nuevasImagenes = nuevasImagenes
                        .Take(disponibles)
                        .ToList();

                    await DisplayAlertAsync(
                        "Límite de imágenes",
                        "Solo puedes seleccionar hasta 5 imágenes por mensaje.",
                        "OK");
                }

                foreach (var imagen in nuevasImagenes)
                {
                    if (imagen != null &&
                        !imagenesSeleccionadas.Contains(imagen))
                    {
                        imagenesSeleccionadas.Add(imagen);
                    }
                }

                ImagenesPreviewCollectionView.ItemsSource = null;
                ImagenesPreviewCollectionView.ItemsSource = imagenesSeleccionadas;

                ImagenPreviewBorder.IsVisible =
                    imagenesSeleccionadas.Count > 0;

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
                imagenesSeleccionadas.Remove(imagen);

                ImagenesPreviewCollectionView.ItemsSource = null;
                ImagenesPreviewCollectionView.ItemsSource =
                    imagenesSeleccionadas;

                ImagenPreviewBorder.IsVisible =
                    imagenesSeleccionadas.Count > 0;
            }
        }
        private async Task<string?> SubirImagenAsync(FileResult imagen)
        {
            try
            {
                using var handler = new SocketsHttpHandler
                {
                    Proxy = null,
                    UseProxy = false
                };

                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };

                client.DefaultRequestHeaders.ConnectionClose = true;

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        SesionUsuario.Token);


                using var stream = await imagen.OpenReadAsync();

                using var contenido = new MultipartFormDataContent();

                using var archivoContenido = new StreamContent(stream);

                archivoContenido.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(
                        "application/octet-stream");

                contenido.Add(
                    archivoContenido,
                    "archivo",
                    imagen.FileName);

                string url =
                    $"{ApiConfig.BaseUrl}/api/ImagenesProducto/Subir";

                var response = await client.PostAsync(
                    url,
                    contenido);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string json =
                    await response.Content.ReadAsStringAsync();

                using var documento =
                    JsonDocument.Parse(json);

                if (!documento.RootElement.TryGetProperty(
                    "urlImagen",
                    out var propiedadUrl))
                {
                    return null;
                }

                return propiedadUrl.GetString();
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> SubirImagenMensajeAsync(FileResult imagen)
        {
            try
            {
                clienteSubidaImagen.DefaultRequestHeaders.Authorization =
                      new System.Net.Http.Headers.AuthenticationHeaderValue(
                     "Bearer",
                      SesionUsuario.Token);

                using var stream = await imagen.OpenReadAsync();

                using var contenido = new MultipartFormDataContent();

                using var archivoContenido =
                    new StreamContent(stream);


                string extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();

                string tipoContenido = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    _ => "application/octet-stream"
                };

                archivoContenido.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(tipoContenido);

                contenido.Add(
                    archivoContenido,
                    "archivo",
                    imagen.FileName);

                string url =
                    $"{ApiConfig.BaseUrl}/api/ImagenesMensaje/Subir?idConversacion={IdConversacion}";

                var response = await clienteSubidaImagen.PostAsync(url, contenido);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await DisplayAlertAsync(
                            "Error al subir imagen",
                            $"Código HTTP: {(int)response.StatusCode}\n\n{error}",
                            "OK");
                    });

                    return null;
                }

                string json =
                    await response.Content.ReadAsStringAsync();

                using var documento =
                    JsonDocument.Parse(json);

                if (!documento.RootElement.TryGetProperty(
                    "urlImagen",
                    out var propiedadUrl))
                {
                    return null;
                }

                return propiedadUrl.GetString();
            }
            catch
            {
                return null;
            }
        }
        private async void OnEnviarMensajeClicked(object sender, EventArgs e)
        {
            try
            {
                EnviarMensajeButton.IsEnabled = false;

                if (!SesionUsuario.EstaLogueado)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "Debes iniciar sesión para enviar mensajes.",
                        "OK");

                    return;
                }

                if (string.IsNullOrWhiteSpace(MensajeEntry.Text) &&
                    imagenesSeleccionadas.Count == 0)
                {
                    return;
                }

                bool teniaImagenes = imagenesSeleccionadas.Count > 0;

                using var handler = new SocketsHttpHandler
                {
                    Proxy = null,
                    UseProxy = false
                };

                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        SesionUsuario.Token);

                if (string.IsNullOrWhiteSpace(IdConversacion))
                {
                    if (!int.TryParse(IdProducto, out int idProducto) ||
                        !int.TryParse(IdUsuarioVendedor, out int idUsuarioVendedor))
                    {
                        await DisplayAlertAsync(
                            "Error",
                            "No se pudo identificar el producto o el vendedor.",
                            "OK");

                        return;
                    }

                    var datosConversacion = new
                    {
                        IdProducto = idProducto
                    };

                    string jsonConversacion =
                        JsonSerializer.Serialize(datosConversacion);

                    using var contenidoConversacion = new StringContent(
                        jsonConversacion,
                        System.Text.Encoding.UTF8,
                        "application/json");

                    string urlConversacion =
                        $"{ApiConfig.BaseUrl}/api/Conversaciones";

                    var respuestaConversacion = await client.PostAsync(
                        urlConversacion,
                        contenidoConversacion);

                    if (!respuestaConversacion.IsSuccessStatusCode)
                    {
                        string errorConversacion =
                            await respuestaConversacion.Content.ReadAsStringAsync();

                        await DisplayAlertAsync(
                            "Error",
                            $"No se pudo crear la conversación.\n\n{errorConversacion}",
                            "OK");

                        return;
                    }

                    string jsonRespuestaConversacion =
                        await respuestaConversacion.Content.ReadAsStringAsync();

                    var conversacionCreada =
                        JsonSerializer.Deserialize<Conversacion>(
                            jsonRespuestaConversacion,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                    if (conversacionCreada == null)
                    {
                        await DisplayAlertAsync(
                            "Error",
                            "No se pudo obtener la información de la conversación.",
                            "OK");

                        return;
                    }

                    omitirCargaAutomatica = true;
                    IdConversacion = conversacionCreada.IdConversacion.ToString();
                    omitirCargaAutomatica = false;
                }

                if (!int.TryParse(IdConversacion, out int idConversacion))
                {
                    await DisplayAlertAsync(
                        "Error",
                        "El identificador de la conversación no es válido.",
                        "OK");

                    return;
                }

                var nuevoMensaje = new
                {
                    IdConversacion = idConversacion,
                    IdUsuario = SesionUsuario.UsuarioActual!.IdUsuario,
                    Contenido = string.IsNullOrWhiteSpace(MensajeEntry.Text)
                        ? null
                        : MensajeEntry.Text.Trim(),
                    UrlImagen = (string?)null,
                    TieneImagenes = imagenesSeleccionadas.Count > 0
                };

                string json = JsonSerializer.Serialize(nuevoMensaje);

                using var content = new StringContent(
                    json,
                    System.Text.Encoding.UTF8,
                    "application/json");

                string url = $"{ApiConfig.BaseUrl}/api/Mensajes";

                var response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string mensaje = await response.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error",
                        $"No se pudo enviar el mensaje.\n\n{mensaje}",
                        "OK");

                    return;
                }

                string respuestaJson = await response.Content.ReadAsStringAsync();

                using var documento = JsonDocument.Parse(respuestaJson);

                int idMensaje =
                    documento.RootElement.GetProperty("idMensaje").GetInt32();

                foreach (var imagen in imagenesSeleccionadas)
                {
                    string? urlImagen = await SubirImagenMensajeAsync(imagen);

                    if (string.IsNullOrWhiteSpace(urlImagen))
                    {
                        await DisplayAlertAsync(
                            "Error",
                            $"No se pudo subir la imagen: {imagen.FileName}",
                            "OK");

                        return;
                    }

                    var nuevaImagen = new
                    {
                        IdMensaje = idMensaje,
                        UrlImagen = urlImagen
                    };

                    string jsonImagen = JsonSerializer.Serialize(nuevaImagen);

                    using var contenidoImagen = new StringContent(
                        jsonImagen,
                        System.Text.Encoding.UTF8,
                        "application/json");

                    string urlRegistro =
                        $"{ApiConfig.BaseUrl}/api/ImagenesMensaje";

                    var responseImagen = await client.PostAsync(
                        urlRegistro,
                        contenidoImagen);

                    if (!responseImagen.IsSuccessStatusCode)
                    {
                        string mensajeError =
                            await responseImagen.Content.ReadAsStringAsync();

                        await DisplayAlertAsync(
                            "Error al registrar imagen",
                            $"Código HTTP: {(int)responseImagen.StatusCode}\n\n{mensajeError}",
                            "OK");

                        return;
                    }
                }

                MensajeEntry.Text = string.Empty;
                imagenesSeleccionadas.Clear();

                ImagenesPreviewCollectionView.ItemsSource = null;
                ImagenesPreviewCollectionView.ItemsSource = imagenesSeleccionadas;

                ImagenPreviewBorder.IsVisible = false;

                if (!teniaImagenes)
                {
                    var mensajeNuevo =
                        JsonSerializer.Deserialize<MarketplaceUAT.App.Models.Mensaje>(
                            respuestaJson,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                    if (mensajeNuevo != null)
                    {
                        mensajeNuevo.EsMio = true;
                        mensajesActuales.Add(mensajeNuevo);
                        MensajesCollectionView.ItemsSource = mensajesActuales;
                    }
                    else
                    {
                        await CargarConversacionAsync();
                    }
                }
                else
                {
                    await CargarConversacionAsync();
                }

                DesplazarAlUltimoMensaje();

            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error de conexión",
                    $"No se pudo enviar el mensaje.\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                EnviarMensajeButton.IsEnabled = true;
            }
        }

        private async void OnEliminarMensajeClicked(
            object sender,
            EventArgs e)
        {
            if (sender is not Button boton ||
                boton.CommandParameter is not Mensaje mensaje)
            {
                return;
            }

            bool confirmar = await DisplayAlertAsync(
                "Eliminar mensaje",
                "¿Quieres eliminar este mensaje?",
                "Sí",
                "No");

            if (!confirmar)
            {
                return;
            }

            try
            {
                if (!SesionUsuario.EstaLogueado)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "Debes iniciar sesión.",
                        "OK");

                    return;
                }

                int idUsuario =
                    SesionUsuario.UsuarioActual!.IdUsuario;

                using var handler = new SocketsHttpHandler
                {
                    Proxy = null,
                    UseProxy = false
                };

                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                client.DefaultRequestHeaders.ConnectionClose = true;

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        SesionUsuario.Token);

                string url =
                    $"{ApiConfig.BaseUrl}/api/Mensajes/{mensaje.IdMensaje}?idUsuario={idUsuario}";

                var response = await client.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    string error =
                        await response.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error",
                        $"No se pudo eliminar el mensaje.\n\n{error}",
                        "OK");

                    return;
                }

                if (MensajesCollectionView.ItemsSource
                    is ObservableCollection<Mensaje> mensajes)
                {
                    mensajes.Remove(mensaje);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudo eliminar el mensaje.\n\n{ex.Message}",
                    "OK");
            }
        }
        private async void OnEditarMensajeClicked(
            object sender,
            EventArgs e)
        {
            if (sender is not Button boton ||
                boton.CommandParameter is not Mensaje mensaje)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(mensaje.Contenido))
            {
                return;
            }

            string? nuevoContenido = await DisplayPromptAsync(
                "Editar mensaje",
                "Modifica el texto de tu mensaje:",
                accept: "Guardar",
                cancel: "Cancelar",
                initialValue: mensaje.Contenido);

            if (nuevoContenido == null)
            {
                return;
            }

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
                if (!SesionUsuario.EstaLogueado)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "Debes iniciar sesión.",
                        "OK");

                    return;
                }

                using var handler = new SocketsHttpHandler
                {
                    Proxy = null,
                    UseProxy = false
                };

                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                client.DefaultRequestHeaders.ConnectionClose = true;

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        SesionUsuario.Token);

                var datos = new
                {
                    Contenido = nuevoContenido
                };

                string json =
                    System.Text.Json.JsonSerializer.Serialize(datos);

                using var contenido = new StringContent(
                    json,
                    System.Text.Encoding.UTF8,
                    "application/json");

                string url =
                    $"{ApiConfig.BaseUrl}/api/Mensajes/{mensaje.IdMensaje}";

                var response = await client.PutAsync(url, contenido);

                if (!response.IsSuccessStatusCode)
                {
                    string error =
                        await response.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error",
                        $"No se pudo editar el mensaje.\n\n{error}",
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
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            imagenesSeleccionadas.Clear();

            if (ImagenesPreviewCollectionView != null)
            {
                ImagenesPreviewCollectionView.ItemsSource = null;
                ImagenesPreviewCollectionView.ItemsSource =
                    imagenesSeleccionadas;
            }

            if (ImagenPreviewBorder != null)
            {
                ImagenPreviewBorder.IsVisible = false;
            }
        }
        private void MostrarAccionesMensaje(Mensaje? mensaje)
        {
            if (mensaje == null)
                return;

            if (MensajesCollectionView.ItemsSource
                is IEnumerable<Mensaje> mensajes)
            {
                bool ocultarActual =
                    mensaje.MostrarAcciones;

                foreach (var item in mensajes)
                {
                    item.MostrarAcciones = false;
                }

                if (!ocultarActual)
                {
                    mensaje.MostrarAcciones = true;
                    mensajeSeleccionadoId = mensaje.IdMensaje;
                }
                else
                {
                    mensajeSeleccionadoId = null;
                }
            }
        }
    }
}
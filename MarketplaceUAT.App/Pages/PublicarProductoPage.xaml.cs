using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using MarketplaceUAT.App.Services;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MarketplaceUAT.App.Pages
{
    public partial class PublicarProductoPage : ContentPage, IQueryAttributable
    {
        private ApiClient Api =>
        Application.Current!.Handler.MauiContext!.Services
        .GetRequiredService<ApiClient>();
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private class ImagenPublicacion
        {
            public FileResult? ArchivoNuevo { get; set; }

            public string? UrlExistente { get; set; }

            public string NombreArchivo { get; set; } = string.Empty;

            public bool EsPrincipal { get; set; }

            public bool EsNueva => ArchivoNuevo != null;
        }

        private List<ImagenPublicacion> ImagenesFormulario = [];

        private List<Categoria> Categorias = [];

        private int? IdProductoOrigen;

        private bool EsVolverAPublicar => IdProductoOrigen.HasValue;

        private bool _datosOrigenCargados;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            System.Diagnostics.Debug.WriteLine(
                "[VolverPublicar] ApplyQueryAttributes: iniciado");

            if (query.TryGetValue("IdProductoOrigen", out object? valor))
            {
                if (valor is int id && id > 0)
                {
                    IdProductoOrigen = id;
                }
                else if (int.TryParse(valor?.ToString(), out int idConvertido)
                         && idConvertido > 0)
                {
                    IdProductoOrigen = idConvertido;
                }
            }

            System.Diagnostics.Debug.WriteLine(
                $"[VolverPublicar] ID recibido: {IdProductoOrigen}");
        }

        public PublicarProductoPage()
        {
            System.Diagnostics.Debug.WriteLine(
        "[VolverPublicar] Constructor: iniciado");

            InitializeComponent();

            System.Diagnostics.Debug.WriteLine(
        "[VolverPublicar] Constructor: InitializeComponent completado");

            NombreEntry.Keyboard = Keyboard.Create(KeyboardFlags.CapitalizeSentence);
            DescripcionEditor.Keyboard = Keyboard.Create(KeyboardFlags.CapitalizeSentence);
            DescripcionEditor.TextChanged += OnDescripcionTextChanged;

        }

        private void OnDescripcionTextChanged(object? sender, TextChangedEventArgs e)
        {
            int caracteres = e.NewTextValue?.Length ?? 0;

            DescripcionContadorLabel.Text =
                $"{caracteres}/500";
        }

        private void OnTipoProductoChanged(object? sender, EventArgs e)
        {
            bool esProductoUnico = TipoProductoPicker.SelectedIndex == 0;

            CantidadDisponibleLabel.IsVisible = !esProductoUnico;
            CantidadDisponibleEntry.IsVisible = !esProductoUnico;

            if (esProductoUnico)
            {
                CantidadDisponibleEntry.Text = "1";
            }
            else if (string.IsNullOrWhiteSpace(CantidadDisponibleEntry.Text) ||
                     CantidadDisponibleEntry.Text == "1")
            {
                CantidadDisponibleEntry.Text = string.Empty;
            }
        }

        private async void OnSeleccionarImagenClicked(object sender, EventArgs e)
        {
            try
            {
                int disponibles = 5 - ImagenesFormulario.Count;

                if (disponibles <= 0)
                {
                    await DisplayAlertAsync(
                        "Límite de imágenes",
                        "Puedes tener un máximo de 5 imágenes por producto. Elimina alguna antes de agregar otra.",
                        "OK");

                    return;
                }

                var fotos = await MediaPicker.Default.PickPhotosAsync(
                    new MediaPickerOptions
                    {
                        SelectionLimit = disponibles
                    });

                if (fotos == null || fotos.Count == 0)
                    return;

                foreach (var foto in fotos.Take(disponibles))
                {
                    ImagenesFormulario.Add(new ImagenPublicacion
                    {
                        ArchivoNuevo = foto,
                        NombreArchivo = foto.FileName,
                        EsPrincipal = ImagenesFormulario.Count == 0
                    });
                }

                if (!ImagenesFormulario.Any(i => i.EsPrincipal))
                {
                    ImagenesFormulario[0].EsPrincipal = true;
                }

                ActualizarMiniaturas();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudieron seleccionar las imágenes.\n\n{ex.Message}",
                    "OK");
            }
        }

        private void ActualizarMiniaturas()
        {
            MiniaturasLayout.Children.Clear();

            ImagenPublicacion? principal =
                ImagenesFormulario.FirstOrDefault(i => i.EsPrincipal);

            if (principal != null)
            {
                ImagenPreview.Source = ObtenerFuenteImagen(principal);
                ImagenPreview.IsVisible = true;
            }
            else
            {
                ImagenPreview.Source = null;
                ImagenPreview.IsVisible = false;
            }

            foreach (ImagenPublicacion imagen in ImagenesFormulario)
            {
                var miniatura = new Image
                {
                    Source = ObtenerFuenteImagen(imagen),
                    WidthRequest = 60,
                    HeightRequest = 60,
                    Aspect = Aspect.AspectFill
                };

                var tapGesture = new TapGestureRecognizer();

                tapGesture.Tapped += (s, e) =>
                {
                    foreach (var item in ImagenesFormulario)
                        item.EsPrincipal = item == imagen;

                    ActualizarMiniaturas();
                };

                miniatura.GestureRecognizers.Add(tapGesture);

                var eliminarButton = new Button
                {
                    Text = "×",
                    FontSize = 14,
                    WidthRequest = 30,
                    HeightRequest = 30,
                    Padding = 0,
                    BackgroundColor = Colors.Black,
                    TextColor = Colors.White,
                    CornerRadius = 15,
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Start
                };

                eliminarButton.Clicked += (s, e) =>
                {
                    bool eraPrincipal = imagen.EsPrincipal;

                    ImagenesFormulario.Remove(imagen);

                    if (eraPrincipal && ImagenesFormulario.Count > 0)
                        ImagenesFormulario[0].EsPrincipal = true;

                    ActualizarMiniaturas();
                };

                var contenedor = new Grid
                {
                    WidthRequest = 70,
                    HeightRequest = 90
                };

                var etiquetaPrincipal = new Label
                {
                    Text = "★",
                    FontSize = 28,
                    TextColor = Colors.Gold,
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Start,
                    Padding = new Thickness(3, 0),
                    IsVisible = imagen.EsPrincipal
                };

                contenedor.Children.Add(miniatura);
                contenedor.Children.Add(etiquetaPrincipal);
                contenedor.Children.Add(eliminarButton);

                MiniaturasLayout.Children.Add(contenedor);
            }

            MiniaturasLayout.IsVisible = ImagenesFormulario.Count > 0;
        }

        private ImageSource ObtenerFuenteImagen(ImagenPublicacion imagen)
        {
            if (imagen.ArchivoNuevo != null)
            {
                return ImageSource.FromFile(imagen.ArchivoNuevo.FullPath);
            }

            if (!string.IsNullOrWhiteSpace(imagen.UrlExistente))
            {
                return ImageSource.FromUri(
                    new Uri($"{ApiConfig.BaseUrl}/Images/{imagen.UrlExistente}"));
            }

            return ImageSource.FromFile("placeholder.png");
        }

        protected override async void OnAppearing()
        {
            System.Diagnostics.Debug.WriteLine(
                "[VolverPublicar] OnAppearing: antes de base");

            base.OnAppearing();

            System.Diagnostics.Debug.WriteLine(
                "[VolverPublicar] OnAppearing: después de base");

            try
            {
                System.Diagnostics.Debug.WriteLine(
                    "[VolverPublicar] Antes de CargarCategorias");

                await CargarCategorias();

                System.Diagnostics.Debug.WriteLine(
                    $"[VolverPublicar] Categorías cargadas: {Categorias.Count}");

                System.Diagnostics.Debug.WriteLine(
                    $"[VolverPublicar] IdProductoOrigen: {IdProductoOrigen}");

                if (EsVolverAPublicar && !_datosOrigenCargados)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "[VolverPublicar] Antes de CargarProductoOrigen");

                    await CargarProductoOrigen();

                    System.Diagnostics.Debug.WriteLine(
                        "[VolverPublicar] Después de CargarProductoOrigen");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[VolverPublicar] ERROR EN OnAppearing: {ex}");

                await DisplayAlertAsync(
                    "Error al abrir el formulario",
                    ex.Message,
                    "OK");
            }
        }

        private async Task CargarCategorias()
        {
            try
            {
                var client = Api.Client;

                string url =
                    $"{ApiConfig.BaseUrl}/api/Categorias";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "No se pudieron cargar las categorías.",
                        "OK");

                    return;
                }

                string json =
                    await response.Content.ReadAsStringAsync();

                var categorias =
                    JsonSerializer.Deserialize<List<Categoria>>(
                    json,
                    JsonOptions);

                if (categorias == null)
                {
                    return;
                }

                Categorias = categorias
                    .Where(c => c.Activo)
                    .ToList();

                CategoriaPicker.ItemsSource =
                Categorias.Select(c => c.Nombre).ToList();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error de conexión",
                    ex.Message,
                    "OK");
            }
        }

        private async Task CargarProductoOrigen()
        {
            if (!IdProductoOrigen.HasValue)
                return;

            System.Diagnostics.Debug.WriteLine(
                "[VolverPublicar] Inicio de CargarProductoOrigen");

            try
            {
                var client = Api.Client;

                string urlProducto =
                    $"{ApiConfig.BaseUrl}/api/Productos/{IdProductoOrigen.Value}";

                System.Diagnostics.Debug.WriteLine(
                    $"[VolverPublicar] Solicitando producto: {urlProducto}");

                var response = await client.GetAsync(urlProducto);

                System.Diagnostics.Debug.WriteLine(
                    $"[VolverPublicar] Respuesta del producto: {(int)response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine(
                        $"[VolverPublicar] Error del producto: {error}");

                    await DisplayAlertAsync(
                        "Error",
                        $"No se pudo cargar la publicación original. " +
                        $"Código HTTP: {(int)response.StatusCode}",
                        "OK");

                    return;
                }

                string json = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(
                    "[VolverPublicar] JSON del producto recibido");

                var producto = JsonSerializer.Deserialize<Producto>(
                    json,
                    JsonOptions);

                if (producto == null)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "No se pudo interpretar la información del producto.",
                        "OK");

                    return;
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[VolverPublicar] Producto deserializado. ID: {producto.IdProducto}");

                if (producto.IdUsuario != SesionUsuario.UsuarioActual?.IdUsuario ||
                    producto.Estado != "Vendido" ||
                    !producto.Activo)
                {
                    await DisplayAlertAsync(
                        "Acción no permitida",
                        "Solo puedes volver a publicar tus propios productos vendidos.",
                        "OK");

                    return;
                }

                NombreEntry.Text = producto.Nombre;
                DescripcionEditor.Text = producto.Descripcion;

                PrecioEntry.Text = producto.Precio.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);

                int indiceCondicion = CondicionPicker.Items.IndexOf(
                    producto.Condicion);

                CondicionPicker.SelectedIndex = indiceCondicion;

                int indiceCategoria = Categorias.FindIndex(
                    c => c.IdCategoria == producto.IdCategoria);

                CategoriaPicker.SelectedIndex = indiceCategoria;

                DescripcionContadorLabel.Text =
                    $"{DescripcionEditor.Text?.Length ?? 0}/500";

                System.Diagnostics.Debug.WriteLine(
                    "[VolverPublicar] Campos del formulario completados");

                string urlImagenes =
                    $"{ApiConfig.BaseUrl}/api/ImagenesProducto/Producto/{producto.IdProducto}";

                System.Diagnostics.Debug.WriteLine(
                    $"[VolverPublicar] Solicitando imágenes: {urlImagenes}");

                var imagenesResponse = await client.GetAsync(urlImagenes);

                System.Diagnostics.Debug.WriteLine(
                    $"[VolverPublicar] Respuesta de imágenes: {(int)imagenesResponse.StatusCode}");

                if (imagenesResponse.IsSuccessStatusCode)
                {
                    string imagenesJson =
                        await imagenesResponse.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine(
                        $"[VolverPublicar] JSON de imágenes recibido. Caracteres: {imagenesJson.Length}");

                    var imagenes =
                        JsonSerializer.Deserialize<List<ImagenProducto>>(
                            imagenesJson,
                            JsonOptions);

                    if (imagenes != null)
                    {
                        ImagenesFormulario.Clear();

                        foreach (var imagen in imagenes
                            .Where(i => i.Activo)
                            .Take(5))
                        {
                            ImagenesFormulario.Add(new ImagenPublicacion
                            {
                                UrlExistente = imagen.UrlImagen,
                                NombreArchivo = Path.GetFileName(imagen.UrlImagen),
                                EsPrincipal = imagen.EsPrincipal
                            });
                        }

                        if (ImagenesFormulario.Count > 0 &&
                            !ImagenesFormulario.Any(i => i.EsPrincipal))
                        {
                            ImagenesFormulario[0].EsPrincipal = true;
                        }

                        System.Diagnostics.Debug.WriteLine(
                            $"[VolverPublicar] Imágenes cargadas: {ImagenesFormulario.Count}");

                        System.Diagnostics.Debug.WriteLine(
                            "[VolverPublicar] Actualizando miniaturas");

                        ActualizarMiniaturas();

                        System.Diagnostics.Debug.WriteLine(
                            "[VolverPublicar] Miniaturas actualizadas");
                    }
                }
                else
                {
                    string errorImagenes =
                        await imagenesResponse.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine(
                        $"[VolverPublicar] Error al consultar imágenes: {errorImagenes}");
                }

                _datosOrigenCargados = true;

                Title = "Volver a publicar";
                PublicarButton.Text = "Volver a publicar";

                System.Diagnostics.Debug.WriteLine(
                    "[VolverPublicar] Carga finalizada correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[VolverPublicar] EXCEPCIÓN: {ex}");

                await DisplayAlertAsync(
                    "Error de conexión",
                    $"No se pudo cargar la publicación original.\n{ex.Message}",
                    "OK");
            }
        }

        private async void OnPublicarClicked(object sender, EventArgs e)
        {
            if (!PublicarButton.IsEnabled)
                return;

            PublicarButton.IsEnabled = false;

            try
            {
                if (!SesionUsuario.EstaLogueado ||
                    SesionUsuario.UsuarioActual == null ||
                    string.IsNullOrWhiteSpace(SesionUsuario.Token))
                {
                    await DisplayAlertAsync(
                        "Iniciar sesión",
                        "Debes iniciar sesión para publicar un producto.",
                        "OK");
                    return;
                }

                string nombre = NombreEntry.Text?.Trim() ?? string.Empty;
                string descripcion = DescripcionEditor.Text?.Trim() ?? string.Empty;
                string precioTexto = PrecioEntry.Text?.Trim() ?? string.Empty;

                if (nombre.Length < 3 || nombre.Length > 100)
                {
                    await DisplayAlertAsync(
                        "Nombre inválido",
                        "El nombre debe tener entre 3 y 100 caracteres.",
                        "OK");
                    return;
                }

                if (descripcion.Length > 500)
                {
                    await DisplayAlertAsync(
                        "Descripción inválida",
                        "La descripción no puede superar los 500 caracteres.",
                        "OK");
                    return;
                }

                if (CondicionPicker.SelectedIndex < 0 ||
                    CategoriaPicker.SelectedIndex < 0 ||
                    string.IsNullOrWhiteSpace(precioTexto))
                {
                    await DisplayAlertAsync(
                        "Datos incompletos",
                        "Completa todos los campos para publicar el producto.",
                        "OK");
                    return;
                }

                if (!decimal.TryParse(
                    precioTexto.Replace(',', '.'),
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal precio) || precio <= 0)
                {
                    await DisplayAlertAsync(
                        "Precio inválido",
                        "Escribe un precio válido mayor que cero. Ejemplo: 350 o 350.50.",
                        "OK");
                    return;
                }

                bool esProductoUnico = TipoProductoPicker.SelectedIndex == 0;

                int cantidadDisponible = 1;

                if (!esProductoUnico)
                {
                    if (!int.TryParse(
                        CantidadDisponibleEntry.Text,
                        out cantidadDisponible) ||
                        cantidadDisponible < 1)
                    {
                        await DisplayAlertAsync(
                            "Cantidad inválida",
                            "Ingresa una cantidad de al menos una unidad.",
                            "OK");
                        return;
                    }
                }

                if (ImagenesFormulario.Count > 5)
                {
                    await DisplayAlertAsync(
                        "Límite de imágenes",
                        "Puedes publicar un máximo de 5 imágenes.",
                        "OK");
                    return;
                }

                if (ImagenesFormulario.Count > 0 &&
                    !ImagenesFormulario.Any(i => i.EsPrincipal))
                {
                    ImagenesFormulario[0].EsPrincipal = true;
                }

                Categoria categoriaSeleccionada =
                    Categorias[CategoriaPicker.SelectedIndex];

                var client = Api.Client;

                var producto = new
                {
                    IdCategoria = categoriaSeleccionada.IdCategoria,
                    EsProductoUnico = esProductoUnico,
                    CantidadDisponible = cantidadDisponible,
                    Nombre = nombre,
                    Descripcion = descripcion,
                    Precio = precio,
                    Condicion = CondicionPicker.SelectedItem?.ToString() ?? string.Empty
                };

                string urlProducto = $"{ApiConfig.BaseUrl}/api/Productos";

                string json = JsonSerializer.Serialize(producto);

                using var response = await Api.EnviarAutenticadoAsync(
    () => new HttpRequestMessage(HttpMethod.Post, urlProducto)
    {
        Content = new StringContent(
            json,
            System.Text.Encoding.UTF8,
            "application/json")
    });

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "No se pudo publicar",
                        error,
                        "OK");

                    return;
                }

                string respuestaJson = await response.Content.ReadAsStringAsync();

                var productoCreado = JsonSerializer.Deserialize<Producto>(
                    respuestaJson,
                    JsonOptions);

                if (productoCreado == null || productoCreado.IdProducto <= 0)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "El producto pudo haberse creado, pero no se obtuvo su identificador.",
                        "OK");
                    return;
                }

                int idProductoNuevo = productoCreado.IdProducto;
                int imagenesRegistradas = 0;

                foreach (var imagen in ImagenesFormulario
                    .OrderByDescending(i => i.EsPrincipal))
                {
                    await SubirYRegistrarImagenAsync(
                        client,
                        idProductoNuevo,
                        imagen);

                    imagenesRegistradas++;
                }

                string mensaje = EsVolverAPublicar
                    ? $"Se creó una nueva publicación correctamente.\n\nNuevo ID: " +
                    $"{idProductoNuevo}\nImágenes: {imagenesRegistradas}\n\nLa publicación vendida original se conservó."
                    : $"El producto se publicó correctamente.\n\nID: " +
                    $"{idProductoNuevo}\nImágenes: {imagenesRegistradas}";

                await DisplayAlertAsync(
                    "Publicación completada",
                    mensaje,
                    "OK");

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error al publicar",
                    ex.Message,
                    "OK");
            }
            finally
            {
                PublicarButton.IsEnabled = true;
            }
        }

        private async Task SubirYRegistrarImagenAsync(
            HttpClient client,
            int idProducto,
            ImagenPublicacion imagen)
        {
            Stream archivoStream;
            string nombreArchivo;

            if (imagen.ArchivoNuevo != null)
            {
                archivoStream = await imagen.ArchivoNuevo.OpenReadAsync();
                nombreArchivo = Path.GetFileName(imagen.ArchivoNuevo.FileName);
            }
            else if (!string.IsNullOrWhiteSpace(imagen.UrlExistente))
            {
                string urlImagenOriginal =
                    $"{ApiConfig.BaseUrl}/Images/{imagen.UrlExistente}";

                using var descarga = await client.GetAsync(
                    urlImagenOriginal,
                    HttpCompletionOption.ResponseHeadersRead);

                if (!descarga.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"No se pudo descargar una imagen original. Código HTTP: {(int)descarga.StatusCode}");
                }

                var memoria = new MemoryStream();

                await using (var streamOriginal =
                    await descarga.Content.ReadAsStreamAsync())
                {
                    await streamOriginal.CopyToAsync(memoria);
                }

                if (memoria.Length > 20 * 1024 * 1024)
                {
                    memoria.Dispose();

                    throw new Exception(
                        "Una de las imágenes originales supera el límite de 20 MB.");
                }

                memoria.Position = 0;

                archivoStream = memoria;
                nombreArchivo = Path.GetFileName(imagen.UrlExistente);
            }
            else
            {
                throw new Exception("Una imagen no tiene un archivo válido.");
            }

            using (archivoStream)
            {
                if (archivoStream.CanSeek &&
                    archivoStream.Length > 20 * 1024 * 1024)
                {
                    throw new Exception(
                        $"La imagen {nombreArchivo} supera el límite de 20 MB.");
                }

                string extension = Path.GetExtension(nombreArchivo)
                    .ToLowerInvariant();

                string tipoMime = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    _ => string.Empty
                };

                if (string.IsNullOrEmpty(tipoMime))
                {
                    throw new Exception(
                        $"La imagen {nombreArchivo} no tiene un formato permitido. Usa JPG, JPEG, PNG o WEBP.");
                }

                string urlSubir =
                    $"{ApiConfig.BaseUrl}/api/ImagenesProducto/Subir?idProducto={idProducto}";

                using var respuestaSubida = await Api.EnviarAutenticadoAsync(
    async () =>
    {
        Stream nuevoStream;

        if (imagen.ArchivoNuevo != null)
        {
            nuevoStream = await imagen.ArchivoNuevo.OpenReadAsync();
        }
        else
        {
            nuevoStream = new MemoryStream();

            if (archivoStream.CanSeek)
                archivoStream.Position = 0;

            await archivoStream.CopyToAsync(nuevoStream);
            nuevoStream.Position = 0;
        }

        var multipart = new MultipartFormDataContent();

        var contenidoArchivo = new StreamContent(nuevoStream);

        contenidoArchivo.Headers.ContentType =
            new MediaTypeHeaderValue(tipoMime);

        multipart.Add(
            contenidoArchivo,
            "archivo",
            nombreArchivo);

        return new HttpRequestMessage(HttpMethod.Post, urlSubir)
        {
            Content = multipart
        };
    });

                if (!respuestaSubida.IsSuccessStatusCode)
                {
                    string error = await respuestaSubida.Content.ReadAsStringAsync();

                    throw new Exception(
                        $"No se pudo subir la imagen {nombreArchivo}.\n{error}");
                }

                string jsonImagen =
                    await respuestaSubida.Content.ReadAsStringAsync();

                using var documento = JsonDocument.Parse(jsonImagen);

                string urlGuardada = documento.RootElement
                    .GetProperty("urlImagen")
                    .GetString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(urlGuardada))
                {
                    throw new Exception(
                        $"El servidor no devolvió la URL de {nombreArchivo}.");
                }

                var registro = new
                {
                    IdProducto = idProducto,
                    UrlImagen = urlGuardada,
                    EsPrincipal = imagen.EsPrincipal
                };

                string jsonRegistro = JsonSerializer.Serialize(registro);

                string urlRegistro =
                    $"{ApiConfig.BaseUrl}/api/ImagenesProducto";

                using var respuestaRegistro = await Api.EnviarAutenticadoAsync(
    () => new HttpRequestMessage(HttpMethod.Post, urlRegistro)
    {
        Content = new StringContent(
            jsonRegistro,
            System.Text.Encoding.UTF8,
            "application/json")
    });

                if (!respuestaRegistro.IsSuccessStatusCode)
                {
                    string error = await respuestaRegistro.Content.ReadAsStringAsync();

                    throw new Exception(
                        $"La imagen {nombreArchivo} se subió, pero no se pudo registrar.\n{error}");
                }
            }
        }
    }
}
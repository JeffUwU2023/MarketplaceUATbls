using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MarketplaceUAT.App.Pages
{
    public partial class PublicarProductoPage : ContentPage
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private FileResult? ImagenSeleccionada;
        private List<FileResult> ImagenesSeleccionadas = [];
        private List<Categoria> Categorias = [];

        public PublicarProductoPage()
        {
            InitializeComponent();

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

        private async void OnSeleccionarImagenClicked(object sender, EventArgs e)
        {
            try
            {
                var fotos = await MediaPicker.Default.PickPhotosAsync(
                new MediaPickerOptions
                {
                    SelectionLimit = 5
                });

                if (fotos == null || fotos.Count == 0)
                {
                    return;
                }

                if (fotos.Count > 5)
                {
                    await DisplayAlertAsync(
                        "Límite de imágenes",
                        "Puedes seleccionar un máximo de 5 imágenes por producto.",
                        "OK");

                    return;
                }

                ImagenesSeleccionadas = fotos.ToList();

                ImagenSeleccionada = ImagenesSeleccionadas.First();

                ImagenPreview.Source =
                    ImageSource.FromFile(ImagenSeleccionada.FullPath);

                ImagenPreview.IsVisible = true;

                ActualizarMiniaturas();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudo seleccionar las imágenes.\n\n{ex.Message}",
                    "OK");
            }
        }

        private void ActualizarMiniaturas()
        {
            MiniaturasLayout.Children.Clear();

            foreach (FileResult imagen in ImagenesSeleccionadas)
            {
                var miniatura = new Image
                {
                    Source = ImageSource.FromFile(imagen.FullPath),
                    WidthRequest = 60,
                    HeightRequest = 60,
                    Aspect = Aspect.AspectFill
                };

                var tapGesture = new TapGestureRecognizer();

                tapGesture.Tapped += (s, e) =>
                {
                    ImagenSeleccionada = imagen;

                    ImagenPreview.Source =
                        ImageSource.FromFile(imagen.FullPath);

                    ActualizarMiniaturas();
                };

                miniatura.GestureRecognizers.Add(tapGesture);

                var eliminarButton = new Button
                {
                    Text = "x",
                    FontSize = 14,
                    WidthRequest = 30,
                    HeightRequest = 30,
                    Padding = 0,
                    BackgroundColor = Colors.Black,
                    TextColor = Colors.White,
                    CornerRadius = 15
                };

                eliminarButton.Clicked += (s, e) =>
                {
                    ImagenesSeleccionadas.Remove(imagen);

                    if (ImagenesSeleccionadas.Count == 0)
                    {
                        ImagenPreview.IsVisible = false;
                        MiniaturasLayout.IsVisible = false;
                        ImagenSeleccionada = null;
                        return;
                    }

                    if (ImagenSeleccionada == imagen)
                    {
                        ImagenSeleccionada = ImagenesSeleccionadas.First();

                        ImagenPreview.Source =
                            ImageSource.FromFile(
                                ImagenSeleccionada.FullPath);
                    }

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
                    Margin = new Thickness(0),
                    IsVisible = ImagenSeleccionada == imagen
                };

                contenedor.Children.Add(miniatura);
                contenedor.Children.Add(etiquetaPrincipal);

                eliminarButton.HorizontalOptions = LayoutOptions.End;
                eliminarButton.VerticalOptions = LayoutOptions.Start;

                contenedor.Children.Add(eliminarButton);

                MiniaturasLayout.Children.Add(contenedor);
            }

            MiniaturasLayout.IsVisible =
                ImagenesSeleccionadas.Count > 0;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await CargarCategorias();
        }

        private async Task CargarCategorias()
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
                    Timeout = TimeSpan.FromSeconds(10)
                };

                client.DefaultRequestHeaders.ConnectionClose = true;

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

        private async void OnPublicarClicked(object sender, EventArgs e)
        {
            try
            {
                PublicarButton.IsEnabled = false;
                if (!SesionUsuario.EstaLogueado)
                {
                    await DisplayAlertAsync(
                        "Iniciar sesión",
                        "Debes iniciar sesión para publicar un producto.",
                        "OK");

                    return;
                }

                string nombre =
                    NombreEntry.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    await DisplayAlertAsync(
                        "Nombre obligatorio",
                        "El nombre del producto es obligatorio.",
                        "OK");

                    return;
                }

                if (nombre.Length > 100)
                {
                    await DisplayAlertAsync(
                        "Nombre demasiado largo",
                        "El nombre del producto no puede superar los 100 caracteres.",
                        "OK");

                    return;
                }

                if (nombre.Length < 10)
                {
                    await DisplayAlertAsync(
                        "Nombre demasiado corto",
                        "El nombre del producto debe tener al menos 10 caracteres.",
                        "OK");

                    return;
                }

                string descripcion =
    DescripcionEditor.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(descripcion))
                {
                    await DisplayAlertAsync(
                        "Descripción obligatoria",
                        "La descripción del producto es obligatoria.",
                        "OK");

                    return;
                }

                if (descripcion.Length > 500)
                {
                    await DisplayAlertAsync(
                        "Descripción demasiado larga",
                        "La descripción no puede superar los 500 caracteres.",
                        "OK");

                    return;
                }

                if (descripcion.Length < 25)
                {
                    await DisplayAlertAsync(
                        "Descripción demasiado corta",
                        "La descripción debe tener al menos 25 caracteres.",
                        "OK");

                    return;
                }

                string precioTexto =
                    PrecioEntry.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(nombre) ||
                    string.IsNullOrWhiteSpace(descripcion) ||
                    string.IsNullOrWhiteSpace(precioTexto) ||
                    CondicionPicker.SelectedIndex == -1 ||
                    CategoriaPicker.SelectedIndex == -1)
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
                out decimal precio))
                {
                    await DisplayAlertAsync(
                        "Precio inválido",
                        "Escribe un precio válido. Ejemplo: 350 o 350.50",
                        "OK");

                    return;
                }

                if (precio <= 0)
                {
                    await DisplayAlertAsync(
                        "Precio inválido",
                        "El precio debe ser mayor que $0.",
                        "OK");

                    return;
                }

                Categoria categoriaSeleccionada =
                Categorias[CategoriaPicker.SelectedIndex];

                if (ImagenSeleccionada != null)
                {
                    using var imagenStream =
                        await ImagenSeleccionada.OpenReadAsync();

                    const long limiteImagen = 20 * 1024 * 1024;

                    if (imagenStream.Length > limiteImagen)
                    {
                        await DisplayAlertAsync(
                            "Imagen demasiado grande",
                            "La imagen no puede superar los 20 MB.",
                            "OK");

                        return;
                    }
                }

                var producto = new
                {
                    IdUsuario = SesionUsuario.UsuarioActual!.IdUsuario,
                    IdCategoria = categoriaSeleccionada.IdCategoria,
                    Nombre = nombre,
                    Descripcion = descripcion,
                    Precio = precio,
                    Condicion = CondicionPicker.SelectedItem?.ToString() ?? string.Empty,
                    Estado = "Disponible"
                };

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
                    $"{ApiConfig.BaseUrl}/api/Productos";

                string json =
                    JsonSerializer.Serialize(producto);

                using var content = new StringContent(
                    json,
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response =
                await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string respuestaJson =
                        await response.Content.ReadAsStringAsync();

                    var productoCreado =
                        JsonSerializer.Deserialize<Producto>(
                        respuestaJson,
                        JsonOptions);

                    if (productoCreado == null)
                    {
                        await DisplayAlertAsync(
                            "Error",
                            "El producto se creó, pero no se pudo obtener su identificador.",
                            "OK");

                        return;
                    }

                    int idProducto = productoCreado.IdProducto;

                    if (ImagenesSeleccionadas.Count == 0)
                    {
                        await DisplayAlertAsync(
                            "Producto creado",
                            $"El producto se creó correctamente.\nID: {idProducto}\n\nNo se seleccionó ninguna imagen.",
                            "OK");

                        await Shell.Current.GoToAsync("..");
                        return;
                    }

                    string subirImagenUrl =
                        $"{ApiConfig.BaseUrl}/api/ImagenesProducto/Subir?idProducto={idProducto}";

                    string imagenRegistroUrl =
                        $"{ApiConfig.BaseUrl}/api/ImagenesProducto";

                    List<string> nombresImagenes = [];

                    var imagenesOrdenadas = ImagenesSeleccionadas
                    .OrderByDescending(imagen => imagen == ImagenSeleccionada)
                    .ToList();

                    for (int i = 0; i < imagenesOrdenadas.Count; i++)
                    {
                        FileResult imagen = imagenesOrdenadas[i];

                        using var imagenContent =
                            new MultipartFormDataContent();

                        using var archivoStream =
                            await imagen.OpenReadAsync();

                        using var streamContent =
                            new StreamContent(archivoStream);

                        string extension = Path.GetExtension(imagen.FileName)
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
                            await DisplayAlertAsync(
                                "Formato no válido",
                                $"La imagen {imagen.FileName} no tiene un formato permitido. Usa JPG, JPEG, PNG o WEBP.",
                                "Aceptar");

                            return;
                        }

                        streamContent.Headers.ContentType =
                            new MediaTypeHeaderValue(tipoMime);

                        imagenContent.Add(
                            streamContent,
                            "archivo",
                            imagen.FileName);

                        var imagenResponse =
                            await client.PostAsync(
                                subirImagenUrl,
                                imagenContent);

                        if (!imagenResponse.IsSuccessStatusCode)
                        {
                            string mensajeImagen =
                                await imagenResponse.Content.ReadAsStringAsync();

                            await DisplayAlertAsync(
                                "Imagen",
                                $"El producto se creó, pero la imagen {i + 1} no se pudo subir.\n\n{mensajeImagen}",
                                "OK");

                            return;
                        }

                        string imagenJson =
                            await imagenResponse.Content.ReadAsStringAsync();

                        using var imagenDocumento =
                            JsonDocument.Parse(imagenJson);

                        string nombreImagen =
                            imagenDocumento.RootElement
                                .GetProperty("urlImagen")
                                .GetString() ?? string.Empty;

                        var imagenDto = new
                        {
                            IdProducto = idProducto,
                            UrlImagen = nombreImagen,
                            EsPrincipal = ImagenSeleccionada == imagen
                        };

                        string imagenRegistroJson =
                            JsonSerializer.Serialize(imagenDto);

                        using var imagenRegistroContent =
                            new StringContent(
                                imagenRegistroJson,
                                System.Text.Encoding.UTF8,
                                "application/json");

                        var registroResponse =
                            await client.PostAsync(
                                imagenRegistroUrl,
                                imagenRegistroContent);

                        if (!registroResponse.IsSuccessStatusCode)
                        {
                            string mensajeRegistro =
                                await registroResponse.Content.ReadAsStringAsync();

                            await DisplayAlertAsync(
                                "Imagen",
                                $"La imagen {i + 1} se subió, pero no se pudo relacionar con el producto.\n\n{mensajeRegistro}",
                                "OK");

                            return;
                        }

                        nombresImagenes.Add(nombreImagen);
                    }

                    await DisplayAlertAsync(
                        "Publicación completada",
                        $"El producto se publicó correctamente.\n\nProducto: {idProducto}\nImágenes: {nombresImagenes.Count}",
                        "OK");

                    await Shell.Current.GoToAsync("..");

                    return;
                }
                    string mensaje =
                    await response.Content.ReadAsStringAsync();

                await DisplayAlertAsync(
                    "No se pudo publicar",
                    mensaje,
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error de conexión",
                    ex.Message,
                    "OK");
            }
            finally
            {
                PublicarButton.IsEnabled = true;
            }
        }
    }
}
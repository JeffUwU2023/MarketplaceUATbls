using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MarketplaceUAT.App.Pages
{
    [QueryProperty(nameof(IdProducto), "idProducto")]
    public partial class EditarProductoPage : ContentPage
    {
        private string idProducto = string.Empty;
        private List<FileResult> NuevasImagenesSeleccionadas = [];
        private List<ImagenProducto> ImagenesActuales = [];
        private List<ImagenProducto> ImagenesEliminadas = [];
        private ImagenProducto? ImagenPrincipalSeleccionada;
        private FileResult? ImagenPrincipalSeleccionadaNueva;
        private readonly List<(object Imagen, Label Indicador)> IndicadoresPrincipales = new();

        public string IdProducto
        {
            get => idProducto;
            set
            {
                idProducto = value;
                CargarProducto();
            }
        }

        public EditarProductoPage()
        {
            InitializeComponent();
        }

        private async void OnAgregarImagenesClicked(object sender, EventArgs e)
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

                int totalImagenes =
    ImagenesActuales.Count +
    NuevasImagenesSeleccionadas.Count;

                int disponibles =
                    5 - totalImagenes;

                if (disponibles <= 0)
                {
                    await DisplayAlertAsync(
                        "Límite de imágenes",
                        "Este producto ya tiene el máximo de 5 imágenes.",
                        "OK");

                    return;
                }

                if (fotos.Count > disponibles)
                {
                    await DisplayAlertAsync(
                        "Límite de imágenes",
                        $"Solo puedes agregar {disponibles} imagen{(disponibles == 1 ? "" : "es")} más.",
                        "OK");

                    return;
                }

                foreach (var foto in fotos)
                {
                    if (NuevasImagenesSeleccionadas.Count >= 5)
                    {
                        break;
                    }

                    NuevasImagenesSeleccionadas.Add(foto);
                }

                ActualizarMiniaturasNuevas();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudieron seleccionar las imágenes.\n\n{ex.Message}",
                    "OK");
            }
        }

        private void ActualizarMiniaturasNuevas()
        {
            IndicadoresPrincipales.Clear();
            MiniaturasLayout.Children.Clear();

            foreach (ImagenProducto imagen in ImagenesActuales)
            {
                var miniatura = new Image
                {
                    Source = ImageSource.FromUri(
                        new Uri(
                            $"{ApiConfig.BaseUrl}/Images/{imagen.UrlImagen}"
                        )
                    ),
                    WidthRequest = 60,
                    HeightRequest = 60,
                    Aspect = Aspect.AspectFill
                };
                var tapGesture = new TapGestureRecognizer();

                tapGesture.Tapped += (s, e) =>
                {
                    ImagenPrincipalSeleccionada = imagen;
                    ImagenPrincipalSeleccionadaNueva = null;
                    ActualizarIndicadoresPrincipales();
                };

                miniatura.GestureRecognizers.Add(tapGesture);

                var eliminarButton = new Button
                {
                    Text = "✕",
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
                    bool eraPrincipal =
                        ImagenPrincipalSeleccionada == imagen;

                    ImagenesActuales.Remove(imagen);
                    ImagenesEliminadas.Add(imagen);

                    if (eraPrincipal)
                    {
                        ImagenPrincipalSeleccionada =
                            ImagenesActuales.FirstOrDefault();

                        ImagenPrincipalSeleccionadaNueva = null;
                    }

                    ActualizarMiniaturasNuevas();
                };

                var contenedor = new Grid
                {
                    WidthRequest = 70,
                    HeightRequest = 70
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
                    IsVisible = ImagenPrincipalSeleccionada == imagen
                };

                IndicadoresPrincipales.Add((imagen, etiquetaPrincipal));

                contenedor.Children.Add(miniatura);
                contenedor.Children.Add(etiquetaPrincipal);

                eliminarButton.HorizontalOptions = LayoutOptions.End;
                eliminarButton.VerticalOptions = LayoutOptions.Start;

                contenedor.Children.Add(eliminarButton);

                MiniaturasLayout.Children.Add(contenedor);
            }

            foreach (FileResult imagen in NuevasImagenesSeleccionadas)
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
                    ImagenPrincipalSeleccionada = null;
                    ImagenPrincipalSeleccionadaNueva = imagen;
                    ActualizarIndicadoresPrincipales();
                };

                miniatura.GestureRecognizers.Add(tapGesture);

                var eliminarButton = new Button
                {
                    Text = "✕",
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
                    NuevasImagenesSeleccionadas.Remove(imagen);

                    ActualizarMiniaturasNuevas();
                };

                var contenedor = new Grid
                {
                    WidthRequest = 70,
                    HeightRequest = 70
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
                    IsVisible = ImagenPrincipalSeleccionadaNueva == imagen
                };

                IndicadoresPrincipales.Add((imagen, etiquetaPrincipal));

                contenedor.Children.Add(miniatura);
                contenedor.Children.Add(etiquetaPrincipal);

                eliminarButton.HorizontalOptions = LayoutOptions.End;
                eliminarButton.VerticalOptions = LayoutOptions.Start;

                contenedor.Children.Add(eliminarButton);

                MiniaturasLayout.Children.Add(contenedor);
            }

            MiniaturasLayout.IsVisible =
                ImagenesActuales.Count > 0 ||
                NuevasImagenesSeleccionadas.Count > 0;
        }

        private void ActualizarIndicadoresPrincipales()
        {
            foreach (var item in IndicadoresPrincipales)
            {
                bool esPrincipal =
                    (item.Imagen is ImagenProducto existente &&
                     ImagenPrincipalSeleccionada == existente)
                    ||
                    (item.Imagen is FileResult nueva &&
                     ImagenPrincipalSeleccionadaNueva == nueva);

                item.Indicador.IsVisible = esPrincipal;
            }
        }

        private async void CargarProducto()
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
                    $"{ApiConfig.BaseUrl}/api/Productos/{IdProducto}";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "No se pudo obtener la información del producto.",
                        "OK");

                    return;
                }

                string json =
                    await response.Content.ReadAsStringAsync();

                var producto =
                    JsonSerializer.Deserialize<Producto>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (producto == null)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "No se pudo interpretar la información del producto.",
                        "OK");

                    return;
                }

                NombreEntry.Text = producto.Nombre;
                DescripcionEditor.Text = producto.Descripcion;
                PrecioEntry.Text = producto.Precio.ToString("0.00");
                CondicionPicker.SelectedItem = producto.Condicion;
                EstadoPicker.SelectedItem = producto.Estado;

                string categoriasUrl =
                    $"{ApiConfig.BaseUrl}/api/Categorias";

                var categoriasResponse =
                    await client.GetAsync(categoriasUrl);

                if (categoriasResponse.IsSuccessStatusCode)
                {
                    string categoriasJson =
                        await categoriasResponse.Content.ReadAsStringAsync();

                    var categorias =
                        JsonSerializer.Deserialize<List<Categoria>>(
                            categoriasJson,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                    if (categorias != null)
                    {
                        CategoriaPicker.ItemsSource = categorias;

                        CategoriaPicker.SelectedItem =
                            categorias.FirstOrDefault(
                                c => c.IdCategoria == producto.IdCategoria);
                        string imagenesUrl =
    $"{ApiConfig.BaseUrl}/api/ImagenesProducto/Producto/{IdProducto}";

                        var imagenesResponse =
                            await client.GetAsync(imagenesUrl);

                        if (imagenesResponse.IsSuccessStatusCode)
                        {
                            string imagenesJson =
                                await imagenesResponse.Content.ReadAsStringAsync();

                            var imagenes =
                                JsonSerializer.Deserialize<List<ImagenProducto>>(
                                    imagenesJson,
                                    new JsonSerializerOptions
                                    {
                                        PropertyNameCaseInsensitive = true
                                    });

                            if (imagenes != null)
                            {
                                ImagenesActuales = imagenes;

                                ImagenPrincipalSeleccionada =
                                    ImagenesActuales.FirstOrDefault(i => i.EsPrincipal);

                                ImagenPrincipalSeleccionadaNueva = null;

                                ActualizarMiniaturasNuevas();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error de conexión",
                    ex.Message,
                    "OK");
            }
        }
        private async void OnGuardarClicked(
        object sender,
        EventArgs e)
        {
            try
            {
                if (!decimal.TryParse(
                        PrecioEntry.Text,
                        out decimal precio))
                {
                    await DisplayAlertAsync(
                        "Datos incorrectos",
                        "El precio debe ser un número válido.",
                        "OK");

                    return;
                }

                if (CategoriaPicker.SelectedItem is not Categoria categoria)
                {
                    await DisplayAlertAsync(
                        "Datos incompletos",
                        "Selecciona una categoría.",
                        "OK");

                    return;
                }

                if (EstadoPicker.SelectedItem is not string estado)
                {
                    await DisplayAlertAsync(
                        "Datos incompletos",
                        "Selecciona un estado.",
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

                if (CondicionPicker.SelectedItem is not string condicion)
                {
                    await DisplayAlertAsync(
                        "Datos incompletos",
                        "Selecciona una condición.",
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

                var productoActualizado = new
                {
                    IdUsuario = SesionUsuario.UsuarioActual!.IdUsuario,
                    IdCategoria = categoria.IdCategoria,
                    Nombre = nombre,
                    Descripcion = descripcion,
                    Precio = precio,
                    Condicion = condicion,
                    Estado = estado
                };

                string json = JsonSerializer.Serialize(productoActualizado);

                using var content = new StringContent(
                    json,
                    System.Text.Encoding.UTF8,
                    "application/json");

                string url =
                    $"{ApiConfig.BaseUrl}/api/Productos/{IdProducto}";

                var response = await client.PutAsync(
                    url,
                    content);

                if (response.IsSuccessStatusCode)
                {
                    string subirImagenUrl =
                        $"{ApiConfig.BaseUrl}/api/ImagenesProducto/Subir?idProducto={IdProducto}";

                    string imagenRegistroUrl =
                        $"{ApiConfig.BaseUrl}/api/ImagenesProducto";

                    foreach (ImagenProducto imagen in ImagenesEliminadas)
                    {
                        string eliminarImagenUrl =
                            $"{ApiConfig.BaseUrl}/api/ImagenesProducto/{imagen.IdImagen}";

                        var eliminarResponse =
                            await client.DeleteAsync(eliminarImagenUrl);

                        if (!eliminarResponse.IsSuccessStatusCode)
                        {
                            string mensajeImagen =
                                await eliminarResponse.Content.ReadAsStringAsync();

                            await DisplayAlertAsync(
                                "Imagen",
                                $"El producto se actualizó, pero no se pudo eliminar una de las imágenes.\n\n{mensajeImagen}",
                                "OK");

                            return;
                        }
                    }

                    foreach (ImagenProducto imagen in ImagenesActuales)
                    {
                        string actualizarImagenUrl =
                            $"{ApiConfig.BaseUrl}/api/ImagenesProducto/{imagen.IdImagen}";

                        var imagenDto = new
                        {
                            IdProducto = imagen.IdProducto,
                            UrlImagen = imagen.UrlImagen,
                            EsPrincipal = ImagenPrincipalSeleccionada == imagen
                        };

                        string imagenJson =
                            JsonSerializer.Serialize(imagenDto);

                        using var imagenContent =
                            new StringContent(
                                imagenJson,
                                System.Text.Encoding.UTF8,
                                "application/json");

                        var actualizarResponse =
                            await client.PutAsync(
                                actualizarImagenUrl,
                                imagenContent);

                        if (!actualizarResponse.IsSuccessStatusCode)
                        {
                            string mensajeImagen =
                                await actualizarResponse.Content.ReadAsStringAsync();

                            await DisplayAlertAsync(
                                "Imagen",
                                $"No se pudo actualizar la imagen principal.\n\n{mensajeImagen}",
                                "OK");

                            return;
                        }
                    }

                    for (int i = 0; i < NuevasImagenesSeleccionadas.Count; i++)
                    {
                        FileResult imagen = NuevasImagenesSeleccionadas[i];

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
                                $"El producto se actualizó, pero la imagen {i + 1} no se pudo subir.\n\n{mensajeImagen}",
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
                            IdProducto = int.Parse(IdProducto),
                            UrlImagen = nombreImagen,
                            EsPrincipal = ImagenPrincipalSeleccionadaNueva == imagen
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
                    }

                    await DisplayAlertAsync(
                        "Publicación actualizada",
                        "Los cambios se guardaron correctamente.",
                        "OK");

                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    string mensaje =
                        await response.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error",
                        $"No se pudieron guardar los cambios.\n\n{mensaje}",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error de conexión",
                    ex.Message,
                    "OK");
            }
        }
        private async void OnCancelarClicked(
            object sender,
            EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
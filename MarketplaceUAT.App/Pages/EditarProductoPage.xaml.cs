using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using MarketplaceUAT.App.Services;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace MarketplaceUAT.App.Pages
{
    [QueryProperty(nameof(IdProducto), "idProducto")]
    public partial class EditarProductoPage : ContentPage
    {
        private ApiClient Api =>
        Application.Current!.Handler.MauiContext!.Services
        .GetRequiredService<ApiClient>();
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

        private async void CargarProducto()
        {
            try
            {
                string url =
                $"{ApiConfig.BaseUrl}/api/Productos/{IdProducto}";

                using var response =
                    await Api.EnviarAutenticadoAsync(() =>
                        new HttpRequestMessage(HttpMethod.Get, url));

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

                TipoProductoPicker.SelectedIndex =
                    producto.EsProductoUnico ? 0 : 1;

                CantidadDisponibleEntry.Text =
                    producto.CantidadDisponible.ToString();

                bool esProductoUnico = producto.EsProductoUnico;

                CantidadDisponibleLabel.IsVisible = !esProductoUnico;
                CantidadDisponibleEntry.IsVisible = !esProductoUnico;

                string categoriasUrl =
                    $"{ApiConfig.BaseUrl}/api/Categorias";

                using var categoriasResponse =
                await Api.EnviarAutenticadoAsync(() =>
                new HttpRequestMessage(HttpMethod.Get, categoriasUrl));

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

                        using var imagenesResponse =
                        await Api.EnviarAutenticadoAsync(() =>
                        new HttpRequestMessage(HttpMethod.Get, imagenesUrl));

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

                bool esProductoUnico = TipoProductoPicker.SelectedIndex == 0;

                int cantidadDisponible = 1;

                if (TipoProductoPicker.SelectedIndex < 0)
                {
                    await DisplayAlertAsync(
                        "Datos incompletos",
                        "Selecciona el tipo de producto.",
                        "OK");

                    return;
                }

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

                if (nombre.Length < 3)
                {
                    await DisplayAlertAsync(
                        "Nombre demasiado corto",
                        "El nombre del producto debe tener al menos 3 caracteres.",
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

                if (descripcion.Length > 500)
                {
                    await DisplayAlertAsync(
                        "Descripción demasiado larga",
                        "La descripción no puede superar los 500 caracteres.",
                        "OK");

                    return;
                }               

                var productoActualizado = new
                {
                    IdCategoria = categoria.IdCategoria,
                    EsProductoUnico = esProductoUnico,
                    CantidadDisponible = cantidadDisponible,
                    Nombre = nombre,
                    Descripcion = descripcion,
                    Precio = precio,
                    Condicion = condicion
                };

                string json = JsonSerializer.Serialize(productoActualizado);

                string url =
    $"{ApiConfig.BaseUrl}/api/Productos/{IdProducto}";

                using var response =
                    await Api.EnviarAutenticadoAsync(() =>
                    {
                        var content = new StringContent(
                            json,
                            System.Text.Encoding.UTF8,
                            "application/json");

                        return new HttpRequestMessage(HttpMethod.Put, url)
                        {
                            Content = content
                        };
                    });

                if (await ManejarSesionExpiradaAsync(response))
                    return;

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

                        using var eliminarResponse =
    await Api.EnviarAutenticadoAsync(() =>
        new HttpRequestMessage(
            HttpMethod.Delete,
            eliminarImagenUrl));

                        if (await ManejarSesionExpiradaAsync(eliminarResponse))
                            return;

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

                        using var actualizarResponse =
    await Api.EnviarAutenticadoAsync(() =>
    {
        var nuevoContent = new StringContent(
            imagenJson,
            System.Text.Encoding.UTF8,
            "application/json");

        return new HttpRequestMessage(
            HttpMethod.Put,
            actualizarImagenUrl)
        {
            Content = nuevoContent
        };
    });

                        if (await ManejarSesionExpiradaAsync(actualizarResponse))
                            return;

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

                        using var imagenResponse =
    await Api.EnviarAutenticadoAsync(async () =>
    {
        var nuevoContent = new MultipartFormDataContent();

        var nuevoStream = await imagen.OpenReadAsync();
        var nuevoStreamContent = new StreamContent(nuevoStream);

        nuevoStreamContent.Headers.ContentType =
            new MediaTypeHeaderValue(tipoMime);

        nuevoContent.Add(
            nuevoStreamContent,
            "archivo",
            imagen.FileName);

        return new HttpRequestMessage(
            HttpMethod.Post,
            subirImagenUrl)
        {
            Content = nuevoContent
        };
    });

                        if (await ManejarSesionExpiradaAsync(imagenResponse))
                            return;

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

                        using var registroResponse =
    await Api.EnviarAutenticadoAsync(() =>
    {
        var nuevoContent = new StringContent(
            imagenRegistroJson,
            System.Text.Encoding.UTF8,
            "application/json");

        return new HttpRequestMessage(
            HttpMethod.Post,
            imagenRegistroUrl)
        {
            Content = nuevoContent
        };
    });

                        if (await ManejarSesionExpiradaAsync(registroResponse))
                            return;

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

        private async Task<bool> ManejarSesionExpiradaAsync(
    HttpResponseMessage response)
        {
            if ((int)response.StatusCode != 401)
                return false;

            await SesionUsuario.CerrarSesionAsync();

            await DisplayAlertAsync(
                "Sesión expirada",
                "Tu sesión ya no es válida. Inicia sesión nuevamente.",
                "OK");

            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync(nameof(LoginPage));
            }

            return true;
        }

        private async void OnCancelarClicked(
            object sender,
            EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
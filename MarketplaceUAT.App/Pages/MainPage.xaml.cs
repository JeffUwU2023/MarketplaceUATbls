using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using MarketplaceUAT.App.Services;
using System.Text.Json;

namespace MarketplaceUAT.App.Pages
{
    public partial class MainPage : ContentPage
    {
        private ApiClient Api =>
        Application.Current!.Handler.MauiContext!.Services
        .GetRequiredService<ApiClient>();
        private List<Producto> TodosLosProductos = new();
        private HashSet<int> IdsProductosFavoritos = new();
        private HashSet<int> IdsProductosCarrito = new();
        private int _ultimoIndiceVisible = -1;
        private int? _ultimoProductoVisibleId = null;

        public MainPage()
        {
            InitializeComponent();
            ActualizarSesion();
            ActualizarBotonConversaciones();
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            ActualizarSesion();
            ActualizarBotonConversaciones();
            await ActualizarInsigniaConversaciones();
            await ActualizarInsigniasTransacciones();
            await CargarIdsFavoritos();
            await CargarProductos();
            await CargarIdsCarrito();
        }

        private void OnProductosScrolled(
    object sender,
    ItemsViewScrolledEventArgs e)
        {
            if (e.FirstVisibleItemIndex >= 0)
            {
                _ultimoIndiceVisible = e.FirstVisibleItemIndex;

                if (ProductosCollection.ItemsSource is List<Producto> productos &&
                    e.FirstVisibleItemIndex < productos.Count)
                {
                    _ultimoProductoVisibleId =
                        productos[e.FirstVisibleItemIndex].IdProducto;
                }
            }
        }

        private void ActualizarBotonConversaciones()
        {
            ConversacionesButton.IsVisible =
                SesionUsuario.EstaLogueado;

            if (!SesionUsuario.EstaLogueado)
            {
                ConversacionesBadge.IsVisible = false;
                ConversacionesBadgeLabel.Text = string.Empty;
            }
        }

        private class ConversacionNotificacionDto
        {
            public int IdConversacion { get; set; }

            public int MensajesSinLeer { get; set; }
        }

        private class NotificacionesTransaccionConteoDto
        {
            public int Compras { get; set; }

            public int Ventas { get; set; }
        }

        private async Task ActualizarInsigniaConversaciones()
        {
            if (!SesionUsuario.EstaLogueado)
            {
                ConversacionesBadge.IsVisible = false;
                ConversacionesBadgeLabel.Text = string.Empty;
                return;
            }

            try
            {
                string url =
    $"{ApiConfig.BaseUrl}/api/Conversaciones/Usuario";

                using var response = await Api.EnviarAutenticadoAsync(() =>
                    new HttpRequestMessage(HttpMethod.Get, url));

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"No se pudo consultar la insignia de chats. " +
                        $"Código: {(int)response.StatusCode}");

                    return;
                }

                string json = await response.Content.ReadAsStringAsync();

                var conversaciones =
                    JsonSerializer.Deserialize<List<ConversacionNotificacionDto>>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                int conversacionesSinLeer = conversaciones?
                    .Count(c => c.MensajesSinLeer > 0) ?? 0;

                ConversacionesBadgeLabel.Text =
                    conversacionesSinLeer.ToString();

                ConversacionesBadge.IsVisible =
                    conversacionesSinLeer > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error al actualizar la insignia de chats: {ex.Message}");
            }
        }

        private async Task ActualizarInsigniasTransacciones()
        {
            if (!SesionUsuario.EstaLogueado)
            {
                MisComprasBadge.IsVisible = false;
                MisComprasBadgeLabel.Text = string.Empty;

                MisVentasBadge.IsVisible = false;
                MisVentasBadgeLabel.Text = string.Empty;

                return;
            }

            try
            {
                string url =
    $"{ApiConfig.BaseUrl}/api/NotificacionesTransacciones/ConteoPorSeccion";

                using var response = await Api.EnviarAutenticadoAsync(() =>
                    new HttpRequestMessage(HttpMethod.Get, url));

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"No se pudieron consultar las insignias de transacciones. " +
                        $"Código: {(int)response.StatusCode}");

                    return;
                }

                string json = await response.Content.ReadAsStringAsync();

                var conteo =
                    JsonSerializer.Deserialize<NotificacionesTransaccionConteoDto>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                int compras = conteo?.Compras ?? 0;
                int ventas = conteo?.Ventas ?? 0;

                MisComprasBadgeLabel.Text = compras.ToString();
                MisComprasBadge.IsVisible = compras > 0;

                MisVentasBadgeLabel.Text = ventas.ToString();
                MisVentasBadge.IsVisible = ventas > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error al actualizar las insignias de transacciones: {ex.Message}");
            }
        }

        private async Task CargarIdsFavoritos()
        {
            IdsProductosFavoritos.Clear();

            if (!SesionUsuario.EstaLogueado)
                return;

            try
            {
                string url = $"{ApiConfig.BaseUrl}/api/Favoritos";

                using var response = await Api.EnviarAutenticadoAsync(() =>
                    new HttpRequestMessage(HttpMethod.Get, url));

                if (!response.IsSuccessStatusCode)
                    return;

                string json = await response.Content.ReadAsStringAsync();

                var favoritos = JsonSerializer.Deserialize<List<Favorito>>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (favoritos == null)
                    return;

                IdsProductosFavoritos = favoritos
                    .Select(f => f.IdProducto)
                    .ToHashSet();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error al cargar favoritos: {ex.Message}");
            }
        }

        private async Task CargarIdsCarrito()
        {
            IdsProductosCarrito.Clear();

            if (!SesionUsuario.EstaLogueado)
                return;

            try
            {
                string url = $"{ApiConfig.BaseUrl}/api/Carrito";

                using var response = await Api.EnviarAutenticadoAsync(() =>
                    new HttpRequestMessage(HttpMethod.Get, url));

                if (!response.IsSuccessStatusCode)
                    return;

                string json = await response.Content.ReadAsStringAsync();

                var productos = JsonSerializer.Deserialize<List<CarritoItem>>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (productos == null)
                    return;

                IdsProductosCarrito = productos
                    .Select(p => p.IdProducto)
                    .ToHashSet();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error al cargar productos del carrito: {ex.Message}");
            }
        }

        private async Task CargarProductos()
        {
            try
            {
                var client = Api.Client;

                string url = $"{ApiConfig.BaseUrl}/api/Productos";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync(
                        "Error",
                        $"No se pudieron obtener los productos.\nCódigo: {(int)response.StatusCode}",
                        "OK");

                    return;
                }

                string json = await response.Content.ReadAsStringAsync();

                var productos =
                    JsonSerializer.Deserialize<List<Producto>>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (productos == null)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "No se pudieron interpretar los productos.",
                        "OK");

                    return;
                }

                foreach (var producto in productos)
                {
                    // Imagen predeterminada si no se encuentra una principal.
                    producto.ImagenUrl = "dotnet_bot.png";

                    try
                    {
                        string imagenesUrl =
                            $"{ApiConfig.BaseUrl}/api/ImagenesProducto/Producto/{producto.IdProducto}";

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

                            var imagenPrincipal = imagenes?
                                .FirstOrDefault(i => i.EsPrincipal);

                            if (imagenPrincipal != null &&
                                !string.IsNullOrWhiteSpace(
                                    imagenPrincipal.UrlImagen))
                            {
                                producto.ImagenUrl =
                                    $"{ApiConfig.BaseUrl.TrimEnd('/')}/Images/{imagenPrincipal.UrlImagen.TrimStart('/')}";
                            }
                        }
                    }
                    catch
                    {
                        // Si falla la carga de una imagen, se conserva
                        // la imagen predeterminada del producto.
                    }
                }

                TodosLosProductos = productos;

                // Marcar cuáles productos están en favoritos y en el carrito.
                foreach (var producto in TodosLosProductos)
                {
                    producto.EsFavorito =
                        IdsProductosFavoritos.Contains(producto.IdProducto);

                    producto.EstaEnCarrito =
                        IdsProductosCarrito.Contains(producto.IdProducto);
                }

                // Mostrar primero los productos más recientes.
                var resultado = TodosLosProductos
                    .OrderByDescending(p => p.FechaPublicacion)
                    .ToList();

                ProductosCollection.ItemsSource = resultado;

                if (_ultimoProductoVisibleId.HasValue)
                {
                    int indice = resultado.FindIndex(
                        p => p.IdProducto == _ultimoProductoVisibleId.Value);

                    if (indice >= 0)
                    {
                        await Task.Delay(300);

                        ProductosCollection.ScrollTo(
                            indice,
                            position: ScrollToPosition.Start,
                            animate: false);
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

        private async void OnBuscarCompleted(
            object sender,
            EventArgs e)
        {
            await AbrirBusqueda();
        }

        private async Task AbrirBusqueda()
        {
            string textoBusqueda =
                BuscarEntry.Text?.Trim() ?? string.Empty;

            string ruta = string.IsNullOrWhiteSpace(textoBusqueda)
                ? nameof(SearchPage)
                : $"{nameof(SearchPage)}?textoBusqueda={Uri.EscapeDataString(textoBusqueda)}";

            await Shell.Current.GoToAsync(ruta);
        }

        private async void OnCategoriaClicked(
            object sender,
            EventArgs e)
        {
            if (sender is not Button button ||
                !int.TryParse(
                    button.CommandParameter?.ToString(),
                    out int idCategoria))
            {
                return;
            }

            await Shell.Current.GoToAsync(
                $"{nameof(SearchPage)}?categoriaId={idCategoria}");
        }

        private async void OnTodasCategoriasClicked(
            object sender,
            EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(SearchPage));
        }

        private async void OnSesionButtonClicked(
            object sender,
            EventArgs e)
        {
            if (SesionUsuario.EstaLogueado)
            {
                bool confirmar = await DisplayAlertAsync(
                    "Cerrar sesión",
                    "¿Quieres cerrar la sesión actual?",
                    "Sí",
                    "No");

                if (!confirmar)
                {
                    return;
                }

                SesionUsuario.CerrarSesion();

                ActualizarSesion();
                ActualizarBotonConversaciones();

                return;
            }

            await Shell.Current.GoToAsync(nameof(LoginPage));
        }

        private async void OnAbrirFavoritosClicked(
            object sender,
            EventArgs e)
        {
            if (!SesionUsuario.EstaLogueado)
            {
                await Shell.Current.GoToAsync(
                    nameof(RequiereCuentaPage));

                return;
            }

            await Shell.Current.GoToAsync(
                nameof(FavoritosPage));
        }

        private async void OnAbrirPublicarProductoClicked(
            object sender,
            EventArgs e)
        {
            if (!SesionUsuario.EstaLogueado)
            {
                await Shell.Current.GoToAsync(
                    nameof(RequiereCuentaPage));

                return;
            }

            await Shell.Current.GoToAsync(
                nameof(PublicarProductoPage));
        }

        private async void OnAbrirMisPublicacionesClicked(
            object sender,
            EventArgs e)
        {
            if (!SesionUsuario.EstaLogueado)
            {
                await Shell.Current.GoToAsync(
                    nameof(RequiereCuentaPage));

                return;
            }

            await Shell.Current.GoToAsync(
                nameof(MisPublicacionesPage));
        }

        private async void OnConversacionesClicked(
            object sender,
            EventArgs e)
        {
            if (!SesionUsuario.EstaLogueado)
            {
                await Shell.Current.GoToAsync(
                    nameof(RequiereCuentaPage));

                return;
            }

            await Shell.Current.GoToAsync(
                nameof(ConversacionesPage));
        }

        private async void OnAbrirMisComprasClicked(
    object sender,
    EventArgs e)
        {
            if (!SesionUsuario.EstaLogueado)
            {
                await Shell.Current.GoToAsync(
                    nameof(RequiereCuentaPage));

                return;
            }

            await Shell.Current.GoToAsync(
                nameof(MisComprasPage));
        }

        private async void OnAbrirMisVentasClicked(
            object sender,
            EventArgs e)
        {
            if (!SesionUsuario.EstaLogueado)
            {
                await Shell.Current.GoToAsync(
                    nameof(RequiereCuentaPage));

                return;
            }

            await Shell.Current.GoToAsync(
                nameof(MisVentasPage));
        }

        public void ActualizarEstadoSesion()
        {
            ActualizarSesion();
            ActualizarBotonConversaciones();
        }

        private void ActualizarSesion()
        {
            MisComprasContenedor.IsVisible = SesionUsuario.EstaLogueado;
            MisVentasContenedor.IsVisible = SesionUsuario.EstaLogueado;
            CarritoButton.IsVisible = SesionUsuario.EstaLogueado;

            if (SesionUsuario.EstaLogueado)
            {
                var usuario = SesionUsuario.UsuarioActual!;

                SesionButton.Text =
                    $"👤 {usuario.Nombres} {usuario.Apellidos} - Cerrar sesión";

                PerfilAvatarBorder.IsVisible = true;

                if (!string.IsNullOrWhiteSpace(usuario.UrlFotoPerfil))
                {
                    string rutaFoto =
                        usuario.UrlFotoPerfil.TrimStart('/');

                    string urlFoto =
                        $"{ApiConfig.BaseUrl.TrimEnd('/')}/{rutaFoto}";

                    if (Uri.TryCreate(
                        urlFoto,
                        UriKind.Absolute,
                        out Uri? uriFoto))
                    {
                        PerfilFotoImage.Source = new UriImageSource
                        {
                            Uri = uriFoto,
                            CachingEnabled = false
                        };

                        PerfilFotoImage.IsVisible = true;
                        PerfilPlaceholder.IsVisible = false;
                    }
                    else
                    {
                        MostrarAvatarPredeterminado();
                    }
                }
                else
                {
                    MostrarAvatarPredeterminado();
                }
            }
            else
            {
                SesionButton.Text = "Iniciar sesión";
                PerfilAvatarBorder.IsVisible = false;

                MostrarAvatarPredeterminado();
            }
        }

        private void MostrarAvatarPredeterminado()
        {
            PerfilFotoImage.Source = null;
            PerfilFotoImage.IsVisible = false;
            PerfilPlaceholder.IsVisible = true;
        }

        private async void OnProductoTapped(
    object sender,
    TappedEventArgs e)
        {
            if (sender is Border border &&
                border.BindingContext is Producto producto)
            {
                _ultimoProductoVisibleId = producto.IdProducto;

                await Shell.Current.GoToAsync(
                    $"{nameof(ProductoDetallePage)}?idProducto={producto.IdProducto}");
            }
        }

        private async void OnFavoritoCorazonClicked(
            object sender,
            EventArgs e)
        {
            if (sender is not Button button ||
                button.BindingContext is not Producto producto)
            {
                return;
            }

            if (!SesionUsuario.EstaLogueado)
            {
                await Shell.Current.GoToAsync(
                    nameof(RequiereCuentaPage));

                return;
            }

            try
            {

                if (producto.EsFavorito)
                {
                    // Buscar el favorito correspondiente para obtener su ID.
                    string consultaUrl = $"{ApiConfig.BaseUrl}/api/Favoritos";

                    using var consultaResponse =
                        await Api.EnviarAutenticadoAsync(() =>
                            new HttpRequestMessage(HttpMethod.Get, consultaUrl));

                    if (!consultaResponse.IsSuccessStatusCode)
                    {
                        await DisplayAlertAsync(
                            "Favoritos",
                            "No se pudieron consultar tus favoritos.",
                            "OK");
                        return;
                    }

                    string consultaJson =
                        await consultaResponse.Content.ReadAsStringAsync();

                    var favoritos = JsonSerializer.Deserialize<List<Favorito>>(
                        consultaJson,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    var favorito = favoritos?.FirstOrDefault(
                        f => f.IdProducto == producto.IdProducto);

                    if (favorito == null)
                    {
                        producto.EsFavorito = false;
                        return;
                    }

                    string eliminarUrl =
                    $"{ApiConfig.BaseUrl}/api/Favoritos/{favorito.IdFavorito}";

                    using var eliminarResponse =
                        await Api.EnviarAutenticadoAsync(() =>
                            new HttpRequestMessage(HttpMethod.Delete, eliminarUrl));

                    if (eliminarResponse.IsSuccessStatusCode)
                    {
                        producto.EsFavorito = false;
                        IdsProductosFavoritos.Remove(producto.IdProducto);
                    }
                    else
                    {
                        await DisplayAlertAsync(
                            "Favoritos",
                            "No se pudo quitar el producto de tus favoritos.",
                            "OK");
                    }
                }
                else
                {
                    string agregarUrl = $"{ApiConfig.BaseUrl}/api/Favoritos";

                    var contenidoJson = JsonSerializer.Serialize(new
                    {
                        idUsuario = SesionUsuario.UsuarioActual!.IdUsuario,
                        idProducto = producto.IdProducto
                    });

                    using var agregarResponse =
                        await Api.EnviarAutenticadoAsync(() =>
                            new HttpRequestMessage(HttpMethod.Post, agregarUrl)
                            {
                                Content = new StringContent(
                                    contenidoJson,
                                    System.Text.Encoding.UTF8,
                                    "application/json")
                            });

                    if (agregarResponse.IsSuccessStatusCode)
                    {
                        producto.EsFavorito = true;
                        IdsProductosFavoritos.Add(producto.IdProducto);
                    }
                    else if (agregarResponse.StatusCode ==
                             System.Net.HttpStatusCode.Conflict)
                    {
                        // Ya estaba guardado: sincronizar el corazón.
                        producto.EsFavorito = true;
                        IdsProductosFavoritos.Add(producto.IdProducto);
                    }
                    else
                    {
                        string mensaje =
                            await agregarResponse.Content.ReadAsStringAsync();

                        await DisplayAlertAsync(
                            "Favoritos",
                            $"No se pudo agregar el producto a favoritos.\n{mensaje}",
                            "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"Ocurrió un problema al actualizar el favorito.\n{ex.Message}",
                    "OK");
            }
        }

        private async void OnCarritoProductoClicked(
    object sender,
    EventArgs e)
        {
            if (sender is not Button button ||
                button.BindingContext is not Producto producto)
            {
                return;
            }

            if (!SesionUsuario.EstaLogueado)
            {
                await Shell.Current.GoToAsync(
                    nameof(RequiereCuentaPage));

                return;
            }

            // Evitar agregar productos propios.
            if (producto.IdUsuario ==
                SesionUsuario.UsuarioActual!.IdUsuario)
            {
                await DisplayAlertAsync(
                    "Carrito",
                    "No puedes agregar tus propios productos al carrito.",
                    "OK");

                return;
            }

            // Evitar agregar productos que ya no están disponibles.
            if (producto.Estado != "Disponible" ||
                producto.CantidadDisponible <= 0)
            {
                await DisplayAlertAsync(
                    "Carrito",
                    "Este producto ya no está disponible.",
                    "OK");

                return;
            }

            try
            {
                if (producto.EstaEnCarrito)
                {
                    // Consultar el carrito para obtener el IdCarrito.
                    string consultaUrl =
                        $"{ApiConfig.BaseUrl}/api/Carrito";

                    using var consultaResponse =
                        await Api.EnviarAutenticadoAsync(() =>
                            new HttpRequestMessage(
                                HttpMethod.Get,
                                consultaUrl));

                    if (!consultaResponse.IsSuccessStatusCode)
                    {
                        await DisplayAlertAsync(
                            "Carrito",
                            "No se pudo consultar tu carrito.",
                            "OK");

                        return;
                    }

                    string consultaJson =
                        await consultaResponse.Content.ReadAsStringAsync();

                    var productosCarrito =
                        JsonSerializer.Deserialize<List<CarritoItem>>(
                            consultaJson,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                    var registro = productosCarrito?.FirstOrDefault(
                        p => p.IdProducto == producto.IdProducto);

                    if (registro == null)
                    {
                        producto.EstaEnCarrito = false;
                        IdsProductosCarrito.Remove(producto.IdProducto);
                        return;
                    }

                    // Quitar el producto del carrito.
                    string eliminarUrl =
                        $"{ApiConfig.BaseUrl}/api/Carrito/{registro.IdCarrito}";

                    using var eliminarResponse =
                        await Api.EnviarAutenticadoAsync(() =>
                            new HttpRequestMessage(
                                HttpMethod.Delete,
                                eliminarUrl));

                    if (eliminarResponse.IsSuccessStatusCode)
                    {
                        producto.EstaEnCarrito = false;
                        IdsProductosCarrito.Remove(producto.IdProducto);
                    }
                    else
                    {
                        await DisplayAlertAsync(
                            "Carrito",
                            "No se pudo quitar el producto de tu carrito.",
                            "OK");
                    }
                }
                else
                {
                    // Agregar el producto al carrito.
                    string agregarUrl =
                        $"{ApiConfig.BaseUrl}/api/Carrito";

                    var contenidoJson = JsonSerializer.Serialize(new
                    {
                        idProducto = producto.IdProducto,
                        cantidad = 1
                    });

                    using var agregarResponse =
                        await Api.EnviarAutenticadoAsync(() =>
                            new HttpRequestMessage(
                                HttpMethod.Post,
                                agregarUrl)
                            {
                                Content = new StringContent(
                                    contenidoJson,
                                    System.Text.Encoding.UTF8,
                                    "application/json")
                            });

                    if (agregarResponse.IsSuccessStatusCode)
                    {
                        producto.EstaEnCarrito = true;
                        IdsProductosCarrito.Add(producto.IdProducto);
                    }
                    else
                    {
                        string mensaje =
                            await agregarResponse.Content.ReadAsStringAsync();

                        await DisplayAlertAsync(
                            "Carrito",
                            $"No se pudo agregar el producto al carrito.\n{mensaje}",
                            "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"Ocurrió un problema al actualizar el carrito.\n{ex.Message}",
                    "OK");
            }
        }

        private async void OnPerfilAvatarTapped(
            object sender,
            TappedEventArgs e)
        {
            if (!SesionUsuario.EstaLogueado)
            {
                return;
            }

            await Shell.Current.GoToAsync(
                nameof(PerfilPage));
        }

        private async void OnAbrirCarritoClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(CarritoPage));
        }
    }
}

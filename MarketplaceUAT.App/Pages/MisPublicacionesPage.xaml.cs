using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using System.Text.Json;

namespace MarketplaceUAT.App.Pages
{
    public partial class MisPublicacionesPage : ContentPage
    {
        public MisPublicacionesPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await CargarMisPublicaciones();
        }
        private List<Producto> TodasMisPublicaciones = new();
        private string OrdenSeleccionado = "Más recientes";
        private async Task CargarMisPublicaciones()
        {
            try
            {
                if (!SesionUsuario.EstaLogueado)
                {
                    await Shell.Current.GoToAsync(
                        nameof(RequiereCuentaPage));

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

                string url =
                    $"{ApiConfig.BaseUrl}/api/Productos";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync(
                        "Mis publicaciones",
                        "No se pudieron obtener tus publicaciones.",
                        "OK");

                    return;
                }

                string json =
                    await response.Content.ReadAsStringAsync();

                var productos = JsonSerializer.Deserialize<List<Producto>>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (productos == null)
                {
                    return;
                }

                int idUsuario =
                    SesionUsuario.UsuarioActual!.IdUsuario;

                var misProductos = productos
                    .Where(p => p.IdUsuario == idUsuario)
                    .ToList();

                foreach (var producto in misProductos)
                {
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
                                .FirstOrDefault(i =>
                                    i.EsPrincipal && i.Activo);

                            if (imagenPrincipal != null)
                            {
                                producto.ImagenUrl =
                                   $"{ApiConfig.BaseUrl}/Images/{imagenPrincipal.UrlImagen}";
                            }
                            else
                            {
                                producto.ImagenUrl = "dotnet_bot.png";
                            }
                        }
                    }
                    catch
                    {
                        producto.ImagenUrl = "dotnet_bot.png";
                    }
                }

                TodasMisPublicaciones = misProductos;

                BuscarPublicacionEntry.IsVisible =
                    TodasMisPublicaciones.Count > 0;

                OrdenarPublicacionesPicker.IsVisible =
                    TodasMisPublicaciones.Count > 0;

                AplicarFiltros();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    ex.Message,
                    "OK");
            }
        }
        private async void OnPublicacionTapped(
            object sender,
            TappedEventArgs e)
        {
            if (sender is Border border &&
                border.BindingContext is Producto producto)
            {
                await Shell.Current.GoToAsync(
                    $"{nameof(ProductoDetallePage)}?idProducto={producto.IdProducto}");
            }
        }

        private async void OnPublicarProductoClicked(
            object sender,
            EventArgs e)
        {
            await Shell.Current.GoToAsync(
                nameof(PublicarProductoPage));
        }

        private async void OnEditarClicked(
        object sender,
        EventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.BindingContext is not Producto producto)
                return;

            await Shell.Current.GoToAsync(
                $"{nameof(EditarProductoPage)}?idProducto={producto.IdProducto}");
        }
        private void OnBuscarPublicacionTextChanged(
        object sender,
        TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void OnOrdenarPublicacionesChanged(
        object sender,
        EventArgs e)
        {
            if (OrdenarPublicacionesPicker.SelectedItem is string orden)
            {
                OrdenSeleccionado = orden;

                AplicarFiltros();
            }
        }

        private void AplicarFiltros()
        {
            string texto =
                BuscarPublicacionEntry.Text?.Trim() ?? string.Empty;

            SinPublicacionesLayout.IsVisible = false;
            SinResultadosBusquedaLayout.IsVisible = false;

            var publicaciones = TodasMisPublicaciones.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(texto))
            {
                publicaciones = publicaciones.Where(p =>
                    p.Nombre.Contains(
                        texto,
                        StringComparison.OrdinalIgnoreCase));
            }

            var resultado = publicaciones.ToList();

            switch (OrdenSeleccionado)
            {
                case "Más recientes":
                    resultado = resultado
                        .OrderByDescending(p => p.FechaPublicacion)
                        .ToList();
                    break;

                case "Más antiguas":
                    resultado = resultado
                        .OrderBy(p => p.FechaPublicacion)
                        .ToList();
                    break;
            }

            MisPublicacionesCollection.ItemsSource = resultado;

            if (TodasMisPublicaciones.Count == 0)
            {
                SinPublicacionesLayout.IsVisible = true;
                SinResultadosBusquedaLayout.IsVisible = false;
            }
            else if (!string.IsNullOrWhiteSpace(texto) &&
                     resultado.Count == 0)
            {
                SinPublicacionesLayout.IsVisible = false;
                SinResultadosBusquedaLayout.IsVisible = true;
            }
            else
            {
                SinPublicacionesLayout.IsVisible = false;
                SinResultadosBusquedaLayout.IsVisible = false;
            }
        }

        private async void OnEliminarClicked(
        object sender,
        EventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.BindingContext is not Producto producto)
                return;

            bool confirmar = await DisplayAlertAsync(
                "Eliminar publicación",
                $"¿Quieres eliminar la publicación \"{producto.Nombre}\"?",
                "Sí",
                "No");

            if (!confirmar)
                return;

            try
            {
                button.IsEnabled = false;
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
                    $"{ApiConfig.BaseUrl}/api/Productos/{producto.IdProducto}";

                var response = await client.DeleteAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync(
                        "Publicación eliminada",
                        $"La publicación \"{producto.Nombre}\" se eliminó correctamente.",
                        "OK");

                    await CargarMisPublicaciones();
                }
                else
                {
                    string mensaje =
                        await response.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error",
                        $"No se pudo eliminar la publicación.\n\n{mensaje}",
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
            finally
            {
                button.IsEnabled = true;
            }
        }
    }
}


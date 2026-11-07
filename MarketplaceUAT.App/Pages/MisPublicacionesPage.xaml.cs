using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using MarketplaceUAT.App.Services;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace MarketplaceUAT.App.Pages
{
    public partial class MisPublicacionesPage : ContentPage
    {
        private ApiClient Api =>
        Application.Current!.Handler.MauiContext!.Services
        .GetRequiredService<ApiClient>();
        private class CompradorDisponibleDto
        {
            public int IdUsuario { get; set; }
            public string Nombre { get; set; } = string.Empty;
        }

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

                var client = Api.Client;

                string url =
                    $"{ApiConfig.BaseUrl}/api/Productos";

                var response = await client.GetAsync(url);

                if (await ManejarSesionExpiradaAsync(response))
                    return;

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

                        if (await ManejarSesionExpiradaAsync(imagenesResponse))
                            return;

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
                string url =
    $"{ApiConfig.BaseUrl}/api/Productos/{producto.IdProducto}";

                using var response = await Api.EnviarAutenticadoAsync(
                    () => new HttpRequestMessage(HttpMethod.Delete, url));

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

        private async void OnRegistrarVentaClicked(
            object sender,
            EventArgs e)
        {
            if (sender is not Button button ||
                button.BindingContext is not Producto producto)
            {
                return;
            }

            if (!SesionUsuario.EstaLogueado ||
                string.IsNullOrWhiteSpace(SesionUsuario.Token) ||
                SesionUsuario.UsuarioActual == null)
            {
                await DisplayAlertAsync(
                    "Sesión requerida",
                    "Inicia sesión para registrar una venta.",
                    "OK");
                return;
            }

            if (producto.IdUsuario != SesionUsuario.UsuarioActual.IdUsuario)
            {
                await DisplayAlertAsync(
                    "Acceso denegado",
                    "Solo el propietario puede registrar la venta.",
                    "OK");
                return;
            }

            if (!producto.Activo || producto.Estado == "Vendido")
            {
                await DisplayAlertAsync(
                    "Producto no disponible",
                    "Esta publicación ya no está disponible.",
                    "OK");

                await CargarMisPublicaciones();
                return;
            }

            button.IsEnabled = false;

            try
            {

                string compradoresUrl =
    $"{ApiConfig.BaseUrl.TrimEnd('/')}/api/Transacciones/Compradores/{producto.IdProducto}";

                using var respuestaCompradores =
                    await Api.EnviarAutenticadoAsync(
                        () => new HttpRequestMessage(HttpMethod.Get, compradoresUrl));

                if (!respuestaCompradores.IsSuccessStatusCode)
                {
                    string detalle =
                        await respuestaCompradores.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error",
                        $"No se pudieron consultar los compradores.\n" +
                        $"HTTP {(int)respuestaCompradores.StatusCode}\n{detalle}",
                        "OK");

                    return;
                }

                var compradores =
                    await respuestaCompradores.Content
                        .ReadFromJsonAsync<List<CompradorDisponibleDto>>(
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                if (compradores == null || compradores.Count == 0)
                {
                    await DisplayAlertAsync(
                        "Sin compradores",
                        "Todavía no hay usuarios con conversaciones sobre esta publicación.",
                        "OK");
                    return;
                }

                var opciones = compradores
                    .Select(c => $"{c.Nombre} (ID: {c.IdUsuario})")
                    .ToArray();

                string seleccion = await DisplayActionSheetAsync(
                    "¿Quién compró el producto?",
                    "Cancelar",
                    null,
                    opciones);

                if (string.IsNullOrWhiteSpace(seleccion) ||
                    seleccion == "Cancelar")
                {
                    return;
                }

                int indice = Array.IndexOf(opciones, seleccion);

                if (indice < 0)
                    return;

                var comprador = compradores[indice];

                bool confirmar = await DisplayAlertAsync(
                    "Confirmar venta",
                    $"¿Confirmas que vendiste \"{producto.Nombre}\" " +
                    $"a {comprador.Nombre} por ${producto.Precio:N2}?",
                    "Sí, registrar",
                    "Cancelar");

                if (!confirmar)
                    return;

                var datosVenta = new
                {
                    IdProducto = producto.IdProducto,
                    IdUsuarioComprador = comprador.IdUsuario
                };

                string ventaUrl =
    $"{ApiConfig.BaseUrl.TrimEnd('/')}/api/Transacciones";

                using var respuestaVenta = await Api.EnviarAutenticadoAsync(
                    () => new HttpRequestMessage(HttpMethod.Post, ventaUrl)
                    {
                        Content = JsonContent.Create(datosVenta)
                    });

                if (!respuestaVenta.IsSuccessStatusCode)
                {
                    string detalle =
                        await respuestaVenta.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "No se pudo registrar",
                        $"La API rechazó la venta.\n" +
                        $"HTTP {(int)respuestaVenta.StatusCode}\n{detalle}",
                        "OK");

                    return;
                }

                await DisplayAlertAsync(
                    "Venta registrada",
                    "La venta se registró correctamente. La publicación dejará de estar disponible y aparecerá en tu historial de ventas.",
                    "OK");

                await CargarMisPublicaciones();
            }
            catch (TaskCanceledException)
            {
                await DisplayAlertAsync(
                    "Tiempo de espera agotado",
                    "La solicitud tardó demasiado. Comprueba que la API esté ejecutándose.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudo registrar la venta.\n{ex.Message}",
                    "OK");
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private async Task<bool> ManejarSesionExpiradaAsync(
    HttpResponseMessage response)
        {
            if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
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
    }
}


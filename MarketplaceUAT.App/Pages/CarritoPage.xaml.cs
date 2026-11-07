using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using MarketplaceUAT.App.Services;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace MarketplaceUAT.App.Pages
{
    public partial class CarritoPage : ContentPage
    {
        private List<CarritoItem> ProductosCarrito = new();

        private ObservableCollection<GrupoCarritoVendedor> GruposCarrito = new();

        private int? _idVendedorVisible;

        private readonly JsonSerializerOptions _opcionesJson =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly ApiClient _apiClient;

        public CarritoPage(ApiClient apiClient)
        {
            InitializeComponent();

            _apiClient = apiClient;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await CargarCarrito();
        }

        private async Task CargarCarrito()
        {
            try
            {
                if (!SesionUsuario.EstaLogueado)
                {
                    await DisplayAlertAsync(
                        "Iniciar sesión",
                        "Debes iniciar sesión para ver tu carrito.",
                        "OK");

                    await Shell.Current.GoToAsync(
                        nameof(RequiereCuentaPage));

                    return;
                }

                string url = $"{ApiConfig.BaseUrl}/api/Carrito";

                using var response = await _apiClient.EnviarAutenticadoAsync(() =>
                new HttpRequestMessage(HttpMethod.Get, url));

                if (await ManejarSesionExpiradaAsync(response))
                    return;

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync(
                        "Carrito",
                        "No se pudieron obtener los productos del carrito.",
                        "OK");

                    return;
                }

                string json =
                    await response.Content.ReadAsStringAsync();

                var productos = JsonSerializer.Deserialize<List<CarritoItem>>(
                    json,
                    _opcionesJson);

                if (productos == null)
                    return;

                foreach (var producto in productos)
                {
                    producto.ImagenUrl = string.Empty;

                    string imagenUrl =
                        $"{ApiConfig.BaseUrl}/api/ImagenesProducto/Producto/{producto.IdProducto}";

                    using var imagenResponse =
                    await _apiClient.EnviarAutenticadoAsync(() =>
                    new HttpRequestMessage(HttpMethod.Get, imagenUrl));

                    if (await ManejarSesionExpiradaAsync(imagenResponse))
                        return;

                    if (!imagenResponse.IsSuccessStatusCode)
                        continue;

                    string imagenJson =
                        await imagenResponse.Content.ReadAsStringAsync();

                    var imagenes =
                        JsonSerializer.Deserialize<List<ImagenProducto>>(
                            imagenJson,
                            _opcionesJson);

                    var imagenPrincipal = imagenes?
                        .FirstOrDefault(i => i.EsPrincipal && i.Activo);

                    if (imagenPrincipal != null &&
                        !string.IsNullOrWhiteSpace(
                            imagenPrincipal.UrlImagen))
                    {
                        producto.ImagenUrl =
                            $"{ApiConfig.BaseUrl}/Images/{imagenPrincipal.UrlImagen}";
                    }
                }

                ProductosCarrito = productos;

                GruposCarrito = new ObservableCollection<GrupoCarritoVendedor>(
                ProductosCarrito
                .GroupBy(p => p.IdUsuarioVendedor)
                .Select(grupo => new GrupoCarritoVendedor
                {
                IdUsuarioVendedor = grupo.Key,
                NombreVendedor = grupo
                .Select(p => p.NombreVendedor)
                .FirstOrDefault(nombre => !string.IsNullOrWhiteSpace(nombre))
                ?? $"Vendedor #{grupo.Key}",
                Productos = new ObservableCollection<CarritoItem>(
                grupo.ToList())
                })
                     .ToList()
                );

                bool hayProductos = ProductosCarrito.Count > 0;

                CarritoCollection.ItemsSource = GruposCarrito;

                CarritoCollection.IsVisible = hayProductos;
                SinCarritoLayout.IsVisible = !hayProductos;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"Ocurrió un problema al cargar el carrito:\n{ex.Message}",
                    "OK");
            }
        }

        private async Task ActualizarCarritoConPosicionAsync()
        {
            // Guardar el vendedor que estaba visible antes de actualizar.
            int? idVendedorAnterior = _idVendedorVisible;

            await CargarCarrito();

            // Si no había un grupo visible registrado, no podemos restaurarlo.
            if (idVendedorAnterior == null)
                return;

            var grupo = GruposCarrito.FirstOrDefault(
                g => g.IdUsuarioVendedor == idVendedorAnterior.Value);

            if (grupo != null)
            {
                CarritoCollection.ScrollTo(
                    grupo,
                    position: ScrollToPosition.Start,
                    animate: false);
            }
        }

        private async Task<bool> ManejarSesionExpiradaAsync(
            HttpResponseMessage response)
        {
            if (response.StatusCode !=
                System.Net.HttpStatusCode.Unauthorized)
            {
                return false;
            }

            await SesionUsuario.CerrarSesionAsync();

            await DisplayAlertAsync(
                "Sesión expirada",
                "Tu sesión ya no es válida. Inicia sesión nuevamente.",
                "OK");

            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync(
                    nameof(LoginPage));
            }

            return true;
        }

        private async void OnVerPublicacionClicked(
        object sender, EventArgs e)
        {
            if (sender is not Button boton ||
                boton.CommandParameter is not CarritoItem producto)
                return;

            await Shell.Current.GoToAsync(
                $"{nameof(ProductoDetallePage)}?idProducto={producto.IdProducto}");
        }

        private async void OnQuitarProductoClicked(
        object sender, EventArgs e)
        {
            if (sender is not Button boton ||
                boton.CommandParameter is not CarritoItem producto)
                return;

            bool confirmar = await DisplayAlertAsync(
                "Quitar producto",
                $"¿Quieres quitar \"{producto.Nombre}\" de tu carrito?",
                "Quitar",
                "Cancelar");

            if (!confirmar)
                return;

            try
            {
                string url =
    $"{ApiConfig.BaseUrl}/api/Carrito/{producto.IdCarrito}";

                using var respuesta =
                    await _apiClient.EnviarAutenticadoAsync(() =>
                        new HttpRequestMessage(HttpMethod.Delete, url));

                if (await ManejarSesionExpiradaAsync(respuesta))
                    return;

                if (!respuesta.IsSuccessStatusCode)
                {
                    string error =
                        await respuesta.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error",
                        $"No se pudo quitar el producto.\n{error}",
                        "OK");

                    return;
                }

                // Buscar el grupo al que pertenece el producto.
                var grupo = GruposCarrito.FirstOrDefault(
                    g => g.IdUsuarioVendedor == producto.IdUsuarioVendedor);

                // Eliminar el producto de la colección visible.
                if (grupo != null)
                {
                    grupo.Productos.Remove(producto);

                    // Si ya no tiene productos, eliminar también el grupo.
                    if (grupo.Productos.Count == 0)
                    {
                        GruposCarrito.Remove(grupo);
                    }
                }

                // Actualizar la lista interna.
                ProductosCarrito.Remove(producto);

                // Actualizar la visibilidad del carrito.
                bool hayProductos = ProductosCarrito.Count > 0;

                CarritoCollection.IsVisible = hayProductos;
                SinCarritoLayout.IsVisible = !hayProductos;

                await DisplayAlertAsync(
                    "Carrito actualizado",
                    "El producto se quitó de tu carrito.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"Ocurrió un problema al quitar el producto:\n{ex.Message}",
                    "OK");
            }
        }

        private async void OnDisminuirCantidadClicked(
        object sender, EventArgs e)
        {
            if (sender is not Button boton ||
                boton.CommandParameter is not CarritoItem producto)
                return;

            // No permitir cantidades inferiores a 1.
            if (producto.Cantidad <= 1)
                return;

            // Si el producto ya no está disponible, permitir
            // reducir la cantidad para facilitar su corrección.
            await ActualizarCantidadCarrito(
                producto,
                producto.Cantidad - 1);
        }

        private async void OnAumentarCantidadClicked(
    object sender, EventArgs e)
        {
            if (sender is not Button boton ||
                boton.CommandParameter is not CarritoItem producto)
                return;

            // No permitir modificar productos que ya no están disponibles.
            if (producto.EstaNoDisponible)
            {
                await DisplayAlertAsync(
                    "Producto no disponible",
                    producto.MensajeDisponibilidad,
                    "OK");

                return;
            }

            // Los productos únicos siempre tienen cantidad 1.
            if (producto.EsProductoUnico)
                return;

            // No superar las existencias actuales.
            if (producto.Cantidad >= producto.CantidadDisponible)
            {
                await DisplayAlertAsync(
                    "Existencias insuficientes",
                    $"Solo hay {producto.CantidadDisponible} unidades disponibles.",
                    "OK");

                return;
            }

            await ActualizarCantidadCarrito(
                producto,
                producto.Cantidad + 1);
        }

        private async Task ActualizarCantidadCarrito(
        CarritoItem producto, int nuevaCantidad)
        {
            try
            {
                string url =
                $"{ApiConfig.BaseUrl}/api/Carrito/{producto.IdCarrito}/cantidad" +
                $"?cantidad={nuevaCantidad}";

                using var respuesta =
                    await _apiClient.EnviarAutenticadoAsync(() =>
                        new HttpRequestMessage(HttpMethod.Put, url));

                if (await ManejarSesionExpiradaAsync(respuesta))
                    return;

                if (!respuesta.IsSuccessStatusCode)
                {
                    string error = await respuesta.Content.ReadAsStringAsync();

                    try
                    {
                        await ActualizarCarritoConPosicionAsync();
                    }
                    catch
                    {
                        // Conservamos el error original del servidor.
                    }

                    await DisplayAlertAsync(
                        "No se pudo actualizar",
                        $"El servidor no permitió cambiar la cantidad.\n\nDetalle: {error}",
                        "OK");

                    return;
                }

                await ActualizarCarritoConPosicionAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudo actualizar la cantidad:\n{ex.Message}",
                    "OK");
            }
        }

        private void OnCarritoScrolled(
        object sender,
        ItemsViewScrolledEventArgs e)
        {
            if (e.FirstVisibleItemIndex < 0 ||
                e.FirstVisibleItemIndex >= GruposCarrito.Count)
            {
                return;
            }

            _idVendedorVisible =
                GruposCarrito[e.FirstVisibleItemIndex].IdUsuarioVendedor;
        }
    }
}

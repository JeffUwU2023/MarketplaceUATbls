using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using System.Net;
using System.Text.Json;
using MarketplaceUAT.App.Services;

namespace MarketplaceUAT.App.Pages
{
    public partial class FavoritosPage : ContentPage
    {
        private ApiClient Api =>
        Application.Current!.Handler.MauiContext!.Services
        .GetRequiredService<ApiClient>();
        private List<FavoritoProducto> TodosLosFavoritos = new();
        private string OrdenSeleccionado = "Más recientes";
        public FavoritosPage()
        {
            InitializeComponent();
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await CargarFavoritos();
        }
        private async Task CargarFavoritos()
        {
            try
            {
                if (!SesionUsuario.EstaLogueado)
                {
                    await DisplayAlertAsync(
                        "Iniciar sesión",
                        "Debes iniciar sesión para ver tus favoritos.",
                        "OK");

                    return;
                }

                string url = $"{ApiConfig.BaseUrl}/api/Favoritos";

                using var response = await Api.EnviarAutenticadoAsync(() =>
                    new HttpRequestMessage(HttpMethod.Get, url));

                if (await ManejarSesionExpiradaAsync((response)))
                    return;

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync(
                        "Favoritos",
                        "No se pudieron obtener los favoritos.",
                        "OK");

                    return;
                }

                string json = await response.Content.ReadAsStringAsync();

                var favoritos = JsonSerializer.Deserialize<List<Favorito>>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (favoritos == null)
                {
                    return;
                }

                var favoritosUsuario = favoritos;

                var productosFavoritos = new List<FavoritoProducto>();

                foreach (var favorito in favoritosUsuario)
                {
                    string productoUrl =
                         $"{ApiConfig.BaseUrl}/api/Productos/DetalleFavorito/{favorito.IdProducto}";

                    using var productoResponse =
                    await Api.EnviarAutenticadoAsync(() =>
                    new HttpRequestMessage(HttpMethod.Get, productoUrl));

                    if (!productoResponse.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    string productoJson =
                        await productoResponse.Content.ReadAsStringAsync();

                    var producto = JsonSerializer.Deserialize<Producto>(
                        productoJson,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (producto != null)
                    {
                        string imagenUrl =
                            $"{ApiConfig.BaseUrl}/api/ImagenesProducto/Producto/{producto.IdProducto}";

                        using var imagenResponse =
    await Api.EnviarAutenticadoAsync(() =>
        new HttpRequestMessage(HttpMethod.Get, imagenUrl));

                        if (imagenResponse.IsSuccessStatusCode)
                        {
                            string imagenJson =
                                await imagenResponse.Content.ReadAsStringAsync();

                            var imagenes = JsonSerializer.Deserialize<List<ImagenProducto>>(
                                imagenJson,
                                new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });

                            var imagenPrincipal = imagenes?
                                .FirstOrDefault(i => i.EsPrincipal && i.Activo);

                            if (imagenPrincipal != null)
                            {
                                producto.ImagenUrl =
                                    $"{ApiConfig.BaseUrl}/Images/{imagenPrincipal.UrlImagen}";
                            }
                        }

                        System.Diagnostics.Debug.WriteLine(
    $"Favorito: {producto.Nombre} | Fecha: {favorito.FechaAgregado:yyyy-MM-dd HH:mm:ss}");

                        productosFavoritos.Add(new FavoritoProducto
                        {
                            IdFavorito = favorito.IdFavorito,
                            IdProducto = producto.IdProducto,
                            FechaAgregado = favorito.FechaAgregado,
                            Nombre = producto.Nombre,
                            Precio = producto.Precio,
                            Condicion = producto.Condicion,
                            Estado = producto.Estado,
                            Activo = producto.Activo,
                            ImagenUrl = producto.ImagenUrl
                        });
                    }
                }
                TodosLosFavoritos = productosFavoritos;

                BuscarFavoritoEntry.IsVisible = TodosLosFavoritos.Count > 0;
                OrdenarFavoritosPicker.IsVisible = TodosLosFavoritos.Count > 0;

                AplicarFiltros();

                if (productosFavoritos.Count == 0)
                {
                    FavoritosCollection.IsVisible = false;
                    SinFavoritosLayout.IsVisible = true;
                }
                else
                {
                    FavoritosCollection.IsVisible = true;
                    SinFavoritosLayout.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    ex.Message,
                    "OK");
            }
        }
        private void OnBuscarFavoritoTextChanged(
    object sender,
    TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void OnOrdenarFavoritosChanged(
            object sender,
            EventArgs e)
        {
            if (OrdenarFavoritosPicker.SelectedItem is string orden)
            {
                OrdenSeleccionado = orden;
                AplicarFiltros();
            }
        }

        private void AplicarFiltros()
        {
            string texto = BuscarFavoritoEntry.Text?.Trim() ?? string.Empty;

            var favoritos = TodosLosFavoritos.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(texto))
            {
                favoritos = favoritos.Where(f =>
                    f.Nombre.Contains(
                        texto,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (OrdenSeleccionado == "Más antiguos")
            {
                favoritos = favoritos.OrderBy(f => f.FechaAgregado);
            }
            else
            {
                favoritos = favoritos.OrderByDescending(f => f.FechaAgregado);
            }

            var resultado = favoritos.ToList();

            FavoritosCollection.ItemsSource = resultado;

            if (TodosLosFavoritos.Count == 0)
            {
                FavoritosCollection.IsVisible = false;
                SinFavoritosLayout.IsVisible = true;
            }
            else
            {
                FavoritosCollection.IsVisible = resultado.Count > 0;
                SinFavoritosLayout.IsVisible = false;
            }
        }
        private async void OnFavoritoTapped(object sender, TappedEventArgs e)
        {
            if (sender is Border border &&
                border.BindingContext is FavoritoProducto favorito)
            {
                await Shell.Current.GoToAsync(
                    $"{nameof(ProductoDetallePage)}?idProducto={favorito.IdProducto}");
            }
        }

        private async void OnEliminarFavoritoClicked(object sender, EventArgs e)
        {
            try
            {
                if (!SesionUsuario.EstaLogueado)
                {
                    await Shell.Current.GoToAsync(nameof(RequiereCuentaPage));
                    return;
                }

                if (sender is not Button button ||
                    button.BindingContext is not FavoritoProducto favorito)
                {
                    return;
                }

                bool confirmar = await DisplayAlertAsync(
                    "Eliminar favorito",
                    "¿Quieres eliminar este producto de tus favoritos?",
                    "Sí",
                    "No");

                if (!confirmar)
                {
                    return;
                }

                string url =
    $"{ApiConfig.BaseUrl}/api/Favoritos/{favorito.IdFavorito}";

                using var response = await Api.EnviarAutenticadoAsync(() =>
                    new HttpRequestMessage(HttpMethod.Delete, url));

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync(
                        "Favoritos",
                        "El producto se eliminó de tus favoritos.",
                        "OK");

                    await CargarFavoritos();
                }
                else
                {
                    string mensaje = await response.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Favoritos",
                        $"No se pudo eliminar el favorito.\n\n{mensaje}",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    ex.Message,
                    "OK");
            }
        }
        private async void OnExplorarProductosClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
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
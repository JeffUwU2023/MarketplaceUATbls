using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using MarketplaceUAT.App.Services;
using System.Text.Json;

namespace MarketplaceUAT.App.Pages
{
    public partial class MainPage : ContentPage
    {
        private const string BusquedasRecientesKey = "BusquedasRecientes";
        private List<string> BusquedasRecientes = new();
        private List<Producto> TodosLosProductos = new();
        private int? CategoriaSeleccionada = null;
        private string? CondicionSeleccionada = null;
        private decimal? PrecioMinimo = null;
        private decimal? PrecioMaximo = null;
        private int? CategoriaPendiente = null;
        private string? CondicionPendiente = null;
        private decimal? PrecioMinimoPendiente = null;
        private decimal? PrecioMaximoPendiente = null;
        private string OrdenSeleccionado = "Más recientes";
        private bool BusquedaEjecutada = false;
        public MainPage()
        {
            InitializeComponent();
            CargarBusquedasRecientes();
            ActualizarSesion();
            ActualizarBotonConversaciones();

        }

        private void ActualizarBotonConversaciones()
        {
            ConversacionesButton.IsVisible =
                SesionUsuario.EstaLogueado;
        }
        private void CargarBusquedasRecientes()
        {
            string datosGuardados =
                Preferences.Default.Get(
                    BusquedasRecientesKey,
                    string.Empty);

            if (string.IsNullOrWhiteSpace(datosGuardados))
            {
                return;
            }

            BusquedasRecientes =
                datosGuardados
                    .Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            ActualizarBotonConversaciones();

            ActualizarSesion();

            await CargarProductos();

            if (string.IsNullOrWhiteSpace(CondicionSeleccionada))
            {
                CondicionPicker.SelectedItem = "Todas";
            }
            else
            {
                CondicionPicker.SelectedItem = CondicionSeleccionada;
            }

            AplicarFiltros();
        }

        private async Task CargarProductos()
        {
            try
            {
                var api = new ApiClient();
                var client = api.Client;

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

                var productos = JsonSerializer.Deserialize<List<Producto>>(
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
                    try
                    {
                        string imagenesUrl =
                             $"{ApiConfig.BaseUrl}/api/ImagenesProducto/Producto/{producto.IdProducto}";

                        var imagenesResponse = await client.GetAsync(imagenesUrl);

                        if (imagenesResponse.IsSuccessStatusCode)
                        {
                            string imagenesJson =
                                await imagenesResponse.Content.ReadAsStringAsync();

                            var imagenes = JsonSerializer.Deserialize<List<ImagenProducto>>(
                                imagenesJson,
                                new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });

                            var imagenPrincipal = imagenes?
                                .FirstOrDefault(i => i.EsPrincipal);

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

                    }
                }

                TodosLosProductos = productos;
                ProductosCollection.ItemsSource = productos;
            }


            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error de conexión",
                    ex.Message,
                    "OK");
            }
        }
        private async void OnAbrirLoginClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(LoginPage));
        }

        private async void OnSesionButtonClicked(object sender, EventArgs e)
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

        private async void OnAbrirFavoritosClicked(object sender, EventArgs e)
        {
            if (!SesionUsuario.EstaLogueado)
            {
                await Shell.Current.GoToAsync(nameof(RequiereCuentaPage));
                return;
            }

            await Shell.Current.GoToAsync(nameof(FavoritosPage));
        }

        private async void OnAbrirPublicarProductoClicked(object sender, EventArgs e)
        {
            if (!SesionUsuario.EstaLogueado)
            {
                await Shell.Current.GoToAsync(nameof(RequiereCuentaPage));
                return;
            }

            await Shell.Current.GoToAsync(nameof(PublicarProductoPage));
        }

        private void ActualizarSesion()
        {
            if (SesionUsuario.EstaLogueado)
            {
                var usuario = SesionUsuario.UsuarioActual!;

                SesionButton.Text =
                    $"👤 {usuario.Nombres} {usuario.Apellidos} - Cerrar sesión";

                // Mostrar el avatar cuando hay una sesión activa.
                PerfilAvatarBorder.IsVisible = true;

                // El botón comparte el espacio con el avatar.
                SesionButton.HorizontalOptions = LayoutOptions.Fill;
                SesionButton.WidthRequest = -1;

                if (!string.IsNullOrWhiteSpace(usuario.UrlFotoPerfil))
                {
                    string rutaFoto = usuario.UrlFotoPerfil.TrimStart('/');

                    string urlFoto =
                        $"{ApiConfig.BaseUrl.TrimEnd('/')}/{rutaFoto}";

                    PerfilFotoImage.Source = new UriImageSource
                    {
                        Uri = new Uri(urlFoto),
                        CachingEnabled = false
                    };

                    PerfilFotoImage.IsVisible = true;
                    PerfilPlaceholder.IsVisible = false;
                }
                else
                {
                    PerfilFotoImage.Source = null;
                    PerfilFotoImage.IsVisible = false;
                    PerfilPlaceholder.IsVisible = true;
                }
            }
            else
            {
                SesionButton.Text = "Iniciar sesión";

                // Ocultar el avatar cuando no hay sesión.
                PerfilAvatarBorder.IsVisible = false;

                // El botón ocupa toda la fila.
                SesionButton.HorizontalOptions = LayoutOptions.Fill;
                SesionButton.WidthRequest = -1;

                PerfilFotoImage.Source = null;
                PerfilFotoImage.IsVisible = false;
                PerfilPlaceholder.IsVisible = true;
            }
        }

        private async void OnProductoTapped(object sender, TappedEventArgs e)
        {
            if (sender is Border border &&
                border.BindingContext is MarketplaceUAT.App.Models.Producto producto)
            {
                await Shell.Current.GoToAsync(
                    $"{nameof(ProductoDetallePage)}?idProducto={producto.IdProducto}");
            }
        }

        private async void OnCalculadoraTapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(ProductoDetallePage)}?idProducto=1");
        }
        private void OnBuscarCompleted(object sender, EventArgs e)
        {
            string textoBusqueda = BuscarEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(textoBusqueda))
            {
                return;
            }

            BusquedasRecientes.Remove(textoBusqueda);
            BusquedasRecientes.Insert(0, textoBusqueda);

            if (BusquedasRecientes.Count > 5)
            {
                BusquedasRecientes.RemoveAt(
                    BusquedasRecientes.Count - 1);
            }

            Preferences.Default.Set(
            BusquedasRecientesKey,
            string.Join("|", BusquedasRecientes));

            BusquedaEjecutada = true;
            BuscarEntry.Unfocus();

            SugerenciasCollection.IsVisible = false;

            AplicarFiltros();

            BuscarEntry.Text = string.Empty;
        }

        private void OnBuscarTextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = BuscarEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(texto))
            {
                SugerenciasCollection.IsVisible = false;
                return;
            }

            var sugerencias = TodosLosProductos
                .Where(p =>
                    !string.IsNullOrWhiteSpace(p.Nombre) &&
                    p.Nombre.Contains(
                        texto,
                        StringComparison.OrdinalIgnoreCase))
                .GroupBy(
                    p => p.Nombre,
                    StringComparer.OrdinalIgnoreCase)
                .Select(grupo => grupo.First())
                .OrderByDescending(p =>
                    p.Nombre.StartsWith(
                        texto,
                        StringComparison.OrdinalIgnoreCase))
                .ThenBy(p => p.Nombre)
                .Take(5)
                .ToList();

            SugerenciasCollection.ItemsSource = sugerencias;

            SugerenciasCollection.IsVisible =
                sugerencias.Count > 0;
        }
        private void OnBuscarFocused(object sender, FocusEventArgs e)
        {
            if (BusquedaEjecutada)
            {
                SugerenciasCollection.IsVisible = false;
                BusquedaEjecutada = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(BuscarEntry.Text) &&
                BusquedasRecientes.Count > 0)
            {
                var sugerenciasRecientes = BusquedasRecientes
                    .Select(busqueda => new Producto
                    {
                        Nombre = busqueda
                    })
                    .ToList();

                SugerenciasCollection.ItemsSource = sugerenciasRecientes;
                SugerenciasCollection.IsVisible = true;
            }
        }
        private void OnSugerenciaSeleccionada(
        object sender,
        SelectionChangedEventArgs e)
        {
            var seleccion = e.CurrentSelection.FirstOrDefault();

            if (seleccion is Producto producto)
            {
                BuscarEntry.Text = producto.Nombre;
            }
            else if (seleccion is string busqueda)
            {
                BuscarEntry.Text = busqueda;
            }
            else
            {
                return;
            }

            string textoBusqueda = BuscarEntry.Text?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(textoBusqueda))
            {
                BusquedasRecientes.Remove(textoBusqueda);
                BusquedasRecientes.Insert(0, textoBusqueda);

                if (BusquedasRecientes.Count > 5)
                {
                    BusquedasRecientes.RemoveAt(
                        BusquedasRecientes.Count - 1);
                }
            }

            Preferences.Default.Set(
            BusquedasRecientesKey,
            string.Join("|", BusquedasRecientes));

            BuscarEntry.Unfocus();

            SugerenciasCollection.IsVisible = false;

            AplicarFiltros();

            BuscarEntry.Text = string.Empty;

            SugerenciasCollection.SelectedItem = null;
        }
        private void OnCategoriaClicked(object sender, EventArgs e)
        {
            if (sender is not Button button)
                return;

            if (!int.TryParse(button.CommandParameter?.ToString(), out int idCategoria))
                return;

            CategoriaPendiente = idCategoria;
            CategoriaSeleccionada = CategoriaPendiente;

            AplicarFiltros();
        }

        private void OnCondicionChanged(object sender, EventArgs e)
        {
            if (CondicionPicker.SelectedItem is string condicion)
            {
                CondicionPendiente = condicion == "Todas"
                    ? null
                    : condicion;
            }
        }

        private void OnPrecioChanged(object sender, TextChangedEventArgs e)
        {
            PrecioMinimoPendiente = null;
            PrecioMaximoPendiente = null;

            if (decimal.TryParse(PrecioMinimoEntry.Text, out decimal minimo))
            {
                PrecioMinimoPendiente = minimo;
            }

            if (decimal.TryParse(PrecioMaximoEntry.Text, out decimal maximo))
            {
                PrecioMaximoPendiente = maximo;
            }
        }

        private void OnOrdenarChanged(object sender, EventArgs e)
        {
            if (OrdenarPicker.SelectedItem is string orden)
            {
                OrdenSeleccionado = orden;

                AplicarFiltros();
            }
        }

        private void OnAplicarFiltrosClicked(object sender, EventArgs e)
        {
            CategoriaSeleccionada = CategoriaPendiente;
            CondicionSeleccionada = CondicionPendiente;
            PrecioMinimo = PrecioMinimoPendiente;
            PrecioMaximo = PrecioMaximoPendiente;

            AplicarFiltros();
        }

        private void OnLimpiarFiltrosClicked(object sender, EventArgs e)
        {
            BuscarEntry.Text = string.Empty;

            CategoriaSeleccionada = null;
            CondicionSeleccionada = null;
            PrecioMinimo = null;
            PrecioMaximo = null;

            CategoriaPendiente = null;
            CondicionPendiente = null;
            PrecioMinimoPendiente = null;
            PrecioMaximoPendiente = null;

            CondicionPicker.SelectedItem = "Todas";

            PrecioMinimoEntry.Text = string.Empty;
            PrecioMaximoEntry.Text = string.Empty;

            OrdenSeleccionado = "Más recientes";
            OrdenarPicker.SelectedItem = "Más recientes";

            AplicarFiltros();
        }
        private void OnTodasCategoriasClicked(object sender, EventArgs e)
        {
            CategoriaPendiente = null;
            CategoriaSeleccionada = null;

            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            string texto = BuscarEntry.Text?.Trim() ?? string.Empty;

            var productosFiltrados = TodosLosProductos.AsEnumerable();

            if (CategoriaSeleccionada.HasValue)
            {
                productosFiltrados = productosFiltrados.Where(
                    p => p.IdCategoria == CategoriaSeleccionada.Value);
            }

            if (!string.IsNullOrWhiteSpace(CondicionSeleccionada))
            {
                productosFiltrados = productosFiltrados.Where(
                    p => p.Condicion.Equals(
                        CondicionSeleccionada,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (PrecioMinimo.HasValue)
            {
                productosFiltrados = productosFiltrados.Where(
                    p => p.Precio >= PrecioMinimo.Value);
            }

            if (PrecioMaximo.HasValue)
            {
                productosFiltrados = productosFiltrados.Where(
                    p => p.Precio <= PrecioMaximo.Value);
            }

            if (!string.IsNullOrWhiteSpace(texto))
            {
                productosFiltrados = productosFiltrados.Where(p =>
                    p.Nombre.Contains(
                        texto,
                        StringComparison.OrdinalIgnoreCase) ||
                    (p.Descripcion != null &&
                     p.Descripcion.Contains(
                         texto,
                         StringComparison.OrdinalIgnoreCase)));
            }

            var resultado = productosFiltrados.ToList();

            switch (OrdenSeleccionado)
            {
                case "Más recientes":
                    resultado = resultado
                        .OrderByDescending(p => p.FechaPublicacion)
                        .ToList();
                    break;

                case "Más antiguos":
                    resultado = resultado
                        .OrderBy(p => p.FechaPublicacion)
                        .ToList();
                    break;

                case "Precio: menor a mayor":
                    resultado = resultado
                        .OrderBy(p => p.Precio)
                        .ToList();
                    break;

                case "Precio: mayor a menor":
                    resultado = resultado
                        .OrderByDescending(p => p.Precio)
                        .ToList();
                    break;

                case "Nombre: A-Z":
                    resultado = resultado
                        .OrderBy(p => p.Nombre)
                        .ToList();
                    break;

                case "Nombre: Z-A":
                    resultado = resultado
                        .OrderByDescending(p => p.Nombre)
                        .ToList();
                    break;
            }

            ProductosCollection.ItemsSource = resultado;

            SinResultadosLabel.IsVisible = resultado.Count == 0;
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
        private async void OnConversacionesClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ConversacionesPage));
        }
        private void OnFiltrosClicked(object sender, EventArgs e)
        {
            FiltrosLayout.IsVisible = !FiltrosLayout.IsVisible;
        }
        private async void OnPerfilAvatarTapped(
            object sender,
            TappedEventArgs e)
        {
            if (!SesionUsuario.EstaLogueado)
            {
                return;
            }

            await Shell.Current.GoToAsync(nameof(PerfilPage));
        }

        private async void OnPerfilClicked(object sender, EventArgs e)
        {
            if (!SesionUsuario.EstaLogueado)
            {
                bool irALogin = await DisplayAlertAsync(
                    "Iniciar sesión",
                    "Necesitas iniciar sesión para consultar tu perfil.",
                    "Iniciar sesión",
                    "Cancelar");

                if (irALogin)
                {
                    await Shell.Current.GoToAsync(nameof(LoginPage));
                }

                return;
            }

            await Shell.Current.GoToAsync(nameof(PerfilPage));
        }
    }
}


using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using MarketplaceUAT.App.Services;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace MarketplaceUAT.App.Pages;

[QueryProperty(nameof(TextoBusqueda), "textoBusqueda")]
[QueryProperty(nameof(CategoriaIdTexto), "categoriaId")]
public partial class SearchPage : ContentPage
{
    private ApiClient Api =>
    Application.Current!.Handler.MauiContext!.Services
        .GetRequiredService<ApiClient>();
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

    private string textoBusqueda = string.Empty;
    public string CategoriaIdTexto
    {
        set
        {
            if (int.TryParse(value, out int idCategoria))
            {
                CategoriaPendiente = idCategoria;
                CategoriaSeleccionada = idCategoria;
            }
        }
    }

    public string TextoBusqueda
    {
        get => textoBusqueda;
        set
        {
            textoBusqueda = value ?? string.Empty;

            if (BuscarEntry != null)
            {
                BuscarEntry.Text = textoBusqueda;
            }
        }
    }

    public SearchPage()
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
            return;

        BusquedasRecientes = datosGuardados
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    private void GuardarBusquedaReciente(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return;

        BusquedasRecientes.RemoveAll(
            b => string.Equals(
                b,
                texto,
                StringComparison.OrdinalIgnoreCase));

        BusquedasRecientes.Insert(0, texto);

        if (BusquedasRecientes.Count > 5)
        {
            BusquedasRecientes.RemoveAt(
                BusquedasRecientes.Count - 1);
        }

        Preferences.Default.Set(
            BusquedasRecientesKey,
            string.Join("|", BusquedasRecientes));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        ActualizarBotonConversaciones();
        ActualizarSesion();

        await CargarProductos();

        if (CondicionPicker.Items.Contains("Todas"))
        {
            CondicionPicker.SelectedItem =
                string.IsNullOrWhiteSpace(CondicionSeleccionada)
                    ? "Todas"
                    : CondicionSeleccionada;
        }

        AplicarFiltros();

        // Si la página recibió una búsqueda, conserva ese texto.
        if (!string.IsNullOrWhiteSpace(TextoBusqueda))
        {
            BuscarEntry.Text = TextoBusqueda;
            AplicarFiltros();
        }
    }

    private async Task CargarProductos()
    {
        try
        {
            var client = Api.Client;
            Api.ConfigurarAutorizacion();

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

            string json =
                await response.Content.ReadAsStringAsync();

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
                    // Si falla la carga de una imagen,
                    // se conserva la imagen predeterminada.
                }
            }

            TodosLosProductos = productos;

            AplicarFiltros();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error de conexión",
                ex.Message,
                "OK");
        }
    }

    private void ActualizarSesion()
    {
        if (SesionUsuario.EstaLogueado)
        {
            var usuario = SesionUsuario.UsuarioActual!;

            SesionButton.Text =
                $"👤 {usuario.Nombres} {usuario.Apellidos} - Cerrar sesión";

            PerfilAvatarBorder.IsVisible = true;

            SesionButton.HorizontalOptions = LayoutOptions.Fill;
            SesionButton.WidthRequest = -1;

            if (!string.IsNullOrWhiteSpace(usuario.UrlFotoPerfil))
            {
                string rutaFoto =
                    usuario.UrlFotoPerfil.TrimStart('/');

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

            PerfilAvatarBorder.IsVisible = false;

            SesionButton.HorizontalOptions = LayoutOptions.Fill;
            SesionButton.WidthRequest = -1;

            PerfilFotoImage.Source = null;
            PerfilFotoImage.IsVisible = false;
            PerfilPlaceholder.IsVisible = true;
        }
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
                return;

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

        await Shell.Current.GoToAsync(nameof(FavoritosPage));
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

    private async void OnPerfilAvatarTapped(
        object sender,
        TappedEventArgs e)
    {
        if (!SesionUsuario.EstaLogueado)
            return;

        await Shell.Current.GoToAsync(nameof(PerfilPage));
    }

    private async void OnProductoTapped(
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

    private void OnBuscarCompleted(
        object sender,
        EventArgs e)
    {
        string texto = BuscarEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(texto))
            return;

        GuardarBusquedaReciente(texto);

        BusquedaEjecutada = true;

        BuscarEntry.Unfocus();
        SugerenciasCollection.IsVisible = false;
        SugerenciasCollection.SelectedItem = null;

        AplicarFiltros();
    }

    private void OnBuscarTextChanged(
        object sender,
        TextChangedEventArgs e)
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

    private void OnBuscarFocused(
        object sender,
        FocusEventArgs e)
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

            SugerenciasCollection.ItemsSource =
                sugerenciasRecientes;

            SugerenciasCollection.IsVisible = true;
        }
    }

    private void OnSugerenciaSeleccionada(
        object sender,
        SelectionChangedEventArgs e)
    {
        var seleccion = e.CurrentSelection.FirstOrDefault();

        if (seleccion is not Producto producto)
            return;

        string texto = producto.Nombre?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(texto))
            return;

        BuscarEntry.Text = texto;

        GuardarBusquedaReciente(texto);

        BusquedaEjecutada = true;

        BuscarEntry.Unfocus();
        SugerenciasCollection.IsVisible = false;
        SugerenciasCollection.SelectedItem = null;

        AplicarFiltros();
    }

    private void OnCategoriaClicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (!int.TryParse(
            button.CommandParameter?.ToString(),
            out int idCategoria))
        {
            return;
        }

        CategoriaPendiente = idCategoria;
        CategoriaSeleccionada = idCategoria;

        AplicarFiltros();
    }

    private void OnTodasCategoriasClicked(
        object sender,
        EventArgs e)
    {
        CategoriaPendiente = null;
        CategoriaSeleccionada = null;

        AplicarFiltros();
    }

    private void OnCondicionChanged(
        object sender,
        EventArgs e)
    {
        if (CondicionPicker.SelectedItem is string condicion)
        {
            CondicionPendiente = condicion == "Todas"
                ? null
                : condicion;
        }
    }

    private void OnPrecioChanged(
        object sender,
        TextChangedEventArgs e)
    {
        PrecioMinimoPendiente = null;
        PrecioMaximoPendiente = null;

        if (decimal.TryParse(
            PrecioMinimoEntry.Text,
            out decimal minimo))
        {
            PrecioMinimoPendiente = minimo;
        }

        if (decimal.TryParse(
            PrecioMaximoEntry.Text,
            out decimal maximo))
        {
            PrecioMaximoPendiente = maximo;
        }
    }

    private void OnOrdenarChanged(
        object sender,
        EventArgs e)
    {
        if (OrdenarPicker.SelectedItem is string orden)
        {
            OrdenSeleccionado = orden;
            AplicarFiltros();
        }
    }

    private void OnAplicarFiltrosClicked(
        object sender,
        EventArgs e)
    {
        CategoriaSeleccionada = CategoriaPendiente;
        CondicionSeleccionada = CondicionPendiente;
        PrecioMinimo = PrecioMinimoPendiente;
        PrecioMaximo = PrecioMaximoPendiente;

        AplicarFiltros();
    }

    private void OnLimpiarFiltrosClicked(
        object sender,
        EventArgs e)
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

        SugerenciasCollection.IsVisible = false;

        AplicarFiltros();
    }

    private void OnFiltrosClicked(
        object sender,
        EventArgs e)
    {
        FiltrosLayout.IsVisible =
            !FiltrosLayout.IsVisible;
    }

    private void AplicarFiltros()
    {
        string texto = BuscarEntry.Text?.Trim() ?? string.Empty;

        IEnumerable<Producto> productosFiltrados =
            TodosLosProductos;

        if (CategoriaSeleccionada.HasValue)
        {
            productosFiltrados = productosFiltrados.Where(
                p => p.IdCategoria == CategoriaSeleccionada.Value);
        }

        if (!string.IsNullOrWhiteSpace(CondicionSeleccionada))
        {
            productosFiltrados = productosFiltrados.Where(
                p => string.Equals(
                    p.Condicion,
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
                (!string.IsNullOrWhiteSpace(p.Nombre) &&
                 p.Nombre.Contains(
                     texto,
                     StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(p.Descripcion) &&
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

        SinResultadosLabel.IsVisible =
            resultado.Count == 0;
    }
}


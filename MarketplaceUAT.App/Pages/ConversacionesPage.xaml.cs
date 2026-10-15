using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using System.Text.Json;

namespace MarketplaceUAT.App.Pages;

public partial class ConversacionesPage : ContentPage
{
    private List<Conversacion> TodasLasConversaciones = new();
    private string OrdenSeleccionado = "Más recientes";
    private string BuscarPorSeleccionado = "Producto";

    public ConversacionesPage()
    {
        InitializeComponent();
        BuscarPorConversacionPicker.SelectedIndex = 0;
        OrdenarConversacionesPicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CargarConversaciones();
    }

    private async Task CargarConversaciones()
    {
        try
        {
            if (!SesionUsuario.EstaLogueado)
            {
                await DisplayAlertAsync(
                    "Cuenta requerida",
                    "Debes iniciar sesión para ver tus conversaciones.",
                    "OK");

                return;
            }

            int idUsuario =
                SesionUsuario.UsuarioActual!.IdUsuario;

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
                $"{ApiConfig.BaseUrl}/api/Conversaciones/Usuario/{idUsuario}";

            client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            SesionUsuario.Token);

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudieron cargar las conversaciones.\nCódigo: {(int)response.StatusCode}",
                    "OK");

                return;
            }

            string json =
                await response.Content.ReadAsStringAsync();

            var conversaciones =
                JsonSerializer.Deserialize<List<Conversacion>>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (conversaciones == null)
            {
                return;
            }

            TodasLasConversaciones = conversaciones;

            BuscarConversacionEntry.IsVisible =
                TodasLasConversaciones.Count > 0;

            OrdenarConversacionesPicker.IsVisible =
                TodasLasConversaciones.Count > 0;

            AplicarFiltros();

        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error de conexión",
                $"No se pudieron cargar las conversaciones.\n\n{ex.Message}",
                "OK");
        }
    }


    private void OnBuscarConversacionTextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        AplicarFiltros();
    }

    private void OnBuscarPorConversacionChanged(object sender, EventArgs e)
    {
        if (BuscarPorConversacionPicker.SelectedItem is string opcion)
        {
            BuscarPorSeleccionado = opcion;
            AplicarFiltros();
        }
    }

    private void OnOrdenarConversacionesChanged(
        object sender,
        EventArgs e)
    {
        if (OrdenarConversacionesPicker.SelectedItem is string orden)
        {
            OrdenSeleccionado = orden;

            AplicarFiltros();
        }
    }

    private void AplicarFiltros()
    {
        string texto = BuscarConversacionEntry.Text?.Trim()
            ?? string.Empty;

        var resultado = TodasLasConversaciones
    .Where(c =>
        string.IsNullOrWhiteSpace(texto) ||
        (BuscarPorSeleccionado == "Producto"
            ? (c.NombreProducto?.Contains(
                texto,
                StringComparison.OrdinalIgnoreCase) ?? false)
            : (c.NombreOtroUsuario?.Contains(
                texto,
                StringComparison.OrdinalIgnoreCase) ?? false)))
    .ToList();


        if (OrdenSeleccionado.Trim().Equals(
            "Más antiguas",
            StringComparison.OrdinalIgnoreCase))
        {
            resultado.Sort(
                (a, b) => a.UltimaActividad.CompareTo(b.UltimaActividad));
        }
        else
        {
            resultado.Sort(
                (a, b) => b.UltimaActividad.CompareTo(a.UltimaActividad));
        }
        ConversacionesCollectionView.ItemsSource = resultado;
    }

    private async void OnConversacionSeleccionada(
    object sender,
    SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0)
        {
            return;
        }

        var conversacion =
            e.CurrentSelection[0] as Conversacion;

        if (conversacion == null)
        {
            return;
        }

        ConversacionesCollectionView.SelectedItem = null;

        await Shell.Current.GoToAsync(
            $"{nameof(ChatPage)}?idConversacion={conversacion.IdConversacion}");
    }
    private async void OnExplorarPublicacionesClicked(
    object sender,
    EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}
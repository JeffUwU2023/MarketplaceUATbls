using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using System.Text.Json;
using MarketplaceUAT.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MarketplaceUAT.App.Pages;

[QueryProperty(nameof(IdProducto), "idProducto")]
public partial class ConversacionesPublicacionPage : ContentPage
{
    private ApiClient Api =>
    Application.Current!.Handler.MauiContext!.Services
        .GetRequiredService<ApiClient>();
    private string idProducto = string.Empty;

    private List<Conversacion> TodasLasConversaciones = new();

    public string IdProducto
    {
        get => idProducto;
        set
        {
            idProducto = value;
        }
    }

    public ConversacionesPublicacionPage()
    {
        InitializeComponent();
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
            ConversacionesCollectionView.ItemsSource = null;
            TodasLasConversaciones.Clear();

            if (!SesionUsuario.EstaLogueado)
            {
                await DisplayAlertAsync(
                    "Cuenta requerida",
                    "Debes iniciar sesión para ver las conversaciones.",
                    "OK");

                return;
            }

            if (!int.TryParse(IdProducto, out int idProductoActual))
            {
                await DisplayAlertAsync(
                    "Error",
                    "No se recibió un identificador de producto válido.",
                    "OK");

                return;
            }

            string url =
            $"{ApiConfig.BaseUrl}/api/Conversaciones/Usuario";

            using var response = await Api.EnviarAutenticadoAsync(() =>
                new HttpRequestMessage(HttpMethod.Get, url));

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudieron cargar las conversaciones.\nCódigo: {(int)response.StatusCode}",
                    "OK");

                return;
            }

            string json = await response.Content.ReadAsStringAsync();

            var conversaciones =
                JsonSerializer.Deserialize<List<Conversacion>>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (conversaciones == null)
                return;

            int idUsuarioActual =
                SesionUsuario.UsuarioActual!.IdUsuario;

            TodasLasConversaciones = conversaciones
                .Where(c =>
                    c.IdProducto == idProductoActual &&
                    c.IdUsuarioVendedor == idUsuarioActual)
                .ToList();

            AplicarFiltro();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error de conexión",
                $"No se pudieron cargar las conversaciones.\n\n{ex.Message}",
                "OK");
        }
    }

    private void OnBuscarUsuarioTextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        string texto = BuscarUsuarioEntry.Text?.Trim()
            ?? string.Empty;

        var resultado = TodasLasConversaciones
            .Where(c =>
                string.IsNullOrWhiteSpace(texto) ||
                (c.NombreOtroUsuario?.Contains(
                    texto,
                    StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderByDescending(c => c.UltimaActividad)
            .ToList();

        ConversacionesCollectionView.ItemsSource = resultado;
    }

    private async void OnConversacionSeleccionada(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0)
            return;

        if (e.CurrentSelection[0] is not Conversacion conversacion)
            return;

        ConversacionesCollectionView.SelectedItem = null;

        await Shell.Current.GoToAsync(
            $"{nameof(ChatPage)}?idConversacion={conversacion.IdConversacion}",
            animate: false);
    }

}

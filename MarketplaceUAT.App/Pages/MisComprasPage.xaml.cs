using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MarketplaceUAT.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MarketplaceUAT.App.Pages
{
    public partial class MisComprasPage : ContentPage
    {
        private ApiClient Api =>
        Application.Current!.Handler.MauiContext!.Services
        .GetRequiredService<ApiClient>();
        private readonly JsonSerializerOptions _opcionesJson =
            new()
            {
                PropertyNameCaseInsensitive = true
            };

        private int _cantidadNotificacionesNoLeidas;

        public MisComprasPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await CargarComprasAsync();
            await CargarNotificacionesAsync();
        }

        private async Task CargarComprasAsync()
        {
            CargandoIndicator.IsVisible = true;
            CargandoIndicator.IsRunning = true;
            ComprasCollection.IsVisible = false;
            ErrorLabel.IsVisible = false;

            string? token = SesionUsuario.Token;

            if (!SesionUsuario.EstaLogueado ||
                string.IsNullOrWhiteSpace(token))
            {
                MostrarError("Inicia sesión para consultar tus compras.");
                return;
            }

            try
            {
                string url =
    $"{ApiConfig.BaseUrl.TrimEnd('/')}/api/Transacciones/MisCompras";

                using var respuesta = await Api.EnviarAutenticadoAsync(() =>
                    new HttpRequestMessage(HttpMethod.Get, url));

                if (!respuesta.IsSuccessStatusCode)
                {
                    string detalle =
                        await respuesta.Content.ReadAsStringAsync();

                    MostrarError(
                        $"No se pudieron cargar tus compras.\n" +
                        $"Código HTTP: {(int)respuesta.StatusCode}\n{detalle}");

                    return;
                }

                var compras =
                    await respuesta.Content.ReadFromJsonAsync<List<TransaccionHistorialDto>>(
                        _opcionesJson);

                ComprasCollection.ItemsSource =
                    compras ?? new List<TransaccionHistorialDto>();

                ComprasCollection.IsVisible = true;
            }
            catch (TaskCanceledException)
            {
                MostrarError(
                    "La solicitud tardó demasiado. Comprueba que la API esté ejecutándose.");
            }
            catch (Exception ex)
            {
                MostrarError(
                    $"No se pudieron cargar tus compras.\n{ex.Message}");
            }
            finally
            {
                CargandoIndicator.IsRunning = false;
                CargandoIndicator.IsVisible = false;
            }
        }


        private async void OnConfirmarCompraClicked(
            object sender, EventArgs e)
        {
            await EjecutarAccionTransaccionAsync(
                sender,
                "Confirmar",
                "¿Confirmas que recibiste el producto y que la compraventa se realizó?");
        }

        private async void OnSolicitarCancelacionClicked(
            object sender, EventArgs e)
        {
            await EjecutarAccionTransaccionAsync(
                sender,
                "SolicitarCancelacion",
                "¿Deseas solicitar la cancelación de esta compraventa?");
        }

        private async Task EjecutarAccionTransaccionAsync(
            object sender,
            string accion,
            string pregunta)
        {
            if (sender is not Button boton ||
                boton.BindingContext is not TransaccionHistorialDto compra)
            {
                return;
            }

            bool confirmar = await DisplayAlertAsync(
                "Confirmar acción",
                pregunta,
                "Sí",
                "No");

            if (!confirmar)
                return;

            string? token = SesionUsuario.Token;

            if (!SesionUsuario.EstaLogueado ||
                string.IsNullOrWhiteSpace(token))
            {
                await DisplayAlertAsync(
                    "Cuenta requerida",
                    "Inicia sesión para realizar esta acción.",
                    "Aceptar");
                return;
            }

            boton.IsEnabled = false;

            try
            {
                string url =
    $"{ApiConfig.BaseUrl.TrimEnd('/')}/api/Transacciones/" +
    $"{compra.IdTransaccion}/{accion}";

                using var respuesta = await Api.EnviarAutenticadoAsync(() =>
                    new HttpRequestMessage(HttpMethod.Put, url));

                string detalle =
                    await respuesta.Content.ReadAsStringAsync();

                if (!respuesta.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync(
                        "No se pudo realizar la acción",
                        $"Código HTTP: {(int)respuesta.StatusCode}\n{detalle}",
                        "Aceptar");
                    return;
                }

                await DisplayAlertAsync(
                    "Operación realizada",
                    detalle,
                    "Aceptar");

                await CargarComprasAsync();
            }
            catch (TaskCanceledException)
            {
                await DisplayAlertAsync(
                    "Tiempo de espera agotado",
                    "La API tardó demasiado en responder.",
                    "Aceptar");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudo completar la operación.\n{ex.Message}",
                    "Aceptar");
            }
            finally
            {
                boton.IsEnabled = true;
            }
        }

        private async void OnAceptarCancelacionCompraClicked(object sender, EventArgs e)
        {
            await EjecutarAccionTransaccionAsync(
                sender,
                "AceptarCancelacion",
                "¿Aceptas la solicitud de cancelación de esta compraventa?");
        }

        private void MostrarError(string mensaje)
        {
            ErrorLabel.Text = mensaje;
            ErrorLabel.IsVisible = true;
            ComprasCollection.IsVisible = false;
        }

        private async Task CargarNotificacionesAsync()
        {
            NotificacionesBorder.IsVisible = false;

            string? token = SesionUsuario.Token;

            if (!SesionUsuario.EstaLogueado ||
                string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            try
            {
                string url =
    $"{ApiConfig.BaseUrl.TrimEnd('/')}/api/NotificacionesTransacciones/PorSeccion/compras";

                using var respuesta = await Api.EnviarAutenticadoAsync(() =>
                    new HttpRequestMessage(HttpMethod.Get, url));

                if (!respuesta.IsSuccessStatusCode)
                    return;

                var notificaciones =
                    await respuesta.Content.ReadFromJsonAsync<List<NotificacionTransaccionDto>>(
                        _opcionesJson);

                if (notificaciones == null || notificaciones.Count == 0)
                    return;

                _cantidadNotificacionesNoLeidas = notificaciones.Count;

                NotificacionesTituloLabel.Text =
                    $"Tienes {_cantidadNotificacionesNoLeidas} notificación(es) sin leer";

                NotificacionesMensajeLabel.Text =
                    notificaciones[0].Mensaje;

                NotificacionesBorder.IsVisible = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error al cargar notificaciones de compras: {ex.Message}");

                NotificacionesBorder.IsVisible = false;
            }
        }

        private async Task<bool> ManejarSesionExpiradaAsync(
            HttpResponseMessage respuesta)
        {
            if (respuesta.StatusCode != HttpStatusCode.Unauthorized)
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

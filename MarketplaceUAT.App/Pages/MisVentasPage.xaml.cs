using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using MarketplaceUAT.App.Services;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;

namespace MarketplaceUAT.App.Pages
{
    public partial class MisVentasPage : ContentPage
    {
        private ApiClient Api =>
        Application.Current!.Handler.MauiContext!.Services
        .GetRequiredService<ApiClient>();
        private readonly JsonSerializerOptions _opcionesJson =
            new()
            {
                PropertyNameCaseInsensitive = true
            };

        public MisVentasPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await CargarVentasAsync();
            await CargarNotificacionesAsync();
        }

        private async Task CargarVentasAsync()
        {
            CargandoIndicator.IsVisible = true;
            CargandoIndicator.IsRunning = true;
            VentasCollection.IsVisible = false;
            ErrorLabel.IsVisible = false;

            string? token = SesionUsuario.Token;

            if (!SesionUsuario.EstaLogueado ||
                string.IsNullOrWhiteSpace(token))
            {
                MostrarError("Inicia sesión para consultar tus ventas.");
                CargandoIndicator.IsRunning = false;
                CargandoIndicator.IsVisible = false;
                return;
            }

            try
            {
                string url =
    $"{ApiConfig.BaseUrl.TrimEnd('/')}/api/Transacciones/MisVentas";

                using var respuesta = await Api.EnviarAutenticadoAsync(() =>
                    new HttpRequestMessage(HttpMethod.Get, url));

                if (!respuesta.IsSuccessStatusCode)
                {
                    string detalle =
                        await respuesta.Content.ReadAsStringAsync();

                    MostrarError(
                        $"No se pudieron cargar tus ventas.\n" +
                        $"Código HTTP: {(int)respuesta.StatusCode}\n{detalle}");

                    return;
                }

                var ventas =
                    await respuesta.Content.ReadFromJsonAsync<List<TransaccionHistorialDto>>(
                        _opcionesJson);

                VentasCollection.ItemsSource =
                    ventas ?? new List<TransaccionHistorialDto>();

                VentasCollection.IsVisible = true;
            }
            catch (TaskCanceledException)
            {
                MostrarError(
                    "La solicitud tardó demasiado. Comprueba que la API esté ejecutándose.");
            }
            catch (Exception ex)
            {
                MostrarError(
                    $"No se pudieron cargar tus ventas.\n{ex.Message}");
            }
            finally
            {
                CargandoIndicator.IsRunning = false;
                CargandoIndicator.IsVisible = false;
            }
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
    $"{ApiConfig.BaseUrl.TrimEnd('/')}/api/NotificacionesTransacciones/PorSeccion/ventas";

                using var respuesta = await Api.EnviarAutenticadoAsync(() =>
                    new HttpRequestMessage(HttpMethod.Get, url));

                if (!respuesta.IsSuccessStatusCode)
                    return;

                var notificaciones =
                    await respuesta.Content.ReadFromJsonAsync<List<NotificacionTransaccionDto>>(
                        _opcionesJson);

                if (notificaciones == null || notificaciones.Count == 0)
                    return;

                NotificacionesTituloLabel.Text =
                    $"Tienes {notificaciones.Count} notificación(es) sin leer";

                NotificacionesMensajeLabel.Text =
                    notificaciones[0].Mensaje;

                NotificacionesBorder.IsVisible = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error al cargar notificaciones de ventas: {ex.Message}");

                NotificacionesBorder.IsVisible = false;
            }
        }

        private void MostrarError(string mensaje)
        {
            ErrorLabel.Text = mensaje;
            ErrorLabel.IsVisible = true;
            VentasCollection.IsVisible = false;
        }

        private async void OnConfirmarVentaClicked(object sender, EventArgs e)
        {
            await EjecutarAccionVentaAsync(
                sender,
                "Confirmar",
                "¿Confirmas que la compraventa se realizó?",
                "Confirmar venta");
        }

        private async void OnSolicitarCancelacionVentaClicked(object sender, EventArgs e)
        {
            await EjecutarAccionVentaAsync(
                sender,
                "SolicitarCancelacion",
                "¿Deseas solicitar la cancelación de esta compraventa?",
                "Solicitar cancelación");
        }

        private async void OnAceptarCancelacionClicked(object sender, EventArgs e)
        {
            await EjecutarAccionVentaAsync(
                sender,
                "AceptarCancelacion",
                "¿Aceptas la solicitud de cancelación del comprador?",
                "Aceptar cancelación");
        }

        private async Task EjecutarAccionVentaAsync(
            object sender,
            string accion,
            string pregunta,
            string titulo)
        {
            if (sender is not Button boton ||
                boton.BindingContext is not TransaccionHistorialDto venta)
            {
                return;
            }

            bool confirmar = await DisplayAlertAsync(
                titulo,
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
    $"{venta.IdTransaccion}/{accion}";

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

                await CargarVentasAsync();
                await CargarNotificacionesAsync();
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

        private async void OnVentaTapped(object sender, TappedEventArgs e)
        {
            if (sender is not Border borde ||
                borde.BindingContext is not TransaccionHistorialDto venta)
            {
                return;
            }

            if (SesionUsuario.UsuarioActual == null ||
                venta.IdUsuarioVendedor != SesionUsuario.UsuarioActual.IdUsuario)
            {
                await DisplayAlertAsync(
                    "Acceso denegado",
                    "Solo el vendedor puede consultar esta venta.",
                    "OK");

                return;
            }

            await Shell.Current.GoToAsync(
                $"{nameof(ProductoDetallePage)}?idProducto={venta.IdProducto}");
        }
    }
}

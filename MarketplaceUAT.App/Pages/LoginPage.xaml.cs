using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using System.Net.Http.Json;

namespace MarketplaceUAT.App.Pages
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void OnLimpiarCorreoClicked(object sender, EventArgs e)
        {
            CorreoEntry.Text = string.Empty;
        }

        private void OnLimpiarPasswordClicked(object sender, EventArgs e)
        {
            PasswordEntry.Text = string.Empty;
        }

        private void OnMostrarPasswordClicked(object sender, EventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;

            MostrarPasswordButton.ImageSource =
                PasswordEntry.IsPassword
                    ? "ojo_abierto.svg"
                    : "ojo_tachado.svg";
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            try
            {
                LoginButton.IsEnabled = false;

                string correo = CorreoEntry.Text?.Trim() ?? string.Empty;
                string password = PasswordEntry.Text ?? string.Empty;

                if (string.IsNullOrWhiteSpace(correo) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    await DisplayAlertAsync(
                        "Datos incompletos",
                        "Escribe tu correo y contraseña.",
                        "OK");

                    return;
                }

                using var handler = new SocketsHttpHandler
                {
                    Proxy = null,
                    UseProxy = false
                };

                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(15)
                };

                client.DefaultRequestHeaders.ConnectionClose = true;

                string url = $"{ApiConfig.BaseUrl}/api/Usuarios/Login";

                var datosLogin = new
                {
                    correo,
                    password
                };

                var response = await client.PostAsJsonAsync(
                    url,
                    datosLogin);

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync(
                        "Error de inicio de sesión",
                        "Correo o contraseña incorrectos.",
                        "OK");

                    return;
                }

                var loginResponse =
                    await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (loginResponse == null ||
                    loginResponse.Usuario == null ||
                    string.IsNullOrWhiteSpace(loginResponse.Token))
                {
                    await DisplayAlertAsync(
                        "Error",
                        "No se pudieron obtener los datos del usuario.",
                        "OK");

                    return;
                }

                SesionUsuario.IniciarSesion(
                    loginResponse.Usuario,
                    loginResponse.Token);

                await DisplayAlertAsync(
                    "Bienvenido",
                    $"Hola, {loginResponse.Usuario.Nombres}",
                    "OK");

                await Shell.Current.GoToAsync("//MainPage");
            }
            catch (TaskCanceledException)
            {
                await DisplayAlertAsync(
                    "Tiempo de espera agotado",
                    "La API tardó demasiado en responder. Comprueba que esté ejecutándose.",
                    "OK");
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
                LoginButton.IsEnabled = true;
            }
        }

        private async void OnCrearCuentaClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(RegistroPage));
        }
    }
}

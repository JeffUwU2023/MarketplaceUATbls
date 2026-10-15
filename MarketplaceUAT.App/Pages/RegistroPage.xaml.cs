using MarketplaceUAT.App.Config;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace MarketplaceUAT.App.Pages
{
    public partial class RegistroPage : ContentPage
    {
        public RegistroPage()
        {
            InitializeComponent();

            TipoUsuarioPicker.SelectedIndex = 0;
        }

        private void OnTipoUsuarioChanged(object sender, EventArgs e)
        {
            bool esAlumno = TipoUsuarioPicker.SelectedItem?.ToString() == "Alumno";

            MatriculaLayout.IsVisible = esAlumno;

            if (esAlumno)
            {
                CorreoAyudaLabel.Text =
                    "Usa el formato a + matrícula de 10 dígitos + @alumnos.uat.edu.mx.";
                CorreoEntry.Placeholder = "a1234567890@alumnos.uat.edu.mx";
            }
            else
            {
                CorreoAyudaLabel.Text =
                    "Usa tu correo institucional con el dominio @docentes.uat.edu.mx.";
                CorreoEntry.Placeholder = "usuario@docentes.uat.edu.mx";
                MatriculaEntry.Text = string.Empty;
            }
        }

        private void OnMostrarPasswordClicked(object sender, EventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;

            MostrarPasswordButton.ImageSource =
                PasswordEntry.IsPassword
                    ? "ojo_abierto.svg"
                    : "ojo_tachado.svg";
        }

        private void OnLimpiarPasswordClicked(object sender, EventArgs e)
        {
            PasswordEntry.Text = string.Empty;
        }

        private async void OnRegistrarClicked(object sender, EventArgs e)
        {
            if (RegistrarButton.IsEnabled == false)
                return;

            try
            {
                RegistrarButton.IsEnabled = false;

                string nombres = NombresEntry.Text?.Trim() ?? string.Empty;
                string apellidos = ApellidosEntry.Text?.Trim() ?? string.Empty;
                string tipoUsuario = TipoUsuarioPicker.SelectedItem?.ToString() ?? string.Empty;
                string matricula = MatriculaEntry.Text?.Trim() ?? string.Empty;
                string correo = CorreoEntry.Text?.Trim().ToLowerInvariant() ?? string.Empty;
                string password = PasswordEntry.Text ?? string.Empty;

                if (string.IsNullOrWhiteSpace(tipoUsuario) ||
                    string.IsNullOrWhiteSpace(nombres) ||
                    string.IsNullOrWhiteSpace(apellidos) ||
                    string.IsNullOrWhiteSpace(correo) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    await DisplayAlertAsync(
                        "Datos incompletos",
                        "Completa todos los campos obligatorios.",
                        "OK");
                    return;
                }

                if (nombres.Length < 2 || nombres.Length > 100 ||
                    apellidos.Length < 2 || apellidos.Length > 100)
                {
                    await DisplayAlertAsync(
                        "Datos inválidos",
                        "Los nombres y apellidos deben tener entre 2 y 100 caracteres.",
                        "OK");
                    return;
                }

                if (password.Length < 8 || password.Length > 20)
                {
                    await DisplayAlertAsync(
                        "Contraseña inválida",
                        "La contraseña debe tener entre 8 y 20 caracteres.",
                        "OK");
                    return;
                }

                if (tipoUsuario == "Alumno")
                {
                    if (!Regex.IsMatch(matricula, @"^[0-9]{10}$"))
                    {
                        await DisplayAlertAsync(
                            "Matrícula inválida",
                            "La matrícula debe contener exactamente 10 dígitos.",
                            "OK");
                        return;
                    }

                    string correoEsperado = $"a{matricula}@alumnos.uat.edu.mx";

                    if (correo != correoEsperado)
                    {
                        await DisplayAlertAsync(
                            "Correo inválido",
                            "El correo debe coincidir con tu matrícula. Ejemplo: a1234567890@alumnos.uat.edu.mx.",
                            "OK");
                        return;
                    }
                }
                else if (tipoUsuario == "Docente")
                {
                    if (!Regex.IsMatch(
                        correo,
                        @"^[^@\s]+@docentes\.uat\.edu\.mx$"))
                    {
                        await DisplayAlertAsync(
                            "Correo inválido",
                            "El correo del docente debe terminar en @docentes.uat.edu.mx.",
                            "OK");
                        return;
                    }

                    matricula = string.Empty;
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

                string url = $"{ApiConfig.BaseUrl}/api/Usuarios";

                var datosRegistro = new
                {
                    nombres,
                    apellidos,
                    tipoUsuario,
                    matricula = string.IsNullOrWhiteSpace(matricula)
                        ? null
                        : matricula,
                    correo,
                    password
                };

                var response = await client.PostAsJsonAsync(
                    url,
                    datosRegistro);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync(
                        "Registro exitoso",
                        "Tu cuenta se creó correctamente. Ya puedes iniciar sesión.",
                        "OK");

                    await Shell.Current.GoToAsync("..");
                    return;
                }

                string mensaje = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    await DisplayAlertAsync(
                        "Cuenta existente",
                        mensaje,
                        "OK");
                }
                else
                {
                    await DisplayAlertAsync(
                        "No se pudo registrar",
                        mensaje,
                        "OK");
                }
            }
            catch (TaskCanceledException)
            {
                await DisplayAlertAsync(
                    "Tiempo de espera agotado",
                    "La API tardó demasiado en responder. Comprueba que esté ejecutándose y que la dirección sea correcta.",
                    "OK");
            }
            catch (HttpRequestException ex)
            {
                await DisplayAlertAsync(
                    "Error de conexión",
                    $"No se pudo conectar con la API. Detalle: {ex.Message}",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    ex.Message,
                    "OK");
            }
            finally
            {
                RegistrarButton.IsEnabled = true;
            }
        }
    }
}

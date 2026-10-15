using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using Microsoft.Maui.Storage;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MarketplaceUAT.App.Pages
{
    public partial class PerfilPage : ContentPage
    {
        public PerfilPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            CargarDatosUsuario();
        }

        private void CargarDatosUsuario()
        {
            if (!SesionUsuario.EstaLogueado ||
                SesionUsuario.UsuarioActual == null)
            {
                // Sin sesión: mostrar el avatar predeterminado
                // y ocultar el botón para eliminar fotografías.
                EliminarFotoButton.IsVisible = false;

                FotoPerfilImage.Source = null;
                FotoPerfilImage.IsVisible = false;
                FotoPerfilPlaceholder.IsVisible = true;

                return;
            }

            var usuario = SesionUsuario.UsuarioActual;

            NombresEntry.Text = usuario.Nombres;
            ApellidosEntry.Text = usuario.Apellidos;
            TipoUsuarioEntry.Text = usuario.TipoUsuario;
            CorreoEntry.Text = usuario.Correo;

            bool esAlumno = usuario.TipoUsuario == "Alumno";

            MatriculaLayout.IsVisible = esAlumno;
            MatriculaEntry.Text = esAlumno
                ? usuario.Matricula
                : string.Empty;

            // Comprobar si el usuario tiene una fotografía.
            bool tieneFoto = !string.IsNullOrWhiteSpace(
                usuario.UrlFotoPerfil);

            // El botón solo aparece si hay una fotografía.
            EliminarFotoButton.IsVisible = tieneFoto;

            if (tieneFoto)
            {
                string rutaFoto = usuario.UrlFotoPerfil!.TrimStart('/');

                string urlFoto =
                    $"{ApiConfig.BaseUrl.TrimEnd('/')}/{rutaFoto}";

                FotoPerfilImage.Source = new UriImageSource
                {
                    Uri = new Uri(urlFoto),
                    CachingEnabled = false
                };

                FotoPerfilImage.IsVisible = true;
                FotoPerfilPlaceholder.IsVisible = false;
            }
            else
            {
                FotoPerfilImage.Source = null;
                FotoPerfilImage.IsVisible = false;
                FotoPerfilPlaceholder.IsVisible = true;
            }
        }

        private async void OnCambiarFotoClicked(
        object sender,
        EventArgs e)
        {
            var usuario = SesionUsuario.UsuarioActual;
            string? token = SesionUsuario.Token;

            if (usuario == null || string.IsNullOrWhiteSpace(token))
            {
                await DisplayAlertAsync(
                    "Sesión requerida",
                    "Inicia sesión nuevamente para cambiar tu fotografía.",
                    "OK");

                return;
            }

            try
            {
                var fotos = await MediaPicker.Default.PickPhotosAsync(
                new MediaPickerOptions
                {
                            SelectionLimit = 1
                });

                var foto = fotos?.FirstOrDefault();

                if (foto == null)
                {
                    return;
                }

                using var stream = await foto.OpenReadAsync();

                using var handler = new SocketsHttpHandler
                {
                    Proxy = null,
                    UseProxy = false
                };

                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };

                client.DefaultRequestHeaders.ConnectionClose = true;

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                using var contenido = new MultipartFormDataContent();

                using var contenidoImagen = new StreamContent(stream);

                contenidoImagen.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        foto.ContentType ?? "application/octet-stream");

                contenido.Add(
                    contenidoImagen,
                    "archivo",
                    foto.FileName);

                string url =
                    $"{ApiConfig.BaseUrl}/api/Usuarios/{usuario.IdUsuario}/FotoPerfil";

                CambiarFotoButton.IsEnabled = false;

                using var respuesta = await client.PostAsync(url, contenido);

                if (!respuesta.IsSuccessStatusCode)
                {
                    string mensaje =
                        await respuesta.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "No se pudo cambiar la fotografía",
                        $"Código HTTP: {(int)respuesta.StatusCode}\n\n{mensaje}",
                        "OK");

                    return;
                }

                var usuarioActualizado =
                    await respuesta.Content.ReadFromJsonAsync<Usuario>();

                if (usuarioActualizado == null ||
                    string.IsNullOrWhiteSpace(usuarioActualizado.UrlFotoPerfil))
                {
                    await DisplayAlertAsync(
                        "Error",
                        "La API no devolvió la dirección de la fotografía.",
                        "OK");

                    return;
                }

                usuario.UrlFotoPerfil = usuarioActualizado.UrlFotoPerfil;

                SesionUsuario.IniciarSesion(usuario, token);

                string rutaFoto = usuario.UrlFotoPerfil.TrimStart('/');

                string urlFoto =
                    $"{ApiConfig.BaseUrl.TrimEnd('/')}/{rutaFoto}";

                FotoPerfilImage.Source = new UriImageSource
                {
                    Uri = new Uri(urlFoto),
                    CachingEnabled = false
                };

                FotoPerfilImage.IsVisible = true;
                FotoPerfilPlaceholder.IsVisible = false;

                await DisplayAlertAsync(
                    "Fotografía actualizada",
                    "Tu fotografía de perfil se guardó correctamente.",
                    "OK");
            }
            catch (TaskCanceledException)
            {
                await DisplayAlertAsync(
                    "Tiempo de espera agotado",
                    "La API tardó demasiado en responder. Comprueba que esté ejecutándose e inténtalo de nuevo.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudo cambiar la fotografía.\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                CambiarFotoButton.IsEnabled = true;
            }
        }

        private async void OnEliminarFotoClicked(object sender, EventArgs e)
        {
            var usuario = SesionUsuario.UsuarioActual;
            var token = SesionUsuario.Token;

            if (usuario == null || string.IsNullOrWhiteSpace(token))
            {
                await DisplayAlertAsync(
                    "Sesión requerida",
                    "Debes iniciar sesión para eliminar tu fotografía.",
                    "Aceptar");
                return;
            }

            if (string.IsNullOrWhiteSpace(usuario.UrlFotoPerfil))
            {
                await DisplayAlertAsync(
                    "Sin fotografía",
                    "No tienes una fotografía de perfil para eliminar.",
                    "Aceptar");
                return;
            }

            bool confirmar = await DisplayAlertAsync(
                "Eliminar fotografía",
                "¿Estás seguro de que deseas eliminar tu fotografía de perfil?",
                "Sí, eliminar",
                "Cancelar");

            if (!confirmar)
                return;

            try
            {
                using var cliente = new HttpClient();

                cliente.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        token);

                string url =
                    $"{ApiConfig.BaseUrl.TrimEnd('/')}/api/Usuarios/{usuario.IdUsuario}/FotoPerfil";

                var respuesta = await cliente.DeleteAsync(url);

                if (!respuesta.IsSuccessStatusCode)
                {
                    string detalle = await respuesta.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error",
                        $"No se pudo eliminar la fotografía. Código: {(int)respuesta.StatusCode}\n{detalle}",
                        "Aceptar");
                    return;
                }

                usuario.UrlFotoPerfil = null;

                SesionUsuario.IniciarSesion(usuario, token);

                FotoPerfilImage.Source = null;
                FotoPerfilImage.IsVisible = false;
                FotoPerfilPlaceholder.IsVisible = true;

                EliminarFotoButton.IsVisible = false;

                await DisplayAlertAsync(
                    "Fotografía eliminada",
                    "Tu fotografía de perfil se eliminó correctamente.",
                    "Aceptar");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error de conexión",
                    $"No fue posible comunicarse con la API.\n{ex.Message}",
                    "Aceptar");
            }
        }

        private bool ValidarDatosPerfil(out string correo)
        {
            correo = CorreoEntry.Text?.Trim().ToLowerInvariant()
                ?? string.Empty;

            string nombres = NombresEntry.Text?.Trim()
                ?? string.Empty;

            string apellidos = ApellidosEntry.Text?.Trim()
                ?? string.Empty;

            var usuario = SesionUsuario.UsuarioActual;

            if (usuario == null)
            {
                DisplayAlertAsync(
                    "Sesión requerida",
                    "Inicia sesión nuevamente.",
                    "OK");

                return false;
            }

            if (nombres.Length < 2 || nombres.Length > 100)
            {
                DisplayAlertAsync(
                    "Nombres incorrectos",
                    "Los nombres deben tener entre 2 y 100 caracteres.",
                    "OK");

                return false;
            }

            if (apellidos.Length < 2 || apellidos.Length > 100)
            {
                DisplayAlertAsync(
                    "Apellidos incorrectos",
                    "Los apellidos deben tener entre 2 y 100 caracteres.",
                    "OK");

                return false;
            }

            if (correo.Length == 0 || correo.Length > 150 ||
                !Regex.IsMatch(
                    correo,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                DisplayAlertAsync(
                    "Correo incorrecto",
                    "Introduce un correo electrónico válido.",
                    "OK");

                return false;
            }

            if (usuario.TipoUsuario == "Alumno")
            {
                string matricula = usuario.Matricula ?? string.Empty;
                string correoEsperado =
                    $"a{matricula}@alumnos.uat.edu.mx";

                if (!Regex.IsMatch(matricula, @"^[0-9]{10}$") ||
                    correo != correoEsperado)
                {
                    DisplayAlertAsync(
                        "Correo institucional",
                        "El correo del alumno debe coincidir con su matrícula y usar @alumnos.uat.edu.mx.",
                        "OK");

                    return false;
                }
            }
            else if (usuario.TipoUsuario == "Docente")
            {
                if (!Regex.IsMatch(
                    correo,
                    @"^[^@\s]+@docentes\.uat\.edu\.mx$"))
                {
                    DisplayAlertAsync(
                        "Correo institucional",
                        "El correo del docente debe pertenecer al dominio @docentes.uat.edu.mx.",
                        "OK");

                    return false;
                }
            }

            return true;
        }

        private async void OnGuardarClicked(
            object sender,
            EventArgs e)
        {
            if (!ValidarDatosPerfil(out string correo))
            {
                return;
            }

            await ActualizarUsuarioAsync(correo, null, false);
        }

        private async void OnCambiarPasswordClicked(
            object sender,
            EventArgs e)
        {
            string password = PasswordEntry.Text ?? string.Empty;
            string confirmacion =
                ConfirmarPasswordEntry.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlertAsync(
                    "Contraseña requerida",
                    "Escribe la nueva contraseña.",
                    "OK");

                return;
            }

            if (password.Length < 8 || password.Length > 20)
            {
                await DisplayAlertAsync(
                    "Contraseña incorrecta",
                    "La contraseña debe tener entre 8 y 20 caracteres.",
                    "OK");

                return;
            }

            if (password != confirmacion)
            {
                await DisplayAlertAsync(
                    "Contraseñas diferentes",
                    "Las contraseñas no coinciden.",
                    "OK");

                return;
            }

            if (!ValidarDatosPerfil(out string correo))
            {
                return;
            }

            await ActualizarUsuarioAsync(correo, password, true);
        }

        private async Task ActualizarUsuarioAsync(
            string correo,
            string? password,
            bool esCambioPassword)
        {
            var usuario = SesionUsuario.UsuarioActual;
            string? token = SesionUsuario.Token;

            if (usuario == null ||
                string.IsNullOrWhiteSpace(token))
            {
                await DisplayAlertAsync(
                    "Sesión requerida",
                    "Inicia sesión nuevamente.",
                    "OK");

                await Shell.Current.GoToAsync(nameof(LoginPage));
                return;
            }

            try
            {
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

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var datos = new
                {
                    Nombres = NombresEntry.Text!.Trim(),
                    Apellidos = ApellidosEntry.Text!.Trim(),
                    Correo = correo,
                    Password = password
                };

                string json = JsonSerializer.Serialize(datos);

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                string url =
                    $"{ApiConfig.BaseUrl}/api/Usuarios/{usuario.IdUsuario}";

                var response = await client.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string mensaje =
                        await response.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "No se pudieron guardar los cambios",
                        $"Código HTTP: {(int)response.StatusCode}\n\n{mensaje}",
                        "OK");

                    return;
                }

                usuario.Nombres = NombresEntry.Text!.Trim();
                usuario.Apellidos = ApellidosEntry.Text!.Trim();
                usuario.Correo = correo;

                SesionUsuario.IniciarSesion(usuario, token);

                if (esCambioPassword)
                {
                    PasswordEntry.Text = string.Empty;
                    ConfirmarPasswordEntry.Text = string.Empty;

                    await DisplayAlertAsync(
                        "Contraseña actualizada",
                        "Tu contraseña se cambió correctamente.",
                        "OK");
                }
                else
                {
                    await DisplayAlertAsync(
                        "Perfil actualizado",
                        "Tus datos se guardaron correctamente.",
                        "OK");
                }
            }
            catch (TaskCanceledException)
            {
                await DisplayAlertAsync(
                    "Tiempo de espera agotado",
                    "La solicitud tardó demasiado. Comprueba que la API esté ejecutándose y vuelve a intentarlo.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error de conexión",
                    ex.Message,
                    "OK");
            }
        }
    }
}
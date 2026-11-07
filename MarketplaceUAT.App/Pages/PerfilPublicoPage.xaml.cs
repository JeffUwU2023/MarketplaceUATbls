using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using System.Net.Http.Json;
using System.Linq;
using MarketplaceUAT.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MarketplaceUAT.App.Pages
{
    [QueryProperty(nameof(IdUsuario), "IdUsuario")]
    public partial class PerfilPublicoPage : ContentPage
    {
        private ApiClient Api =>
        Application.Current!.Handler.MauiContext!.Services
        .GetRequiredService<ApiClient>();
        private bool _cargandoPerfil;
        public PerfilPublicoPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_cargandoPerfil ||
                string.IsNullOrWhiteSpace(IdUsuario))
            {
                return;
            }

            _cargandoPerfil = true;

            try
            {
                await CargarPerfilAsync();
                await CargarPublicacionesAsync();
            }
            finally
            {
                _cargandoPerfil = false;
            }
        }

        private async Task CargarPerfilAsync()
        {
            IndicadorCarga.IsVisible = true;
            IndicadorCarga.IsRunning = true;
            MensajeErrorLabel.IsVisible = false;

            try
            {
                if (!SesionUsuario.EstaLogueado ||
                    string.IsNullOrWhiteSpace(SesionUsuario.Token))
                {
                    MostrarError("Inicia sesión para consultar este perfil.");
                    return;
                }

                if (!int.TryParse(IdUsuario, out int idUsuario) ||
                    idUsuario <= 0)
                {
                    MostrarError("No se recibió un identificador de usuario válido.");
                    return;
                }

                string url =
    $"{ApiConfig.BaseUrl.TrimEnd('/')}/api/Usuarios/{idUsuario}";

                using var respuesta = await Api.EnviarAutenticadoAsync(() =>
                    new HttpRequestMessage(HttpMethod.Get, url));

                if (!respuesta.IsSuccessStatusCode)
                {
                    MostrarError(
                        respuesta.StatusCode == System.Net.HttpStatusCode.NotFound
                            ? "Este usuario no existe o no está disponible."
                            : $"No se pudo cargar el perfil. Código HTTP: {(int)respuesta.StatusCode}.");

                    return;
                }

                var usuario = await respuesta.Content
                    .ReadFromJsonAsync<UsuarioPerfil>();

                if (usuario == null)
                {
                    MostrarError("La API no devolvió los datos del perfil.");
                    return;
                }

                NombreCompletoLabel.Text =
                    $"{usuario.Nombres} {usuario.Apellidos}".Trim();

                NombresLabel.Text = usuario.Nombres;
                ApellidosLabel.Text = usuario.Apellidos;
                TipoUsuarioLabel.Text = usuario.TipoUsuario;

                bool tieneCorreo =
                    !string.IsNullOrWhiteSpace(usuario.Correo);

                CorreoLayout.IsVisible = tieneCorreo;
                CorreoLabel.Text = usuario.Correo ?? string.Empty;

                FechaRegistroLabel.Text =
                    usuario.FechaRegistro.ToString("MMMM yyyy");

                if (!string.IsNullOrWhiteSpace(usuario.UrlFotoPerfil))
                {
                    string ruta = usuario.UrlFotoPerfil.TrimStart('/');
                    string urlFoto =
                        $"{ApiConfig.BaseUrl.TrimEnd('/')}/{ruta}";

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
            catch (TaskCanceledException)
            {
                MostrarError("La solicitud tardó demasiado. Inténtalo nuevamente.");
            }
            catch (Exception ex)
            {
                MostrarError($"No se pudo cargar el perfil: {ex.Message}");
            }
            finally
            {
                IndicadorCarga.IsRunning = false;
                IndicadorCarga.IsVisible = false;
            }
        }

        public string? IdUsuario { get; set; }

        public void EstablecerUsuario(int idUsuario)
        {
            IdUsuario = idUsuario.ToString();
        }

        private async Task CargarPublicacionesAsync()
        {
            if (!SesionUsuario.EstaLogueado ||
                string.IsNullOrWhiteSpace(SesionUsuario.Token))
            {
                return;
            }

            if (!int.TryParse(IdUsuario, out int idUsuario) ||
                idUsuario <= 0)
            {
                return;
            }

            IndicadorCarga.IsVisible = true;
            IndicadorCarga.IsRunning = true;

            try
            {

                string urlProductos =
    $"{ApiConfig.BaseUrl.TrimEnd('/')}/api/Productos/Usuario/{idUsuario}";

                using var respuesta = await Api.EnviarAutenticadoAsync(() =>
                    new HttpRequestMessage(HttpMethod.Get, urlProductos));

                if (!respuesta.IsSuccessStatusCode)
                {
                    PublicacionesCollection.ItemsSource = null;
                    SinPublicacionesLabel.Text =
                        "No se pudieron cargar las publicaciones.";
                    SinPublicacionesLabel.IsVisible = true;
                    return;
                }

                var productos = await respuesta.Content
                    .ReadFromJsonAsync<List<Producto>>();

                productos ??= new List<Producto>();

                // Mostrar solamente publicaciones activas y disponibles o reservadas.
                productos = productos
                    .Where(p =>
                        p.Activo &&
                        (p.Estado == "Disponible" ||
                         p.Estado == "Reservado"))
                    .ToList();

                foreach (var producto in productos)
                {
                    // Imagen predeterminada si no existe una imagen principal.
                    producto.ImagenUrl = "dotnet_bot.png";

                    try
                    {
                        string urlImagenes =
                            $"{ApiConfig.BaseUrl.TrimEnd('/')}/api/ImagenesProducto/Producto/{producto.IdProducto}";

                        using var respuestaImagenes =
    await Api.EnviarAutenticadoAsync(() =>
        new HttpRequestMessage(HttpMethod.Get, urlImagenes));

                        if (respuestaImagenes.IsSuccessStatusCode)
                        {
                            var imagenes = await respuestaImagenes.Content
                                .ReadFromJsonAsync<List<ImagenProducto>>();

                            var imagenPrincipal =
                                imagenes?.FirstOrDefault(i => i.EsPrincipal);

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
                        // Si falla la carga de una imagen, se conserva la imagen predeterminada.
                    }
                }


                PublicacionesCollection.ItemsSource = productos;

                // Actualizar las estadísticas del perfil.
                int totalDisponibles = productos.Count(p => p.Estado == "Disponible");
                int totalReservados = productos.Count(p => p.Estado == "Reservado");

                TotalPublicacionesLabel.Text = productos.Count.ToString();
                TotalDisponiblesLabel.Text = totalDisponibles.ToString();
                TotalReservadosLabel.Text = totalReservados.ToString();

                SinPublicacionesLabel.Text =
                    "Este usuario todavía no tiene publicaciones disponibles.";

                SinPublicacionesLabel.IsVisible = productos.Count == 0;

            }
            catch (TaskCanceledException)
            {
                SinPublicacionesLabel.Text =
                    "La solicitud tardó demasiado. Inténtalo nuevamente.";
                SinPublicacionesLabel.IsVisible = true;
            }
            catch (Exception ex)
            {
                SinPublicacionesLabel.Text =
                    $"No se pudieron cargar las publicaciones: {ex.Message}";
                SinPublicacionesLabel.IsVisible = true;
            }
            finally
            {
                IndicadorCarga.IsRunning = false;
                IndicadorCarga.IsVisible = false;
            }
        }

        private async void OnPublicacionTapped(
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

        private void MostrarError(string mensaje)
        {
            MensajeErrorLabel.Text = mensaje;
            MensajeErrorLabel.IsVisible = true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }
    }
}

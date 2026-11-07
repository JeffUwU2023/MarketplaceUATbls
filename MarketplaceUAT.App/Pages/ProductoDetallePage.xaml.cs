using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using MarketplaceUAT.App.Services;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MarketplaceUAT.App.Pages
{
    [QueryProperty(nameof(IdProducto), "idProducto")]
    public partial class ProductoDetallePage : ContentPage
    {
        private ApiClient Api =>
        Application.Current!.Handler.MauiContext!.Services
        .GetRequiredService<ApiClient>();
        private string idProducto = string.Empty;
        private List<string> ImagenesProducto = new();
        private Usuario? VendedorActual = null;
        private Producto? ProductoActual = null;

        private int? IdFavoritoActual = null;

        private int CantidadSeleccionada = 1;

        private int? IdCarritoActual = null;

        public string IdProducto
        {
            get => idProducto;
            set
            {
                idProducto = value;
                CargarProducto();
            }
        }

        public ProductoDetallePage()
        {

            InitializeComponent();

        }

        private async void CargarProducto()
        {
            try
            {
                var client = Api.Client;

                string url;

                if (SesionUsuario.EstaLogueado &&
                !string.IsNullOrWhiteSpace(SesionUsuario.Token))
                {
                    url = $"{ApiConfig.BaseUrl}/api/Productos/DetalleFavorito/{IdProducto}";
                }
                else
                {
                    url = $"{ApiConfig.BaseUrl}/api/Productos/{IdProducto}";
                }

                using var response = SesionUsuario.EstaLogueado &&
                     !string.IsNullOrWhiteSpace(SesionUsuario.Token)
    ? await Api.EnviarAutenticadoAsync(
        () => new HttpRequestMessage(HttpMethod.Get, url))
    : await client.GetAsync(url);

                if (await ManejarSesionExpiradaAsync(response))
                    return;

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync(
                        "Error",
                        $"No se pudo obtener el producto.\nCódigo: {(int)response.StatusCode}",
                        "OK");

                    return;
                }

                string json = await response.Content.ReadAsStringAsync();

                var producto = JsonSerializer.Deserialize<Producto>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (producto == null)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "No se pudo interpretar la información del producto.",
                        "OK");

                    return;
                }

                ProductoActual = producto;

                // Restablecer los botones al cargar un producto.
                RegistrarVentaButton.IsVisible = false;
                ConversacionesPublicacionButton.IsVisible = false;
                FavoritoButton.IsVisible = false;

                BorrarPublicacionButton.IsVisible = false;
                VolverPublicarButton.IsVisible = false;
                MarcarDisponibleButton.IsVisible = false;
                EditarPublicacionButton.IsVisible = false;
                ReactivarPublicacionButton.IsVisible = false;

                CantidadSeleccionada = 1;
                CantidadSeleccionadaLabel.Text = "1";

                SelectorCantidadLayout.IsVisible = false;
                AgregarCarritoButton.IsVisible = false;

                DisminuirCantidadButton.IsEnabled = false;
                AumentarCantidadButton.IsEnabled = false;

                // Restablecer el área de imágenes.
                ImagenesCarousel.IsVisible = false;
                IndicadorImagenes.IsVisible = false;
                ImagenPlaceholder.IsVisible = true;

                NombreProductoLabel.Text = producto.Nombre;
                PrecioProductoLabel.Text = $"${producto.Precio:N2}";
                CondicionProductoLabel.Text = $"Condición: {producto.Condicion}";

                bool noDisponible =
                    !producto.Activo || producto.Estado == "Vendido";

                bool esVendedorActual =
    SesionUsuario.EstaLogueado &&
    SesionUsuario.UsuarioActual != null &&
    SesionUsuario.UsuarioActual.IdUsuario == producto.IdUsuario;

                System.Diagnostics.Debug.WriteLine(
    $"[Detalle] ID: {producto.IdProducto}");

                System.Diagnostics.Debug.WriteLine(
                    $"[Detalle] Estado: {producto.Estado}");

                System.Diagnostics.Debug.WriteLine(
                    $"[Detalle] Activo: {producto.Activo}");

                System.Diagnostics.Debug.WriteLine(
                    $"[Detalle] ID vendedor: {producto.IdUsuario}");

                System.Diagnostics.Debug.WriteLine(
                    $"[Detalle] ID usuario actual: {SesionUsuario.UsuarioActual?.IdUsuario}");

                System.Diagnostics.Debug.WriteLine(
                    $"[Detalle] Es vendedor actual: {esVendedorActual}");

                EditarPublicacionButton.IsVisible =
    esVendedorActual &&
    producto.Activo &&
    producto.Estado == "Disponible";

                // Los botones especiales solo aparecen para el vendedor.
                BorrarPublicacionButton.IsVisible =
                    esVendedorActual && producto.Estado == "Vendido";

                VolverPublicarButton.IsVisible =
                    esVendedorActual && producto.Estado == "Vendido";

                ReactivarPublicacionButton.IsVisible =
                    esVendedorActual &&
                    producto.Estado == "Vendido";

                MarcarDisponibleButton.IsVisible =
                    esVendedorActual && producto.Estado == "Reservado";

                EstadoProductoLabel.Text = noDisponible
                    ? "Estado: No disponible"
                    : $"Estado: {producto.EstadoTexto}";

                if (producto.CantidadDisponible <= 0)
                {
                    CantidadDisponibleLabel.Text = "📦 Agotado · 0 unidades";
                    CantidadDisponibleLabel.TextColor = Colors.Red;
                    CantidadDisponibleLabel.IsVisible = true;
                }
                else if (producto.EsProductoUnico)
                {
                    CantidadDisponibleLabel.Text = "📦 Disponible · 1 unidad";
                    CantidadDisponibleLabel.TextColor = Color.FromArgb("#218838");
                    CantidadDisponibleLabel.IsVisible = true;
                }
                else
                {
                    CantidadDisponibleLabel.Text =
                        $"📦 Disponibles · {producto.CantidadDisponible} unidades";

                    CantidadDisponibleLabel.TextColor = Color.FromArgb("#218838");
                    CantidadDisponibleLabel.IsVisible = true;
                }

                NoDisponibleLabel.IsVisible = noDisponible;

                if (noDisponible)
                {
                    AccionProductoButton.IsEnabled = false;
                    AccionProductoButton.Text = "Publicación no disponible";

                    RegistrarVentaButton.IsVisible = false;
                    ConversacionesPublicacionButton.IsVisible = false;
                }
                else
                {
                    AccionProductoButton.IsEnabled = true;
                }

                string imagenesUrl =
                       $"{ApiConfig.BaseUrl}/api/ImagenesProducto/Producto/{producto.IdProducto}";

                var imagenesResponse = await client.GetAsync(imagenesUrl);

                if (await ManejarSesionExpiradaAsync(imagenesResponse))
                    return;

                ImagenesProducto.Clear();

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

                    if (imagenes != null)
                    {
                        foreach (var imagen in imagenes.Where(i => i.Activo))
                        {
                            string urlImagen =
                                $"{ApiConfig.BaseUrl}/Images/{imagen.UrlImagen}";

                            ImagenesProducto.Add(urlImagen);
                        }
                    }
                }

                ImagenesCarousel.ItemsSource = null;
                ImagenesCarousel.ItemsSource = ImagenesProducto;

                bool tieneImagenes = ImagenesProducto.Count > 0;

                ImagenesCarousel.IsVisible = tieneImagenes;
                IndicadorImagenes.IsVisible = ImagenesProducto.Count > 1;
                ImagenPlaceholder.IsVisible = !tieneImagenes;

                string categoriaUrl =
                       $"{ApiConfig.BaseUrl}/api/Categorias/{producto.IdCategoria}";

                var categoriaResponse = await client.GetAsync(categoriaUrl);

                if (await ManejarSesionExpiradaAsync(categoriaResponse))
                    return;

                if (categoriaResponse.IsSuccessStatusCode)
                {
                    string categoriaJson =
                        await categoriaResponse.Content.ReadAsStringAsync();

                    var categoria = JsonSerializer.Deserialize<Categoria>(
                        categoriaJson,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    CategoriaProductoLabel.Text =
                        $"Categoría: {categoria?.Nombre ?? "Sin categoría"}";
                }
                else
                {
                    CategoriaProductoLabel.Text = "Categoría: Sin categoría";
                }

                DescripcionProductoLabel.Text =
                        producto.Descripcion ?? "Sin descripción";

                string vendedorUrl =
    $"{ApiConfig.BaseUrl}/api/Usuarios/{producto.IdUsuario}";

                var vendedorResponse = await client.GetAsync(vendedorUrl);

                if (await ManejarSesionExpiradaAsync(vendedorResponse))
                    return;

                if (vendedorResponse.IsSuccessStatusCode)
                {
                    string vendedorJson =
                        await vendedorResponse.Content.ReadAsStringAsync();

                    VendedorActual = JsonSerializer.Deserialize<Usuario>(
                        vendedorJson,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });


                    if (VendedorActual != null)
                    {
                        NombreVendedorLabel.Text =
                            $"Nombre: {VendedorActual.Nombres} {VendedorActual.Apellidos}";

                        CorreoVendedorLabel.Text =
                            $"Correo: {VendedorActual.Correo}";

                        if (noDisponible)
                        {
                            AccionProductoButton.Text = "Publicación no disponible";
                            AccionProductoButton.IsEnabled = false;
                        }
                        else if (SesionUsuario.EstaLogueado &&
                        SesionUsuario.UsuarioActual != null &&
                        SesionUsuario.UsuarioActual.IdUsuario == VendedorActual.IdUsuario)
                        {
                            AccionProductoButton.Text = "🗑 Eliminar publicación";
                            AccionProductoButton.IsEnabled = true;

                            RegistrarVentaButton.IsVisible =
                                ProductoActual != null &&
                                ProductoActual.Activo &&
                                ProductoActual.Estado != "Vendido";
                        }
                        else if (SesionUsuario.EstaLogueado)
                        {
                            AccionProductoButton.Text = "📩 Contactar vendedor";
                            AccionProductoButton.IsEnabled = true;

                            try
                            {
                                string conversacionUrl =
                                    $"{ApiConfig.BaseUrl}/api/Conversaciones/Producto/{producto.IdProducto}";

                                using var conversacionResponse =
    await Api.EnviarAutenticadoAsync(
        () => new HttpRequestMessage(HttpMethod.Get, conversacionUrl));

                                if (conversacionResponse.IsSuccessStatusCode)
                                {
                                    var jsonConversacion =
                                        await conversacionResponse.Content.ReadAsStringAsync();

                                    var conversacion = JsonSerializer.Deserialize<Conversacion>(
                                        jsonConversacion,
                                        new JsonSerializerOptions
                                        {
                                            PropertyNameCaseInsensitive = true
                                        });

                                    if (conversacion != null)
                                    {
                                        AccionProductoButton.Text = "💬 Seguir conversación";
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    $"No se pudo comprobar la conversación: {ex.Message}");
                            }
                        }
                        else
                        {
                            AccionProductoButton.Text = "📩 Contactar vendedor";
                        }
                    }
                }
                else
                {
                    string error = await vendedorResponse.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error al cargar vendedor",
                        $"Código HTTP: {(int)vendedorResponse.StatusCode}\n{error}",
                        "OK");
                }

                // Mostrar favoritos después de cargar la información del producto.
                FavoritoButton.IsVisible =
                ProductoActual != null &&
                (!SesionUsuario.EstaLogueado ||
                  SesionUsuario.UsuarioActual == null ||
                  SesionUsuario.UsuarioActual.IdUsuario != ProductoActual.IdUsuario);

                if (ProductoActual != null)
                {
                    bool productoDisponible =
                        ProductoActual.Activo &&
                        ProductoActual.Estado == "Disponible" &&
                        ProductoActual.CantidadDisponible > 0;

                    bool esVendedor =
                        SesionUsuario.EstaLogueado &&
                        SesionUsuario.UsuarioActual != null &&
                        SesionUsuario.UsuarioActual.IdUsuario == ProductoActual.IdUsuario;

                    AgregarCarritoButton.IsVisible =
                        productoDisponible && !esVendedor;

                    SelectorCantidadLayout.IsVisible =
                        productoDisponible &&
                        !esVendedor &&
                        !ProductoActual.EsProductoUnico &&
                        ProductoActual.CantidadDisponible > 1;

                    CantidadSeleccionada = 1;
                    CantidadSeleccionadaLabel.Text = "1";

                    DisminuirCantidadButton.IsEnabled = false;

                    AumentarCantidadButton.IsEnabled =
                        SelectorCantidadLayout.IsVisible &&
                        ProductoActual.CantidadDisponible > 1;
                }

                await ComprobarConversacionesPublicacion();
                await ComprobarFavorito();
                await ComprobarCarrito();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error de conexión",
                    ex.Message,
                    "OK");
            }
        }

        private async Task<bool> ManejarSesionExpiradaAsync(
        HttpResponseMessage response)
        {
            if ((int)response.StatusCode != 401)
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

        private async Task ComprobarConversacionesPublicacion()
        {
            try
            {
                // Ocultar el botón por defecto.
                ConversacionesPublicacionButton.IsVisible = false;

                // Solo el vendedor puede ver este botón.
                if (!SesionUsuario.EstaLogueado ||
                     SesionUsuario.UsuarioActual == null ||
                     ProductoActual == null ||
                     VendedorActual == null ||
                     !ProductoActual.Activo ||
                     ProductoActual.Estado == "Vendido" ||
                     SesionUsuario.UsuarioActual.IdUsuario != VendedorActual.IdUsuario)
                {
                    return;
                }

                string url = $"{ApiConfig.BaseUrl}/api/Conversaciones/Usuario";

                using var response = await Api.EnviarAutenticadoAsync(
                    () => new HttpRequestMessage(HttpMethod.Get, url));

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"No se pudieron comprobar las conversaciones: {(int)response.StatusCode}");
                    return;
                }

                string json = await response.Content.ReadAsStringAsync();

                var conversaciones = JsonSerializer.Deserialize<List<Conversacion>>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (conversaciones == null)
                    return;

                // Mostrar el botón si existe al menos una conversación
                // relacionada con la publicación actual.
                ConversacionesPublicacionButton.IsVisible =
                    conversaciones.Any(c =>
                        c.IdProducto == ProductoActual.IdProducto &&
                        c.IdUsuarioVendedor == VendedorActual.IdUsuario);
            }
            catch (Exception ex)
            {
                ConversacionesPublicacionButton.IsVisible = false;

                System.Diagnostics.Debug.WriteLine(
                    $"Error al comprobar conversaciones: {ex.Message}");
            }
        }

        private async Task ComprobarFavorito()
        {
            try
            {
                if (!SesionUsuario.EstaLogueado)
                {
                    IdFavoritoActual = null;
                    FavoritoButton.Text = "♡ Agregar a favoritos";
                    return;
                }

                string url = $"{ApiConfig.BaseUrl}/api/Favoritos";

                using var response = await Api.EnviarAutenticadoAsync(
                    () => new HttpRequestMessage(HttpMethod.Get, url));

                if (!response.IsSuccessStatusCode)
                {
                    return;
                }

                string json = await response.Content.ReadAsStringAsync();

                var favoritos = JsonSerializer.Deserialize<List<Favorito>>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (favoritos == null)
                {
                    return;
                }

                int idUsuario = SesionUsuario.UsuarioActual!.IdUsuario;
                int idProductoActual = int.Parse(IdProducto);

                var favorito = favoritos.FirstOrDefault(f =>
                    f.IdUsuario == idUsuario &&
                    f.IdProducto == idProductoActual);

                if (favorito != null)
                {
                    IdFavoritoActual = favorito.IdFavorito;
                    FavoritoButton.Text = "♥ Eliminar de favoritos";
                }
                else
                {
                    IdFavoritoActual = null;
                    FavoritoButton.Text = "♡ Agregar a favoritos";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudo comprobar el favorito.\n\n{ex.Message}",
                    "OK");
            }
        }

        private async void OnAgregarFavoritoClicked(object sender, EventArgs e)
        {
            try
            {
                FavoritoButton.IsEnabled = false;
                if (!SesionUsuario.EstaLogueado)
                {
                    await Shell.Current.GoToAsync(nameof(RequiereCuentaPage));
                    return;
                }

                if (IdFavoritoActual.HasValue)
                {
                    string eliminarUrl =
    $"{ApiConfig.BaseUrl}/api/Favoritos/{IdFavoritoActual.Value}";

                    using var responseEliminar = await Api.EnviarAutenticadoAsync(
                        () => new HttpRequestMessage(HttpMethod.Delete, eliminarUrl));

                    if (responseEliminar.IsSuccessStatusCode)
                    {
                        IdFavoritoActual = null;

                        FavoritoButton.Text = "♡ Agregar a favoritos";

                        await DisplayAlertAsync(
                            "Favoritos",
                            "El producto se eliminó de favoritos.",
                            "OK");
                    }
                    else
                    {
                        string mensaje = await responseEliminar.Content.ReadAsStringAsync();

                        await DisplayAlertAsync(
                            "Favoritos",
                            $"No se pudo eliminar el producto de favoritos.\n\n{mensaje}",
                            "OK");
                    }

                    return;
                }

                if (ProductoActual == null ||
    !ProductoActual.Activo ||
    ProductoActual.Estado == "Vendido")
                {
                    await DisplayAlertAsync(
                        "Producto no disponible",
                        "No puedes agregar a favoritos una publicación que ya no está disponible.",
                        "OK");

                    return;
                }

                var favorito = new
                {
                    IdUsuario = SesionUsuario.UsuarioActual!.IdUsuario,
                    IdProducto = int.Parse(IdProducto)
                };

                string agregarUrl = $"{ApiConfig.BaseUrl}/api/Favoritos";

                using var responseAgregar = await Api.EnviarAutenticadoAsync(
                    () => new HttpRequestMessage(HttpMethod.Post, agregarUrl)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(favorito),
                            System.Text.Encoding.UTF8,
                            "application/json")
                    });

                if (responseAgregar.IsSuccessStatusCode)
                {
                    string respuestaJson =
                        await responseAgregar.Content.ReadAsStringAsync();

                    var favoritoCreado = JsonSerializer.Deserialize<Favorito>(
                        respuestaJson,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (favoritoCreado != null)
                    {
                        IdFavoritoActual = favoritoCreado.IdFavorito;
                    }

                    FavoritoButton.Text = "♥ Eliminar de favoritos";

                    await DisplayAlertAsync(
                        "Favoritos",
                        "El producto se agregó a favoritos.",
                        "OK");
                }
                else
                {
                    string mensaje =
                        await responseAgregar.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Favoritos",
                        $"No se pudo agregar el producto.\n\n{mensaje}",
                        "OK");
                }
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
                FavoritoButton.IsEnabled = true;
            }
        }

        private async void OnAccionProductoClicked(object sender, EventArgs e)
        {
            if (ProductoActual == null)
            {
                await DisplayAlertAsync(
                    "Error",
                    "No se encontró la información del producto.",
                    "OK");
                return;
            }

            if (!ProductoActual.Activo || ProductoActual.Estado == "Vendido")
            {
                await DisplayAlertAsync(
                    "Producto no disponible",
                    "Esta publicación ya no está disponible para contactar al vendedor.",
                    "OK");

                AccionProductoButton.IsEnabled = false;
                AccionProductoButton.Text = "Publicación no disponible";
                return;
            }

            if (VendedorActual == null)
            {
                await DisplayAlertAsync(
                    "Error",
                    "No se cargaron los datos del vendedor.",
                    "OK");
                return;
            }

            AccionProductoButton.IsEnabled = false;

            try
            {

                // Si el usuario actual es el vendedor, puede eliminar su publicación.
                if (SesionUsuario.EstaLogueado &&
                    SesionUsuario.UsuarioActual != null &&
                    SesionUsuario.UsuarioActual.IdUsuario == VendedorActual.IdUsuario)
                {
                    bool confirmar = await DisplayAlertAsync(
                        "Eliminar publicación",
                        $"¿Quieres eliminar la publicación \"{NombreProductoLabel.Text}\"?",
                        "Sí",
                        "No");

                    if (!confirmar)
                        return;

                    string eliminarUrl =
    $"{ApiConfig.BaseUrl}/api/Productos/{ProductoActual.IdProducto}";

                    using var response = await Api.EnviarAutenticadoAsync(
                        () => new HttpRequestMessage(HttpMethod.Delete, eliminarUrl));

                    if (response.IsSuccessStatusCode)
                    {
                        await DisplayAlertAsync(
                            "Publicación eliminada",
                            "La publicación se eliminó correctamente.",
                            "OK");

                        await Shell.Current.GoToAsync("//MainPage");
                    }
                    else
                    {
                        string mensaje = await response.Content.ReadAsStringAsync();

                        await DisplayAlertAsync(
                            "Error",
                            $"No se pudo eliminar la publicación.\n\n{mensaje}",
                            "OK");
                    }

                    return;
                }

                // Si no ha iniciado sesión, debe acceder primero a su cuenta.
                if (!SesionUsuario.EstaLogueado)
                {
                    await Shell.Current.GoToAsync(
                        $"{nameof(RequiereCuentaPage)}?esConversacion=true");
                    return;
                }

                if (SesionUsuario.UsuarioActual == null)
                {
                    await DisplayAlertAsync(
                        "Sesión requerida",
                        "No se pudo obtener la información de tu cuenta. Inicia sesión nuevamente.",
                        "OK");
                    return;
                }

                // Consultar si ya existe una conversación con este vendedor.
                string urlConversacion =
    $"{ApiConfig.BaseUrl}/api/Conversaciones/Producto/{ProductoActual.IdProducto}";

                using var respuestaConversacion = await Api.EnviarAutenticadoAsync(
                    () => new HttpRequestMessage(HttpMethod.Get, urlConversacion));

                if (respuestaConversacion.StatusCode ==
                    System.Net.HttpStatusCode.OK)
                {
                    string jsonConversacion =
                        await respuestaConversacion.Content.ReadAsStringAsync();

                    var conversacion =
                        JsonSerializer.Deserialize<Conversacion>(
                            jsonConversacion,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                    if (conversacion == null)
                    {
                        await DisplayAlertAsync(
                            "Error",
                            "No se pudo cargar la conversación existente.",
                            "OK");
                        return;
                    }

                    await Shell.Current.GoToAsync(
                        $"{nameof(ChatPage)}?idConversacion={conversacion.IdConversacion}");
                }
                else if (respuestaConversacion.StatusCode ==
                         System.Net.HttpStatusCode.NoContent)
                {
                    await Shell.Current.GoToAsync(
                        $"{nameof(ChatPage)}?idProducto={ProductoActual.IdProducto}&idUsuarioVendedor={VendedorActual.IdUsuario}");
                }
                else
                {
                    string detalle =
                        await respuestaConversacion.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error al consultar conversación",
                        $"Código HTTP: {(int)respuestaConversacion.StatusCode}\n" +
                        $"Descripción: {respuestaConversacion.ReasonPhrase}\n\n" +
                        $"Respuesta de la API:\n{detalle}",
                        "OK");
                }
            }
            catch (TaskCanceledException)
            {
                await DisplayAlertAsync(
                    "Tiempo de espera agotado",
                    "La solicitud tardó demasiado. Comprueba que la API esté funcionando.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudo completar la acción.\n\n{ex.Message}",
                    "OK");
            }
            finally
            {
                AccionProductoButton.IsEnabled = true;
            }
        }

        private async void OnConversacionesPublicacionClicked(
            object sender,
            EventArgs e)
        {
            if (!SesionUsuario.EstaLogueado ||
                ProductoActual == null ||
                VendedorActual == null)
            {
                await DisplayAlertAsync(
                    "Error",
                    "No se pudo comprobar la información de la publicación.",
                    "OK");

                return;
            }

            if (SesionUsuario.UsuarioActual!.IdUsuario !=
                VendedorActual.IdUsuario)
            {
                await DisplayAlertAsync(
                    "Acceso denegado",
                    "Solo el vendedor puede consultar estas conversaciones.",
                    "OK");

                return;
            }

            await Shell.Current.GoToAsync(
                $"{nameof(ConversacionesPublicacionPage)}?idProducto={ProductoActual.IdProducto}");
        }

        private class CompradorDisponibleDto
        {
            public int IdUsuario { get; set; }
            public string Nombre { get; set; } = string.Empty;
        }

        private async void OnRegistrarVentaClicked(object sender, EventArgs e)
        {
            if (!SesionUsuario.EstaLogueado ||
                string.IsNullOrWhiteSpace(SesionUsuario.Token) ||
                SesionUsuario.UsuarioActual == null ||
                ProductoActual == null ||
                VendedorActual == null)
            {
                await DisplayAlertAsync(
                    "Error",
                    "No se pudo comprobar la sesión o el producto.",
                    "OK");
                return;
            }

            if (SesionUsuario.UsuarioActual.IdUsuario != VendedorActual.IdUsuario)
            {
                await DisplayAlertAsync(
                    "Acceso denegado",
                    "Solo el vendedor puede registrar esta venta.",
                    "OK");
                return;
            }

            if (!ProductoActual.Activo ||
                ProductoActual.Estado == "Vendido")
            {
                await DisplayAlertAsync(
                    "Producto no disponible",
                    "Esta publicación ya no está disponible.",
                    "OK");
                RegistrarVentaButton.IsVisible = false;
                return;
            }

            RegistrarVentaButton.IsEnabled = false;

            try
            {

                string compradoresUrl =
                    $"{ApiConfig.BaseUrl.TrimEnd('/')}/api/Transacciones/Compradores/{ProductoActual.IdProducto}";

                using var ctsCompradores =
                    new CancellationTokenSource(TimeSpan.FromSeconds(30));

                using var respuestaCompradores =
    await Api.EnviarAutenticadoAsync(
        () => new HttpRequestMessage(HttpMethod.Get, compradoresUrl),
        ctsCompradores.Token);

                if (!respuestaCompradores.IsSuccessStatusCode)
                {
                    string detalle =
                        await respuestaCompradores.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error",
                        $"No se pudieron consultar los compradores.\n" +
                        $"HTTP {(int)respuestaCompradores.StatusCode}\n{detalle}",
                        "OK");
                    return;
                }

                var compradores =
                    await respuestaCompradores.Content
                        .ReadFromJsonAsync<List<CompradorDisponibleDto>>(
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                if (compradores == null || compradores.Count == 0)
                {
                    await DisplayAlertAsync(
                        "Sin compradores",
                        "Todavía no hay usuarios con conversaciones sobre esta publicación. " +
                        "No se puede registrar la venta hasta que haya un comprador disponible.",
                        "OK");
                    return;
                }

                var opciones = compradores
                    .Select(c => $"{c.Nombre} (ID: {c.IdUsuario})")
                    .ToArray();

                string? seleccion = await DisplayActionSheetAsync(
                    "¿Quién compró el producto?",
                    "Cancelar",
                    null,
                    opciones);

                if (string.IsNullOrWhiteSpace(seleccion) ||
                    seleccion == "Cancelar")
                {
                    return;
                }

                int indice = Array.IndexOf(opciones, seleccion);

                if (indice < 0)
                    return;

                int idComprador = compradores[indice].IdUsuario;

                bool confirmar = await DisplayAlertAsync(
                    "Confirmar venta",
                    $"¿Confirmas que vendiste \"{ProductoActual.Nombre}\" " +
                    $"a {compradores[indice].Nombre} por ${ProductoActual.Precio:N2}?",
                    "Sí, registrar",
                    "Cancelar");

                if (!confirmar)
                    return;

                var datosVenta = new
                {
                    IdProducto = ProductoActual.IdProducto,
                    IdUsuarioComprador = idComprador
                };

                using var ctsVenta =
                new CancellationTokenSource(TimeSpan.FromSeconds(30));

                string registrarVentaUrl =
    $"{ApiConfig.BaseUrl.TrimEnd('/')}/api/Transacciones";

                using var respuestaVenta = await Api.EnviarAutenticadoAsync(
                    () => new HttpRequestMessage(HttpMethod.Post, registrarVentaUrl)
                    {
                        Content = JsonContent.Create(datosVenta)
                    },
                    ctsVenta.Token);

                if (!respuestaVenta.IsSuccessStatusCode)
                {
                    string detalle =
                        await respuestaVenta.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "No se pudo registrar",
                        $"La API rechazó la venta.\n" +
                        $"HTTP {(int)respuestaVenta.StatusCode}\n{detalle}",
                        "OK");
                    return;
                }

                await CargarProductoAsyncYActualizar();

                await DisplayAlertAsync(
                    "Venta registrada",
                    "La venta se registró correctamente. Se ha actualizado el inventario de la publicación y la venta aparecerá en tu historial.",
                    "OK");
            }
            catch (TaskCanceledException)
            {
                await DisplayAlertAsync(
                    "Tiempo de espera agotado",
                    "La solicitud tardó demasiado. Comprueba que la API esté funcionando.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudo completar el registro de la venta.\n{ex.Message}",
                    "OK");
            }
            finally
            {
                RegistrarVentaButton.IsEnabled = true;
            }
        }

        private async Task CargarProductoAsyncYActualizar()
        {
            await Shell.Current.GoToAsync(
                $"{nameof(ProductoDetallePage)}?idProducto={ProductoActual!.IdProducto}");
        }

        private async void OnBorrarPublicacionClicked(object sender, EventArgs e)
        {
            if (ProductoActual == null ||
                VendedorActual == null ||
                SesionUsuario.UsuarioActual == null ||
                SesionUsuario.UsuarioActual.IdUsuario != ProductoActual.IdUsuario ||
                ProductoActual.Estado != "Vendido")
            {
                await DisplayAlertAsync(
                    "Acción no permitida",
                    "Solo el vendedor puede borrar una publicación vendida.",
                    "OK");
                return;
            }

            bool confirmar = await DisplayAlertAsync(
                "Borrar publicación",
                "La publicación dejará de estar disponible. Se conservará el historial de la venta. ¿Deseas continuar?",
                "Sí, borrar",
                "Cancelar");

            if (!confirmar)
                return;

            try
            {
                string url =
    $"{ApiConfig.BaseUrl}/api/Productos/{ProductoActual.IdProducto}/BorrarPublicacionVendida";

                using var response = await Api.EnviarAutenticadoAsync(
                    () => new HttpRequestMessage(HttpMethod.Delete, url));

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync(
                        "Publicación borrada",
                        "La publicación se retiró y el historial de venta se conservó.",
                        "OK");

                    await Shell.Current.GoToAsync("//MainPage");
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error",
                        $"No se pudo borrar la publicación.\n{error}",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async void OnVolverPublicarClicked(object sender, EventArgs e)
        {
            if (ProductoActual == null ||
                SesionUsuario.UsuarioActual == null ||
                SesionUsuario.UsuarioActual.IdUsuario != ProductoActual.IdUsuario ||
                ProductoActual.Estado != "Vendido")
            {
                await DisplayAlertAsync(
                    "Acción no permitida",
                    "Solo el vendedor puede volver a publicar un producto vendido.",
                    "OK");
                return;
            }

            bool confirmar = await DisplayAlertAsync(
                "Volver a publicar",
                "Se creará una publicación nueva con los datos del producto. " +
                "Podrás revisar y modificar la información y las imágenes antes de publicarla. " +
                "La publicación vendida y su historial se conservarán.",
                "Continuar",
                "Cancelar");

            if (!confirmar)
                return;

            var parametros = new Dictionary<string, object>
{
    { "IdProductoOrigen", ProductoActual.IdProducto }
};

            System.Diagnostics.Debug.WriteLine(
                $"[VolverPublicar] Navegando. ID enviado: {ProductoActual.IdProducto}");

            System.Diagnostics.Debug.WriteLine(
                $"[VolverPublicar] Ruta: {nameof(PublicarProductoPage)}");

            await Shell.Current.GoToAsync(
                nameof(PublicarProductoPage),
                parametros);

            System.Diagnostics.Debug.WriteLine(
                "[VolverPublicar] Navegación completada");

        }

        private async void OnReactivarPublicacionClicked(object sender, EventArgs e)
        {
            if (ProductoActual == null ||
                SesionUsuario.UsuarioActual == null ||
                SesionUsuario.UsuarioActual.IdUsuario != ProductoActual.IdUsuario ||
                ProductoActual.Estado != "Vendido" ||
                !ProductoActual.Activo)
            {
                await DisplayAlertAsync(
                    "Acción no permitida",
                    "Solo el vendedor puede reactivar una publicación vendida.",
                    "OK");
                return;
            }

            bool confirmar = await DisplayAlertAsync(
                "Reactivar publicación",
                "La publicación original volverá a estar disponible con el mismo ID. " +
                "¿Deseas continuar?",
                "Reactivar",
                "Cancelar");

            if (!confirmar)
                return;

            try
            {
                string url =
    $"{ApiConfig.BaseUrl.TrimEnd('/')}/api/Productos/{ProductoActual.IdProducto}/Reactivar";

                using var respuesta = await Api.EnviarAutenticadoAsync(
                    () => new HttpRequestMessage(HttpMethod.Put, url));

                string contenido = await respuesta.Content.ReadAsStringAsync();

                if (respuesta.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync(
                        "Publicación reactivada",
                        "La publicación original está disponible nuevamente.",
                        "OK");

                    CargarProducto();
                }
                else
                {
                    await DisplayAlertAsync(
                        "No se pudo reactivar",
                        string.IsNullOrWhiteSpace(contenido)
                            ? $"La API respondió: {(int)respuesta.StatusCode}."
                            : contenido,
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error de conexión",
                    $"No se pudo contactar con la API: {ex.Message}",
                    "OK");
            }
        }

        private async void OnMarcarDisponibleClicked(object sender, EventArgs e)
        {
            if (ProductoActual == null ||
                SesionUsuario.UsuarioActual == null ||
                SesionUsuario.UsuarioActual.IdUsuario != ProductoActual.IdUsuario ||
                ProductoActual.Estado != "Reservado")
            {
                await DisplayAlertAsync(
                    "Acción no permitida",
                    "Solo el vendedor puede cambiar una publicación reservada.",
                    "OK");
                return;
            }

            bool confirmar = await DisplayAlertAsync(
                "Marcar como disponible",
                "¿Quieres que esta publicación vuelva a estar disponible?",
                "Sí",
                "Cancelar");

            if (!confirmar)
                return;

            try
            {
                string url =
    $"{ApiConfig.BaseUrl}/api/Productos/{ProductoActual.IdProducto}/Disponible";

                using var response = await Api.EnviarAutenticadoAsync(
                    () => new HttpRequestMessage(HttpMethod.Put, url));

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync(
                        "Estado actualizado",
                        "La publicación volvió a estar disponible.",
                        "OK");

                    await CargarProductoAsyncYActualizar();
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "Error",
                        $"No se pudo actualizar el estado.\n{error}",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async void OnEditarPublicacionClicked(
            object sender,
            EventArgs e)
        {
            if (!SesionUsuario.EstaLogueado ||
                SesionUsuario.UsuarioActual == null ||
                ProductoActual == null)
            {
                await DisplayAlertAsync(
                    "Sesión requerida",
                    "Debes iniciar sesión para editar una publicación.",
                    "OK");
                return;
            }

            if (SesionUsuario.UsuarioActual.IdUsuario != ProductoActual.IdUsuario ||
                !ProductoActual.Activo ||
                ProductoActual.Estado != "Disponible")
            {
                await DisplayAlertAsync(
                    "Acción no permitida",
                    "Solo puedes editar tus propias publicaciones activas y disponibles.",
                    "OK");
                return;
            }

            await Shell.Current.GoToAsync(
                $"{nameof(EditarProductoPage)}?idProducto={ProductoActual.IdProducto}");
        }


        private async void OnVerPerfilVendedorClicked(
            object sender,
            EventArgs e)
        {
            if (VendedorActual == null ||
                VendedorActual.IdUsuario <= 0)
            {
                await DisplayAlertAsync(
                    "Perfil no disponible",
                    "No se pudo obtener la información del vendedor.",
                    "OK");

                return;
            }

            await Shell.Current.GoToAsync(
                nameof(PerfilPublicoPage),
                new Dictionary<string, object>
                {
                    ["IdUsuario"] = VendedorActual.IdUsuario.ToString()
                });
        }
        private void OnDisminuirCantidadClicked(object sender, EventArgs e)
        {
            if (CantidadSeleccionada > 1)
            {
                CantidadSeleccionada--;
                CantidadSeleccionadaLabel.Text = CantidadSeleccionada.ToString();
            }

            DisminuirCantidadButton.IsEnabled = CantidadSeleccionada > 1;

            AumentarCantidadButton.IsEnabled =
                ProductoActual != null &&
                CantidadSeleccionada < ProductoActual.CantidadDisponible;
        }

        private void OnAumentarCantidadClicked(object sender, EventArgs e)
        {
            if (ProductoActual == null)
                return;

            if (CantidadSeleccionada < ProductoActual.CantidadDisponible)
            {
                CantidadSeleccionada++;
                CantidadSeleccionadaLabel.Text = CantidadSeleccionada.ToString();
            }

            DisminuirCantidadButton.IsEnabled = CantidadSeleccionada > 1;

            AumentarCantidadButton.IsEnabled =
                CantidadSeleccionada < ProductoActual.CantidadDisponible;
        }
        private async void OnAgregarCarritoClicked(object sender, EventArgs e)
        {
            try
            {
                if (!SesionUsuario.EstaLogueado)
                {
                    await DisplayAlertAsync(
                        "Iniciar sesión",
                        "Debes iniciar sesión para agregar productos al carrito.",
                        "OK");

                    await Shell.Current.GoToAsync(
                        nameof(RequiereCuentaPage));

                    return;
                }

                if (ProductoActual == null)
                {
                    await DisplayAlertAsync(
                        "Carrito",
                        "No se pudo obtener la información del producto.",
                        "OK");

                    return;
                }

                if (!ProductoActual.Activo ||
                    ProductoActual.Estado != "Disponible" ||
                    ProductoActual.CantidadDisponible < 1)
                {
                    await DisplayAlertAsync(
                        "Producto no disponible",
                        "Este producto ya no está disponible.",
                        "OK");

                    return;
                }

                if (SesionUsuario.UsuarioActual == null ||
                    SesionUsuario.UsuarioActual.IdUsuario == ProductoActual.IdUsuario)
                {
                    await DisplayAlertAsync(
                        "Carrito",
                        "No puedes agregar tu propia publicación al carrito.",
                        "OK");

                    return;
                }

                if (CantidadSeleccionada < 1 ||
                    CantidadSeleccionada > ProductoActual.CantidadDisponible)
                {
                    await DisplayAlertAsync(
                        "Cantidad no válida",
                        "La cantidad seleccionada supera las unidades disponibles.",
                        "OK");

                    return;
                }

                if (ProductoActual.EsProductoUnico &&
                    CantidadSeleccionada != 1)
                {
                    await DisplayAlertAsync(
                        "Cantidad no válida",
                        "Los productos únicos solo permiten una unidad.",
                        "OK");

                    return;
                }

                if (IdCarritoActual.HasValue)
                {
                    bool confirmar = await DisplayAlertAsync(
                        "Quitar del carrito",
                        "¿Quieres quitar este producto de tu carrito?",
                        "Quitar",
                        "Cancelar");

                    if (!confirmar)
                        return;

                    string eliminarUrl =
    $"{ApiConfig.BaseUrl}/api/Carrito/{IdCarritoActual.Value}";

                    using var respuestaEliminar = await Api.EnviarAutenticadoAsync(
                        () => new HttpRequestMessage(HttpMethod.Delete, eliminarUrl));

                    if (!respuestaEliminar.IsSuccessStatusCode)
                    {
                        string error = await respuestaEliminar.Content.ReadAsStringAsync();

                        await DisplayAlertAsync(
                            "Error",
                            $"No se pudo quitar el producto del carrito.\n{error}",
                            "OK");

                        return;
                    }

                    IdCarritoActual = null;
                    ActualizarBotonCarrito();

                    await DisplayAlertAsync(
                        "Carrito actualizado",
                        "El producto se quitó de tu carrito.",
                        "OK");

                    return;
                }

                var datos = new
                {
                    IdProducto = ProductoActual.IdProducto,
                    Cantidad = CantidadSeleccionada
                };

                string agregarUrl = $"{ApiConfig.BaseUrl}/api/Carrito";

                using var respuesta = await Api.EnviarAutenticadoAsync(
                    () => new HttpRequestMessage(HttpMethod.Post, agregarUrl)
                    {
                        Content = JsonContent.Create(datos)
                    });

                if (!respuesta.IsSuccessStatusCode)
                {
                    string error = await respuesta.Content.ReadAsStringAsync();

                    await DisplayAlertAsync(
                        "No se pudo agregar",
                        $"La API rechazó la solicitud.\nCódigo: {(int)respuesta.StatusCode}\n{error}",
                        "OK");

                    return;
                }

                // Reiniciar la selección del detalle.
                // Esto no modifica la cantidad guardada en el carrito.
                CantidadSeleccionada = 1;
                CantidadSeleccionadaLabel.Text = "1";

                DisminuirCantidadButton.IsEnabled = false;

                AumentarCantidadButton.IsEnabled =
                    SelectorCantidadLayout.IsVisible &&
                    ProductoActual != null &&
                    ProductoActual.CantidadDisponible > 1;

                bool verCarrito = await DisplayAlertAsync(
                    "Producto agregado",
                    "El producto se agregó correctamente a tu carrito. ¿Quieres verlo ahora?",
                    "Ver carrito",
                    "Continuar comprando");

                if (verCarrito)
                {
                    await Shell.Current.GoToAsync(nameof(CarritoPage));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"No se pudo agregar el producto al carrito:\n{ex.Message}",
                    "OK");
            }
        }

        private async Task ComprobarCarrito()
        {
            IdCarritoActual = null;

            if (!SesionUsuario.EstaLogueado)
                return;

            try
            {
                string carritoUrl = $"{ApiConfig.BaseUrl}/api/Carrito";

                using var respuesta = await Api.EnviarAutenticadoAsync(
                    () => new HttpRequestMessage(HttpMethod.Get, carritoUrl));

                if (!respuesta.IsSuccessStatusCode)
                    return;

                string json = await respuesta.Content.ReadAsStringAsync();

                var productos = JsonSerializer.Deserialize<List<CarritoItem>>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                var productoEnCarrito = productos?.FirstOrDefault(
                    p => p.IdProducto == ProductoActual?.IdProducto);

                IdCarritoActual = productoEnCarrito?.IdCarrito;

                ActualizarBotonCarrito();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error al comprobar el carrito: {ex.Message}");
            }
        }

        private void ActualizarBotonCarrito()
        {
            if (IdCarritoActual.HasValue)
            {
                AgregarCarritoButton.Text = "🗑 Quitar del carrito";
                AgregarCarritoButton.BackgroundColor = Colors.IndianRed;
            }
            else
            {
                AgregarCarritoButton.Text = "🛒 Agregar al carrito";
                AgregarCarritoButton.BackgroundColor =
                    Color.FromArgb("#218838");
            }
        }
    }
}

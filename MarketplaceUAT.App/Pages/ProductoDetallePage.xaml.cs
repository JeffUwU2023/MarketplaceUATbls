using MarketplaceUAT.App.Config;
using MarketplaceUAT.App.Models;
using System.Text.Json;

namespace MarketplaceUAT.App.Pages
{
    [QueryProperty(nameof(IdProducto), "idProducto")]
    public partial class ProductoDetallePage : ContentPage
    {
        private string idProducto = string.Empty;
        private List<string> ImagenesProducto = new();
        private Usuario? VendedorActual = null;
        private Producto? ProductoActual = null;

        private int? IdFavoritoActual = null;

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

                string url = $"{ApiConfig.BaseUrl}/api/Productos/{IdProducto}";

                var response = await client.GetAsync(url);

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

                NombreProductoLabel.Text = producto.Nombre;
                PrecioProductoLabel.Text = $"${producto.Precio:N2}";
                CondicionProductoLabel.Text = $"Condición: {producto.Condicion}";
                EstadoProductoLabel.Text = $"Estado: {producto.EstadoTexto}";

                string imagenesUrl =
                       $"{ApiConfig.BaseUrl}/api/ImagenesProducto/Producto/{producto.IdProducto}";

                var imagenesResponse = await client.GetAsync(imagenesUrl);

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

                    ImagenesProducto.Clear();

                    if (imagenes != null)
                    {
                        foreach (var imagen in imagenes)
                        {
                            string urlImagen =
                                   $"{ApiConfig.BaseUrl}/Images/{imagen.UrlImagen}";

                            ImagenesProducto.Add(urlImagen);
                        }
                    }

                    ImagenesCarousel.ItemsSource = null;
                    ImagenesCarousel.ItemsSource = ImagenesProducto;

                    bool tieneImagenes = ImagenesProducto.Count > 0;

                    ImagenesCarousel.IsVisible = tieneImagenes;

                    IndicadorImagenes.IsVisible =
                        ImagenesProducto.Count > 1;

                    ImagenPlaceholder.IsVisible = !tieneImagenes;
                }

                string categoriaUrl =
                       $"{ApiConfig.BaseUrl}/api/Categorias/{producto.IdCategoria}";

                var categoriaResponse = await client.GetAsync(categoriaUrl);

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

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        SesionUsuario.Token);

                var vendedorResponse = await client.GetAsync(vendedorUrl);

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

                        MatriculaVendedorLabel.Text =
                            VendedorActual.TipoUsuario == "Alumno"
                                ? $"Matrícula: {VendedorActual.Matricula}"
                                : "Tipo de cuenta: Docente";

                        CorreoVendedorLabel.Text =
                            $"Correo: {VendedorActual.Correo}";

                        if (SesionUsuario.EstaLogueado &&
                            SesionUsuario.UsuarioActual!.IdUsuario == VendedorActual.IdUsuario)
                        {
                            AccionProductoButton.Text = "🗑 Eliminar publicación";
                        }
                        else if (SesionUsuario.EstaLogueado)
                        {
                            AccionProductoButton.Text = "📩 Contactar vendedor";

                            try
                            {
                                string conversacionUrl =
                                    $"{ApiConfig.BaseUrl}/api/Conversaciones/Producto/{producto.IdProducto}";

                                var conversacionResponse =
                                    await client.GetAsync(conversacionUrl);

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

                await ComprobarFavorito();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Error de conexión",
                    ex.Message,
                    "OK");
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

                client.DefaultRequestHeaders.Authorization =
                 new System.Net.Http.Headers.AuthenticationHeaderValue(
                 "Bearer",
                     SesionUsuario.Token);

                string url = $"{ApiConfig.BaseUrl}/api/Favoritos";

                var response = await client.GetAsync(url);

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

                client.DefaultRequestHeaders.Authorization =
    new System.Net.Http.Headers.AuthenticationHeaderValue(
        "Bearer",
        SesionUsuario.Token);

                if (IdFavoritoActual.HasValue)
                {
                    string eliminarUrl =
                           $"{ApiConfig.BaseUrl}/api/Favoritos/{IdFavoritoActual.Value}";

                    var responseEliminar = await client.DeleteAsync(eliminarUrl);

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

                var favorito = new
                {
                    IdUsuario = SesionUsuario.UsuarioActual!.IdUsuario,
                    IdProducto = int.Parse(IdProducto)
                };

                string json = JsonSerializer.Serialize(favorito);

                using var content = new StringContent(
                    json,
                    System.Text.Encoding.UTF8,
                    "application/json");

                string agregarUrl = $"{ApiConfig.BaseUrl}/api/Favoritos";

                var responseAgregar = await client.PostAsync(
                    agregarUrl,
                    content);

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
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    SesionUsuario.Token);

            if (VendedorActual == null)
            {
                await DisplayAlertAsync(
                    "Diagnóstico",
                    "VendedorActual es null. No se cargaron los datos del vendedor.",
                    "OK");

                return;
            }

            AccionProductoButton.IsEnabled = false;

            try
            {
                // Si el usuario actual es el vendedor, puede eliminar su publicación.
                if (SesionUsuario.EstaLogueado &&
                    SesionUsuario.UsuarioActual!.IdUsuario == VendedorActual.IdUsuario)
                {
                    bool confirmar = await DisplayAlertAsync(
                        "Eliminar publicación",
                        $"¿Quieres eliminar la publicación \"{NombreProductoLabel.Text}\"?",
                        "Sí",
                        "No");

                    if (!confirmar)
                        return;

                    if (ProductoActual == null)
                    {
                        await DisplayAlertAsync(
                            "Error",
                            "No se encontró la información del producto.",
                            "OK");

                        return;
                    }

                    try
                    {

                        client.DefaultRequestHeaders.ConnectionClose = true;
                        client.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue(
                                "Bearer",
                                SesionUsuario.Token);

                        string eliminarUrl =
                            $"{ApiConfig.BaseUrl}/api/Productos/{ProductoActual.IdProducto}";

                        var response = await client.DeleteAsync(eliminarUrl);

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
                            string mensaje =
                                await response.Content.ReadAsStringAsync();

                            await DisplayAlertAsync(
                                "Error",
                                $"No se pudo eliminar la publicación.\n\n{mensaje}",
                                "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlertAsync(
                            "Error de conexión",
                            ex.Message,
                            "OK");
                    }

                    return;
                }

                if (!SesionUsuario.EstaLogueado)
                {
                    await Shell.Current.GoToAsync(
                        $"{nameof(RequiereCuentaPage)}?esConversacion=true");

                    return;
                }

                if (ProductoActual == null)
                {
                    await DisplayAlertAsync(
                        "Error",
                        "No se encontró la información del producto.",
                        "OK");

                    return;
                }

                string urlConversacion =
    $"{ApiConfig.BaseUrl}/api/Conversaciones/Producto/{ProductoActual.IdProducto}";

                var respuestaConversacion = await client.GetAsync(urlConversacion);

                if (respuestaConversacion.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string jsonConversacion =
                        await respuestaConversacion.Content.ReadAsStringAsync();

                    var conversacion = System.Text.Json.JsonSerializer.Deserialize<Conversacion>(
                        jsonConversacion,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (conversacion == null)
                    {
                        await DisplayAlertAsync(
                            "Error",
                            "No se pudo cargar la conversación existente.",
                            "Aceptar");
                        return;
                    }

                    await Shell.Current.GoToAsync(
                        $"{nameof(ChatPage)}?idConversacion={conversacion.IdConversacion}");
                }
                else if (respuestaConversacion.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    await Shell.Current.GoToAsync(
                        $"{nameof(ChatPage)}?idProducto={ProductoActual.IdProducto}&idUsuarioVendedor={VendedorActual.IdUsuario}");
                }
                else
                {
                    await DisplayAlertAsync(
                        "Error",
                        "No se pudo comprobar si ya existe una conversación. Inténtalo de nuevo.",
                        "Aceptar");
                }
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
    }
}

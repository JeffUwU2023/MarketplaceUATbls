using MarketplaceUAT.App.Pages;
namespace MarketplaceUAT.App 
{ 
    public partial class AppShell : Shell 
    {
        private bool _sesionComprobada;
        public AppShell() 
        { 
            InitializeComponent();

            Routing.RegisterRoute(
            nameof(ChatPage), 
            typeof(ChatPage));

            Routing.RegisterRoute(
            nameof(ConversacionesPage),
            typeof(ConversacionesPage));

            Routing.RegisterRoute(
            nameof(ConversacionesPublicacionPage),
            typeof(ConversacionesPublicacionPage));


            Routing.RegisterRoute(
            nameof(ProductoDetallePage),
            typeof(ProductoDetallePage));

            Routing.RegisterRoute(
            nameof(LoginPage),
            typeof(LoginPage));

            Routing.RegisterRoute(
            nameof(FavoritosPage),
            typeof(FavoritosPage));

            Routing.RegisterRoute(
            nameof(CarritoPage),
            typeof(CarritoPage));

            Routing.RegisterRoute(
            nameof(RegistroPage),
            typeof(RegistroPage));

            Routing.RegisterRoute(
            nameof(PublicarProductoPage),
            typeof(PublicarProductoPage));

            Routing.RegisterRoute(
            nameof(RequiereCuentaPage),
            typeof(RequiereCuentaPage));

            Routing.RegisterRoute(
            nameof(MisPublicacionesPage),
            typeof(MisPublicacionesPage));

            Routing.RegisterRoute(
            nameof(EditarProductoPage),
            typeof(EditarProductoPage));

            Routing.RegisterRoute(
            nameof(PerfilPage),
            typeof(PerfilPage));

            Routing.RegisterRoute(
            nameof(SearchPage),
            typeof(SearchPage));

            Routing.RegisterRoute(
            nameof(MisComprasPage),
            typeof(MisComprasPage));

            Routing.RegisterRoute(
            nameof(MisVentasPage),
            typeof(MisVentasPage));

            Routing.RegisterRoute(
            nameof(PerfilPublicoPage),
            typeof(PerfilPublicoPage));

            Loaded += OnAppShellLoaded;
        }
        private async void OnAppShellLoaded(object? sender, EventArgs e)
        {
            if (_sesionComprobada)
                return;

            _sesionComprobada = true;

            bool sesionRestaurada =
                await SesionUsuario.RestaurarSesionAsync();

            if (Shell.Current?.CurrentPage is MainPage mainPage)
            {
                mainPage.ActualizarEstadoSesion();
            }

            if (!sesionRestaurada)
            {
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync(nameof(LoginPage));
                }
            }
        }

        public async Task MostrarLoginAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Shell.Current == null)
                    return;

                await Shell.Current.GoToAsync(
                      nameof(LoginPage));
            });
        }
    } 
}

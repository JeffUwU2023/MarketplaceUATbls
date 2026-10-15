using MarketplaceUAT.App.Pages;
namespace MarketplaceUAT.App 
{ 
    public partial class AppShell : Shell 
    { 
        public AppShell() 
        { 
            InitializeComponent();

            Routing.RegisterRoute(nameof(ChatPage), 
            typeof(ChatPage));

            Routing.RegisterRoute(
            nameof(ConversacionesPage),
            typeof(ConversacionesPage));

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
        } 
    } 
}

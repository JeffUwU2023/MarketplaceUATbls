using MarketplaceUAT.App;

namespace MarketplaceUAT.App.Pages
{
    public partial class RequiereCuentaPage : ContentPage, IQueryAttributable
    {
        public RequiereCuentaPage()
        {
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("esConversacion", out var valor) &&
                valor?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            {
                MensajeLabel.Text =
                    "Para iniciar una conversación necesitas iniciar sesión o crear una cuenta.";
            }
        }

        private async void OnIniciarSesionClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(LoginPage));
        }

        private async void OnCrearCuentaClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(RegistroPage));
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
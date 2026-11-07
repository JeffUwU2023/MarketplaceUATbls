namespace MarketplaceUAT.App.Models
{
    public class LoginResponse
    {
        public Usuario Usuario { get; set; } = new();

        public string Token { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;
    }
}

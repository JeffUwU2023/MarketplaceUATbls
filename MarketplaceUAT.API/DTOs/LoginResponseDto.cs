namespace MarketplaceUAT.API.DTOs
{
    public class LoginResponseDto
    {
        public UsuarioResponseDto Usuario { get; set; } = new();

        public string Token { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;
    }
}
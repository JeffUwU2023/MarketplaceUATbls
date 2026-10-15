namespace MarketplaceUAT.API.DTOs
{
    public class LoginResponseDto
    {
        public UsuarioResponseDto Usuario { get; set; } = new();

        public string Token { get; set; } = string.Empty;
    }
}
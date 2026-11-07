namespace MarketplaceUAT.Data.Models
{
    public class RefreshToken
    {
        public int IdRefreshToken { get; set; }

        public int IdUsuario { get; set; }

        public Guid IdFamilia { get; set; }

        public string TokenHash { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; }

        public DateTime FechaExpiracion { get; set; }

        public DateTime? FechaRevocacion { get; set; }

        public string? TokenReemplazo { get; set; }

        public string? RespuestaRenovacionCifrada { get; set; }

        public DateTime? FechaLimiteReintento { get; set; }
    }
}

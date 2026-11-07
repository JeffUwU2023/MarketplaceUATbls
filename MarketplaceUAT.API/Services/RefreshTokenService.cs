using System.Security.Cryptography;
using System.Text;
using MarketplaceUAT.Data.Models;
using Microsoft.AspNetCore.DataProtection;

namespace MarketplaceUAT.API.Services
{
    public class RefreshTokenService
    {
        private readonly IDataProtector _protector;

        public RefreshTokenService(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector(
                "MarketplaceUAT.RefreshToken.Renovacion.v1");
        }

        public string GenerarRefreshToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(64);

            return Convert.ToBase64String(bytes);
        }

        public string CalcularHash(string token)
        {
            byte[] bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(token));

            return Convert.ToHexString(bytes);
        }

        public string ProtegerRespuesta(string respuesta)
        {
            return _protector.Protect(respuesta);
        }

        public string DesprotegerRespuesta(string respuestaCifrada)
        {
            return _protector.Unprotect(respuestaCifrada);
        }

        public RefreshToken CrearRegistro(
    int idUsuario,
    string refreshToken,
    DateTime fechaExpiracion,
    Guid idFamilia)
        {
            return new RefreshToken
            {
                IdUsuario = idUsuario,
                IdFamilia = idFamilia,
                TokenHash = CalcularHash(refreshToken),
                FechaCreacion = DateTime.UtcNow,
                FechaExpiracion = fechaExpiracion
            };
        }
    }
}

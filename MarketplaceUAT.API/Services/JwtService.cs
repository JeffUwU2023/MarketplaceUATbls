using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MarketplaceUAT.API.Services
{
    public class JwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerarToken(
        int idUsuario,
        string correo,
        bool esAdministrador)
        {
            if (idUsuario <= 0)
            {
                throw new ArgumentException(
                    "El identificador del usuario no es válido.",
                    nameof(idUsuario)
                );
            }

            if (string.IsNullOrWhiteSpace(correo))
            {
                throw new ArgumentException(
                    "El correo del usuario es obligatorio.",
                    nameof(correo)
                );
            }

            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];
            var expirationConfig =
                _configuration["Jwt:ExpirationMinutes"];

            if (string.IsNullOrWhiteSpace(jwtKey) ||
                Encoding.UTF8.GetByteCount(jwtKey) < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:Key no está configurada correctamente."
                );
            }

            if (string.IsNullOrWhiteSpace(jwtIssuer))
            {
                throw new InvalidOperationException(
                    "Falta configurar Jwt:Issuer."
                );
            }

            if (string.IsNullOrWhiteSpace(jwtAudience))
            {
                throw new InvalidOperationException(
                    "Falta configurar Jwt:Audience."
                );
            }

            if (!double.TryParse(
                    expirationConfig,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var expirationMinutes) ||
                expirationMinutes <= 0 ||
                double.IsNaN(expirationMinutes) ||
                double.IsInfinity(expirationMinutes))
            {
                throw new InvalidOperationException(
                    "Jwt:ExpirationMinutes debe ser un número válido mayor que cero."
                );
            }

            var claims = new List<Claim>
            {
            new Claim(
            ClaimTypes.NameIdentifier,
            idUsuario.ToString(CultureInfo.InvariantCulture)
            ),
            new Claim(
            ClaimTypes.Email,
             correo
            )
            };

            if (esAdministrador)
            {
                claims.Add(
                    new Claim(ClaimTypes.Role, "Administrador")
                );
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            );

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var ahora = DateTime.UtcNow;
            var expiracion = ahora.AddMinutes(expirationMinutes);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                notBefore: ahora,
                expires: expiracion,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}
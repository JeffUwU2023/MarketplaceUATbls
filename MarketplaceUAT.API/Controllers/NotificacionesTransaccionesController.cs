using MarketplaceUAT.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketplaceUAT.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificacionesTransaccionesController : ControllerBase
    {
        private readonly MarketplaceContext _context;

        public NotificacionesTransaccionesController(
            MarketplaceContext context)
        {
            _context = context;
        }

        // Obtener las notificaciones del usuario autenticado
        [HttpGet]
        public async Task<IActionResult> ObtenerNotificaciones()
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario) ||
                idUsuario <= 0)
            {
                return Unauthorized();
            }

            var notificaciones = await _context.NotificacionesTransacciones
             .AsNoTracking()
             .Where(n =>
    n.IdUsuario == idUsuario &&
    !_context.Transacciones.Any(t =>
        t.IdTransaccion == n.IdTransaccion &&
        (t.EstadoTransaccion == "Completada" ||
         t.EstadoTransaccion == "Cancelada")))
             .OrderByDescending(n => n.FechaCreacion)
             .ThenByDescending(n => n.IdNotificacion)
             .Select(n => new
                {
                    n.IdNotificacion,
                    n.IdTransaccion,
                    n.Tipo,
                    n.Mensaje,
                    n.FechaCreacion,
                    n.Leida
                })
                .ToListAsync();

            return Ok(notificaciones);
        }

        // Contar las notificaciones pendientes de leer
        [HttpGet("NoLeidas/Conteo")]
        public async Task<IActionResult> ObtenerConteoNoLeidas()
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario) ||
                idUsuario <= 0)
            {
                return Unauthorized();
            }

            var cantidad = await _context.NotificacionesTransacciones
                .CountAsync(n =>
                    n.IdUsuario == idUsuario &&
                    !n.Leida);

            return Ok(new
            {
                cantidad
            });
        }

        // Marcar como leída una notificación propia
        [HttpPut("{idNotificacion}/Leer")]
        public async Task<IActionResult> MarcarComoLeida(
            int idNotificacion)
        {
            if (idNotificacion <= 0)
            {
                return BadRequest(
                    "El identificador de la notificación no es válido.");
            }

            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario) ||
                idUsuario <= 0)
            {
                return Unauthorized();
            }

            var notificacion =
                await _context.NotificacionesTransacciones
                    .FirstOrDefaultAsync(n =>
                        n.IdNotificacion == idNotificacion &&
                        n.IdUsuario == idUsuario);

            if (notificacion == null)
            {
                return NotFound(
                    "No se encontró la notificación para este usuario.");
            }

            if (!notificacion.Leida)
            {
                notificacion.Leida = true;
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                mensaje = "La notificación está marcada como leída.",
                idNotificacion = notificacion.IdNotificacion,
                leida = notificacion.Leida
            });
        }

        [HttpGet("ConteoPorSeccion")]
        public async Task<IActionResult> ObtenerConteoPorSeccion()
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario) ||
                idUsuario <= 0)
            {
                return Unauthorized();
            }

            var notificacionesPendientes =
                from n in _context.NotificacionesTransacciones
                join t in _context.Transacciones
                    on n.IdTransaccion equals t.IdTransaccion
                where n.IdUsuario == idUsuario
                      && !n.Leida
                      && t.EstadoTransaccion != "Completada"
                      && t.EstadoTransaccion != "Cancelada"
                select new
                {
                    n.IdNotificacion,
                    n.IdUsuario,
                    t.IdUsuarioComprador,
                    t.IdUsuarioVendedor
                };

            int compras = await notificacionesPendientes
                .CountAsync(n =>
                    n.IdUsuarioComprador == idUsuario);

            int ventas = await notificacionesPendientes
                .CountAsync(n =>
                    n.IdUsuarioVendedor == idUsuario);

            return Ok(new
            {
                compras,
                ventas
            });
        }

        [HttpGet("PorSeccion/{seccion}")]
        public async Task<IActionResult> ObtenerPorSeccion(string seccion)
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario) ||
                idUsuario <= 0)
            {
                return Unauthorized();
            }

            seccion = seccion.Trim().ToLowerInvariant();

            if (seccion != "compras" && seccion != "ventas")
            {
                return BadRequest(
                    "La sección debe ser compras o ventas.");
            }

            var consulta =
                from n in _context.NotificacionesTransacciones.AsNoTracking()
                join t in _context.Transacciones
                    on n.IdTransaccion equals t.IdTransaccion
                where n.IdUsuario == idUsuario
                      && !n.Leida
                      && t.EstadoTransaccion != "Completada"
                      && t.EstadoTransaccion != "Cancelada"
                      && (seccion == "compras"
                          ? t.IdUsuarioComprador == idUsuario
                          : t.IdUsuarioVendedor == idUsuario)
                orderby n.FechaCreacion descending,
                        n.IdNotificacion descending
                select new
                {
                    n.IdNotificacion,
                    n.IdTransaccion,
                    n.Tipo,
                    n.Mensaje,
                    n.FechaCreacion,
                    n.Leida
                };

            var notificaciones = await consulta.ToListAsync();

            return Ok(notificaciones);
        }
    }
}

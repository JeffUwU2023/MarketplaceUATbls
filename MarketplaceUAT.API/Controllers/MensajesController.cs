using Microsoft.AspNetCore.Mvc;
using MarketplaceUAT.Data.Context;
using MarketplaceUAT.API.DTOs;
using MarketplaceUAT.Data.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceUAT.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MensajesController : ControllerBase
    {
        private readonly MarketplaceContext _context;

        public MensajesController(MarketplaceContext context)
        {
            _context = context;
        }

        [HttpGet("Conversacion/{idConversacion}")]
        [Authorize]
        public IActionResult GetMensajes(int idConversacion)
        {
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuarioAutenticado))
            {
                return Unauthorized();
            }
            var conversacion = _context.Conversaciones
               .FirstOrDefault(c =>
              c.IdConversacion == idConversacion);

            if (conversacion == null)
            {
                return NotFound("La conversación no existe.");
            }

            if (idUsuarioAutenticado != conversacion.IdUsuarioComprador &&
                idUsuarioAutenticado != conversacion.IdUsuarioVendedor)
            {
                return Forbid();
            }
            var mensajes = _context.Mensajes
                .Where(m =>
                    m.IdConversacion == idConversacion &&
                    m.Activo)
                .OrderBy(m => m.FechaEnvio)
                .ToList();

            var idsMensajes = mensajes
                .Select(m => m.IdMensaje)
                .ToList();

            var imagenes = _context.ImagenesMensajes
                .Where(i =>
                    i.Activo &&
                    idsMensajes.Contains(i.IdMensaje))
                .ToList();

            var imagenesPorMensaje = imagenes.ToLookup(
                    i => i.IdMensaje);

            var resultado = mensajes
                .Select(m => new MensajeResponseDto
                {
                    IdMensaje = m.IdMensaje,
                    IdConversacion = m.IdConversacion,
                    IdUsuario = m.IdUsuario,
                    Contenido = m.Contenido,

                    UrlImagen = m.UrlImagen,

                    Imagenes = imagenesPorMensaje[m.IdMensaje]
                              .Select(i => i.UrlImagen)
                              .ToList(),

                    FechaEnvio = m.FechaEnvio,
                    Visto = m.Visto
                })
                .ToList();

            return Ok(resultado);
        }
        [HttpPost]
        [Authorize]
        public IActionResult CrearMensaje(MensajeCreateDto dto)
        {
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuarioAutenticado))
            {
                return Unauthorized();
            }
            if (dto.IdConversacion <= 0)
            {
                return BadRequest("La conversación no es válida.");
            }

            if (string.IsNullOrWhiteSpace(dto.Contenido) &&
            string.IsNullOrWhiteSpace(dto.UrlImagen) &&
            !dto.TieneImagenes)
            {
                return BadRequest(
                    "El mensaje debe contener texto, imagen o imágenes.");
            }

            var conversacion = _context.Conversaciones
                .FirstOrDefault(c =>
                    c.IdConversacion == dto.IdConversacion);

            if (conversacion == null)
            {
                return NotFound(
                    "La conversación no existe.");
            }

            if (!conversacion.Activa || conversacion.Archivada)
            {
                return BadRequest(
                    "Esta conversación está archivada y no permite nuevos mensajes.");
            }

            if (idUsuarioAutenticado != conversacion.IdUsuarioComprador &&
                idUsuarioAutenticado != conversacion.IdUsuarioVendedor)
            {
                return Forbid();
            }

            var mensaje = new Mensaje
            {
                IdConversacion = dto.IdConversacion,
                IdUsuario = idUsuarioAutenticado,
                Contenido = string.IsNullOrWhiteSpace(dto.Contenido)
                    ? null
                    : dto.Contenido.Trim(),
                UrlImagen = string.IsNullOrWhiteSpace(dto.UrlImagen)
                    ? null
                    : dto.UrlImagen.Trim(),
                FechaEnvio = DateTime.Now,
                Visto = false,
                Activo = true
            };

            _context.Mensajes.Add(mensaje);

            conversacion.UltimaActividad = DateTime.Now;

            _context.SaveChanges();

            return Ok(mensaje);
        }
        [HttpPut("{idMensaje}/Leer")]
        [Authorize]
        public IActionResult MarcarComoLeido(int idMensaje)
        {
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuarioAutenticado))
            {
                return Unauthorized();
            }
            var mensaje = _context.Mensajes
                .FirstOrDefault(m =>
                    m.IdMensaje == idMensaje &&
                    m.Activo);

            if (mensaje == null)
            {
                return NotFound(
                    "El mensaje no existe.");
            }

            if (mensaje.IdUsuario == idUsuarioAutenticado)
            {
                return BadRequest(
                    "No puedes marcar como leído un mensaje que tú enviaste.");
            }

            var conversacion = _context.Conversaciones
                .FirstOrDefault(c =>
                    c.IdConversacion == mensaje.IdConversacion);

            if (conversacion == null)
            {
                return NotFound(
                    "La conversación no existe.");
            }

            if (idUsuarioAutenticado != conversacion.IdUsuarioComprador &&
                idUsuarioAutenticado != conversacion.IdUsuarioVendedor)
            {
                return Forbid();
            }

            mensaje.Visto = true;

            _context.SaveChanges();

            return Ok(mensaje);
        }

        [HttpPut("Leer")]
        [Authorize]
        public IActionResult MarcarVariosComoLeidos(
    [FromBody] List<int> idsMensajes)
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(
                claimIdUsuario,
                out int idUsuarioAutenticado))
            {
                return Unauthorized();
            }

            if (idsMensajes == null || idsMensajes.Count == 0)
            {
                return BadRequest("Debes indicar los mensajes.");
            }

            var mensajes = _context.Mensajes
                .Where(m =>
                    idsMensajes.Contains(m.IdMensaje) &&
                    m.Activo &&
                    !m.Visto &&
                    m.IdUsuario != idUsuarioAutenticado &&
                    _context.Conversaciones.Any(c =>
                        c.IdConversacion == m.IdConversacion &&
                        (c.IdUsuarioComprador == idUsuarioAutenticado ||
                         c.IdUsuarioVendedor == idUsuarioAutenticado)))
                .ToList();

            foreach (var mensaje in mensajes)
            {
                mensaje.Visto = true;
            }

            _context.SaveChanges();

            return Ok(new
            {
                Actualizados = mensajes.Count
            });
        }

        [HttpPut("{idMensaje}")]
        [Authorize]
        public async Task<IActionResult> EditarMensaje(
            int idMensaje,
            [FromBody] MensajeUpdateDto dto)
        {
            var idUsuarioClaim = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(idUsuarioClaim, out int idUsuario))
            {
                return Unauthorized();
            }

            if (dto == null || string.IsNullOrWhiteSpace(dto.Contenido))
            {
                return BadRequest(new
                {
                    mensaje = "El texto del mensaje no puede estar vacío."
                });
            }

            var mensaje = await _context.Mensajes
                .FirstOrDefaultAsync(m =>
                    m.IdMensaje == idMensaje &&
                    m.Activo);

            if (mensaje == null)
            {
                return NotFound(new
                {
                    mensaje = "No se encontró el mensaje."
                });
            }

            if (mensaje.IdUsuario != idUsuario)
            {
                return Forbid();
            }

            mensaje.Contenido = dto.Contenido.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Mensaje actualizado correctamente.",
                idMensaje = mensaje.IdMensaje,
                contenido = mensaje.Contenido
            });
        }

        [HttpDelete("{idMensaje}")]
        [Authorize]
        public IActionResult EliminarMensaje(int idMensaje)
        {
            var claimIdUsuario = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(
                    claimIdUsuario,
                    out int idUsuarioAutenticado))
            {
                return Unauthorized();
            }

            var mensaje = _context.Mensajes
                .FirstOrDefault(m =>
                    m.IdMensaje == idMensaje &&
                    m.Activo);

            if (mensaje == null)
            {
                return NotFound("El mensaje no existe.");
            }

            if (mensaje.IdUsuario != idUsuarioAutenticado)
            {
                return Forbid();
            }

            // Desactiva el mensaje sin borrar físicamente sus imágenes.
            mensaje.Activo = false;

            var imagenes = _context.ImagenesMensajes
                .Where(i =>
                    i.IdMensaje == idMensaje &&
                    i.Activo)
                .ToList();

            foreach (var imagen in imagenes)
            {
                imagen.Activo = false;
            }

            _context.SaveChanges();

            return NoContent();
        }
    }
}
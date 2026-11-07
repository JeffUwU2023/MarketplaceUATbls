using MarketplaceUAT.API.DTOs;
using MarketplaceUAT.Data.Context;
using MarketplaceUAT.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

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
        public async Task<IActionResult> GetMensajes(int idConversacion)
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(
                claimIdUsuario,
                out int idUsuarioAutenticado))
            {
                return Unauthorized();
            }

            if (idConversacion <= 0)
            {
                return BadRequest(
                    "El identificador de la conversación no es válido.");
            }

            var conversacion = await _context.Conversaciones
                .AsNoTracking() 
                .FirstOrDefaultAsync(c =>
                    c.IdConversacion == idConversacion);

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

            // Consulta únicamente los campos necesarios.
            var mensajes = await _context.Mensajes
                .AsNoTracking()
                .Where(m =>
                    m.IdConversacion == idConversacion &&
                    m.Activo)
                .OrderBy(m => m.FechaEnvio)
                .Select(m => new MensajeResponseDto
                {
                    IdMensaje = m.IdMensaje,
                    IdConversacion = m.IdConversacion,
                    IdUsuario = m.IdUsuario,
                    Contenido = m.Contenido,
                    UrlImagen = m.UrlImagen,
                    FechaEnvio = m.FechaEnvio,
                    Visto = m.Visto,
                    Imagenes = new List<string>()
                })
                .ToListAsync();

            var idsMensajes = mensajes
                .Select(m => m.IdMensaje)
                .ToList();

            // Recupera las imágenes activas de los mensajes.
            var imagenes = await _context.ImagenesMensajes
                .AsNoTracking()
                .Where(i =>
                    i.Activo &&
                    idsMensajes.Contains(i.IdMensaje))
                .ToListAsync();

            var imagenesPorMensaje = imagenes.ToLookup(
                i => i.IdMensaje);

            // Asocia las imágenes a cada mensaje.
            foreach (var mensaje in mensajes)
            {
                mensaje.Imagenes = imagenesPorMensaje[mensaje.IdMensaje]
                    .Select(i => i.UrlImagen)
                    .ToList();
            }

            return Ok(mensajes);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CrearMensaje(
            [FromBody] MensajeCreateDto dto)
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(
                claimIdUsuario,
                out int idUsuarioAutenticado))
            {
                return Unauthorized();
            }

            if (dto == null)
            {
                return BadRequest("Los datos del mensaje son obligatorios.");
            }

            if (dto.IdConversacion <= 0)
            {
                return BadRequest("La conversación no es válida.");
            }

            if (dto.IdSolicitud == Guid.Empty)
            {
                return BadRequest("El identificador de solicitud no es válido.");
            }

            // Las imágenes se registran mediante el flujo específico
            // de ImagenesMensajeController.
            if (!string.IsNullOrWhiteSpace(dto.UrlImagen))
            {
                return BadRequest(
                    "Las imágenes deben enviarse mediante el servicio de imágenes del chat.");
            }

            if (string.IsNullOrWhiteSpace(dto.Contenido) &&
                !dto.TieneImagenes)
            {
                return BadRequest(
                    "El mensaje debe contener texto o imágenes.");
            }

            var conversacion = await _context.Conversaciones
                .FirstOrDefaultAsync(c =>
                    c.IdConversacion == dto.IdConversacion);

            if (conversacion == null)
            {
                return NotFound("La conversación no existe.");
            }

            // Autorizar al usuario antes de revelar información.
            if (idUsuarioAutenticado != conversacion.IdUsuarioComprador &&
                idUsuarioAutenticado != conversacion.IdUsuarioVendedor)
            {
                return NotFound(
                    "La conversación no existe o no tienes acceso.");
            }

            // Comprobar si la solicitud ya fue procesada.
            var mensajeExistente = await _context.Mensajes
                .FirstOrDefaultAsync(m =>
                    m.IdSolicitud == dto.IdSolicitud);

            if (mensajeExistente != null)
            {
                if (mensajeExistente.IdConversacion != dto.IdConversacion ||
                    mensajeExistente.IdUsuario != idUsuarioAutenticado)
                {
                    return Conflict(
                        "El identificador de solicitud ya fue utilizado.");
                }

                if (!mensajeExistente.Activo)
                {
                    return StatusCode(
                        StatusCodes.Status410Gone,
                        "El mensaje asociado a esta solicitud fue eliminado. " +
                        "Debes generar un nuevo identificador de solicitud.");
                }

                return Ok(mensajeExistente);
            }

            if (!conversacion.Activa || conversacion.Archivada)
            {
                return Conflict(
                    "Esta conversación está archivada o inactiva y no permite nuevos mensajes.");
            }

            var fechaActual = DateTime.Now;

            var mensaje = new Mensaje
            {
                IdConversacion = dto.IdConversacion,
                IdUsuario = idUsuarioAutenticado,
                IdSolicitud = dto.IdSolicitud,
                Contenido = string.IsNullOrWhiteSpace(dto.Contenido)
                    ? null
                    : dto.Contenido.Trim(),
                UrlImagen = null,
                FechaEnvio = fechaActual,
                Visto = false,
                Activo = true
            };

            _context.Mensajes.Add(mensaje);
            conversacion.UltimaActividad = fechaActual;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
    when (ex.InnerException is SqlException sqlEx &&
          (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                // Otra solicitud pudo registrar el mismo mensaje primero.
                _context.Entry(mensaje).State = EntityState.Detached;

                var mensajeDuplicado = await _context.Mensajes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m =>
                        m.IdSolicitud == dto.IdSolicitud);

                if (mensajeDuplicado == null)
                {
                    return Conflict(
                        "No se pudo registrar el mensaje porque existe una solicitud duplicada.");
                }

                if (mensajeDuplicado.IdConversacion != dto.IdConversacion ||
                    mensajeDuplicado.IdUsuario != idUsuarioAutenticado)
                {
                    return Conflict(
                        "El identificador de solicitud ya fue utilizado.");
                }

                return Ok(mensajeDuplicado);
            }

            return Ok(mensaje);
        }

        [HttpPut("{idMensaje}/Leer")]
        [Authorize]
        public async Task<IActionResult> MarcarComoLeido(int idMensaje)
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(
                claimIdUsuario,
                out int idUsuarioAutenticado))
            {
                return Unauthorized();
            }

            if (idMensaje <= 0)
            {
                return BadRequest("El identificador del mensaje no es válido.");
            }

            var mensaje = await _context.Mensajes
                .FirstOrDefaultAsync(m =>
                    m.IdMensaje == idMensaje &&
                    m.Activo);

            if (mensaje == null)
            {
                return NotFound("El mensaje no existe.");
            }

            var conversacion = await _context.Conversaciones
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.IdConversacion == mensaje.IdConversacion);

            if (conversacion == null || !conversacion.Activa)
            {
                return NotFound(
                    "La conversación no existe o está inactiva.");
            }

            if (idUsuarioAutenticado != conversacion.IdUsuarioComprador &&
                idUsuarioAutenticado != conversacion.IdUsuarioVendedor)
            {
                return Forbid();
            }

            if (mensaje.IdUsuario == idUsuarioAutenticado)
            {
                return BadRequest(
                    "No puedes marcar como leído un mensaje que tú enviaste.");
            }

            if (!mensaje.Visto)
            {
                mensaje.Visto = true;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        [HttpPut("Leer")]
        [Authorize]
        public async Task<IActionResult> MarcarVariosComoLeidos(
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

            if (idsMensajes.Count > 100)
            {
                return BadRequest(
                    "No puedes marcar más de 100 mensajes por solicitud.");
            }

            if (idsMensajes.Any(id => id <= 0))
            {
                return BadRequest(
                    "La lista contiene identificadores de mensajes no válidos.");
            }

            var idsUnicos = idsMensajes.Distinct().ToList();

            var mensajes = await _context.Mensajes
                .Where(m =>
                    idsUnicos.Contains(m.IdMensaje) &&
                    m.Activo &&
                    !m.Visto &&
                    m.IdUsuario != idUsuarioAutenticado &&
                    _context.Conversaciones.Any(c =>
                        c.IdConversacion == m.IdConversacion &&
                        c.Activa &&
                        (c.IdUsuarioComprador == idUsuarioAutenticado ||
                         c.IdUsuarioVendedor == idUsuarioAutenticado)))
                .ToListAsync();

            foreach (var mensaje in mensajes)
            {
                mensaje.Visto = true;
            }

            if (mensajes.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

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
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(
                claimIdUsuario,
                out int idUsuario))
            {
                return Unauthorized();
            }

            if (idMensaje <= 0)
            {
                return BadRequest(new
                {
                    mensaje = "El identificador del mensaje no es válido."
                });
            }

            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.Contenido))
            {
                return BadRequest(new
                {
                    mensaje = "El texto del mensaje no puede estar vacío."
                });
            }

            string contenido = dto.Contenido.Trim();

            if (contenido.Length > 1000)
            {
                return BadRequest(new
                {
                    mensaje = "El mensaje no puede superar los 1000 caracteres."
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

            var conversacion = await _context.Conversaciones
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.IdConversacion == mensaje.IdConversacion);

            if (conversacion == null || !conversacion.Activa)
            {
                return NotFound(new
                {
                    mensaje = "La conversación no existe o está inactiva."
                });
            }

            if (conversacion.Archivada)
            {
                return Conflict(new
                {
                    mensaje = "No puedes editar mensajes de una conversación archivada."
                });
            }

            mensaje.Contenido = contenido;

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
        public async Task<IActionResult> EliminarMensaje(int idMensaje)
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(
                claimIdUsuario,
                out int idUsuarioAutenticado))
            {
                return Unauthorized();
            }

            if (idMensaje <= 0)
            {
                return BadRequest(
                    "El identificador del mensaje no es válido.");
            }

            var mensaje = await _context.Mensajes
                .FirstOrDefaultAsync(m =>
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

            var conversacion = await _context.Conversaciones
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.IdConversacion == mensaje.IdConversacion);

            if (conversacion == null || !conversacion.Activa)
            {
                return NotFound(
                    "La conversación no existe o está inactiva.");
            }

            if (conversacion.Archivada)
            {
                return Conflict(new
                {
                    mensaje = "No puedes eliminar mensajes de una conversación archivada."
                });
            }

            // Desactiva el mensaje sin borrar físicamente sus archivos.
            mensaje.Activo = false;

            var imagenes = await _context.ImagenesMensajes
                .Where(i =>
                    i.IdMensaje == idMensaje &&
                    i.Activo)
                .ToListAsync();

            foreach (var imagen in imagenes)
            {
                imagen.Activo = false;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
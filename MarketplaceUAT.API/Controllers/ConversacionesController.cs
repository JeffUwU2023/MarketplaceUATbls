using MarketplaceUAT.API.DTOs;
using MarketplaceUAT.Data.Context;
using MarketplaceUAT.Data.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace MarketplaceUAT.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConversacionesController : ControllerBase
    {
        private readonly MarketplaceContext _context;
        private readonly IWebHostEnvironment _environment;

        public ConversacionesController(
    MarketplaceContext context,
    IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet("{idConversacion:int}")]
        [Authorize]
        public IActionResult GetConversacionPorId(int idConversacion)
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuarioAutenticado))
            {
                return Unauthorized();
            }

            var conversacion = _context.Conversaciones
                .Where(c =>
                    c.IdConversacion == idConversacion &&
                    (c.IdUsuarioComprador == idUsuarioAutenticado ||
                     c.IdUsuarioVendedor == idUsuarioAutenticado))
                .Select(c => new ConversacionResponseDto
                {
                    IdConversacion = c.IdConversacion,
                    IdProducto = c.IdProducto,

                    NombreProducto = _context.Productos
                        .Where(p => p.IdProducto == c.IdProducto)
                        .Select(p => p.Nombre)
                        .FirstOrDefault() ?? "Producto no disponible",

                    NombreOtroUsuario = _context.Usuarios
                        .Where(u =>
                            u.IdUsuario ==
                            (c.IdUsuarioComprador == idUsuarioAutenticado
                                ? c.IdUsuarioVendedor
                                : c.IdUsuarioComprador))
                        .Select(u => u.Nombres + " " + u.Apellidos)
                        .FirstOrDefault() ?? "Usuario",

                    IdUsuarioComprador = c.IdUsuarioComprador,
                    IdUsuarioVendedor = c.IdUsuarioVendedor,
                    FechaCreacion = c.FechaCreacion,
                    UltimaActividad = c.UltimaActividad,
                    Archivada = c.Archivada,
                    Activa = c.Activa
                })
                .FirstOrDefault();

            if (conversacion == null)
            {
                return NotFound(
                    "La conversación no existe o no tienes permiso para consultarla.");
            }

            return Ok(conversacion);
        }

        [HttpGet("Usuario")]
        [Authorize]
        public IActionResult GetConversacionesUsuario()
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuarioAutenticado))
            {
                return Unauthorized();
            }

            var conversaciones = _context.Conversaciones
                .Where(c =>
                    c.IdUsuarioComprador == idUsuarioAutenticado ||
                    c.IdUsuarioVendedor == idUsuarioAutenticado)
                .OrderByDescending(c => c.UltimaActividad)
                .Select(c => new ConversacionResponseDto
                {
                    IdConversacion = c.IdConversacion,
                    IdProducto = c.IdProducto,

                    NombreProducto = _context.Productos
                        .Where(p => p.IdProducto == c.IdProducto)
                        .Select(p => p.Nombre)
                        .FirstOrDefault() ?? "Producto no disponible",

                    NombreOtroUsuario = _context.Usuarios
                        .Where(u =>
                            u.IdUsuario ==
                            (c.IdUsuarioComprador == idUsuarioAutenticado
                                ? c.IdUsuarioVendedor
                                : c.IdUsuarioComprador))
                        .Select(u => u.Nombres + " " + u.Apellidos)
                        .FirstOrDefault() ?? "Usuario",

                    IdUsuarioComprador = c.IdUsuarioComprador,
                    IdUsuarioVendedor = c.IdUsuarioVendedor,
                    FechaCreacion = c.FechaCreacion,
                    UltimaActividad = c.UltimaActividad,
                    Archivada = c.Archivada,
                    Activa = c.Activa,

                    MensajesSinLeer = _context.Mensajes.Count(m =>
                        m.IdConversacion == c.IdConversacion &&
                        m.Activo &&
                        !m.Visto &&
                        m.IdUsuario != idUsuarioAutenticado &&
                        c.Activa &&
                        !c.Archivada)
                })
                .ToList();

            return Ok(conversaciones);
        }

        [HttpGet("Producto/{idProducto}")]
        [Authorize]
        public IActionResult GetConversacionPorProducto(int idProducto)
        {
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuarioAutenticado))
            {
                return Unauthorized();
            }

            var producto = _context.Productos
    .FirstOrDefault(p =>
        p.IdProducto == idProducto &&
        p.Activo);

            if (producto == null)
            {
                return NotFound("El producto no existe o ya no está disponible.");
            }

            if (producto.IdUsuario == idUsuarioAutenticado)
            {
                return BadRequest("No puedes consultar una conversación contigo mismo.");
            }

            int idUsuarioVendedor = producto.IdUsuario;

            var conversacion = _context.Conversaciones
                .Where(c =>
                    c.IdProducto == idProducto &&
                    c.IdUsuarioComprador == idUsuarioAutenticado &&
                    c.IdUsuarioVendedor == idUsuarioVendedor)
                .OrderByDescending(c => c.UltimaActividad)
                .Select(c => new ConversacionResponseDto
                {
                    IdConversacion = c.IdConversacion,
                    IdProducto = c.IdProducto,
                    NombreProducto = _context.Productos
                        .Where(p => p.IdProducto == c.IdProducto)
                        .Select(p => p.Nombre)
                        .FirstOrDefault() ?? "Producto no disponible",
                    NombreOtroUsuario = _context.Usuarios
                        .Where(u => u.IdUsuario == c.IdUsuarioVendedor)
                        .Select(u => u.Nombres + " " + u.Apellidos)
                        .FirstOrDefault() ?? "Usuario",
                    IdUsuarioComprador = c.IdUsuarioComprador,
                    IdUsuarioVendedor = c.IdUsuarioVendedor,
                    FechaCreacion = c.FechaCreacion,
                    UltimaActividad = c.UltimaActividad,
                    Archivada = c.Archivada,
                    Activa = c.Activa
                })
                .FirstOrDefault();

            if (conversacion == null)
            {
                return NoContent();
            }

            return Ok(conversacion);
        }

        [HttpPost]
        [Authorize]
        public IActionResult CrearConversacion(ConversacionCreateDto dto)
        {
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuarioComprador))
            {
                return Unauthorized();
            }

            var producto = _context.Productos
                .FirstOrDefault(p => p.IdProducto == dto.IdProducto);

            if (producto == null || !producto.Activo)
            {
                return BadRequest(
                    "El producto no existe o ya no está disponible.");
            }

            if (producto.IdUsuario == idUsuarioComprador)
            {
                return BadRequest(
                    "No puedes iniciar una conversación contigo mismo.");
            }

            var idUsuarioVendedor = producto.IdUsuario;
            var vendedorActivo = _context.Usuarios
    .Any(u =>
        u.IdUsuario == idUsuarioVendedor &&
        u.Activo);

            if (!vendedorActivo)
            {
                return NotFound(
                    "El vendedor no existe o su cuenta no está disponible.");
            }

            var conversacionExistente = _context.Conversaciones
               .FirstOrDefault(c =>
              c.IdProducto == dto.IdProducto &&
              c.IdUsuarioComprador == idUsuarioComprador &&
              c.IdUsuarioVendedor == idUsuarioVendedor);

            if (conversacionExistente != null)
            {
                if (conversacionExistente.Archivada ||
                    !conversacionExistente.Activa)
                {
                    return Conflict(
                        "Esta conversación está archivada y no admite mensajes nuevos.");
                }

                return Ok(CrearRespuestaConversacion(conversacionExistente));
            }


            var ahora = DateTime.Now;

            var conversacion = new Conversacion
            {
                IdProducto = dto.IdProducto,
                IdUsuarioComprador = idUsuarioComprador,
                IdUsuarioVendedor = idUsuarioVendedor,
                FechaCreacion = ahora,
                UltimaActividad = ahora,
                Archivada = false,
                Activa = true
            };

            _context.Conversaciones.Add(conversacion);

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sqlEx &&
                      (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                // Otra solicitud pudo crear la conversación primero.
                _context.Entry(conversacion).State = EntityState.Detached;

                var conversacionCreada = _context.Conversaciones
                    .FirstOrDefault(c =>
                        c.IdProducto == dto.IdProducto &&
                        c.IdUsuarioComprador == idUsuarioComprador &&
                        c.IdUsuarioVendedor == idUsuarioVendedor);

                if (conversacionCreada == null)
                {
                    return Conflict(
                        "No se pudo crear la conversación porque ya existe un registro duplicado.");
                }

                if (conversacionCreada.Archivada || !conversacionCreada.Activa)
                {
                    return Conflict(
                        "Esta conversación está archivada y no admite mensajes nuevos.");
                }

                return Ok(CrearRespuestaConversacion(conversacionCreada));
            }

            return Ok(CrearRespuestaConversacion(conversacion));
        }

        private string ObtenerRutaCarpetaChat(int idConversacion)
        {
            string carpetaBase = Path.GetFullPath(
                Path.Combine(
                    _environment.WebRootPath ?? Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot"),
                    "Images",
                    "Chat"));

            string carpetaChat = Path.GetFullPath(
                Path.Combine(
                    carpetaBase,
                    idConversacion.ToString()));

            string carpetaBaseConSeparador =
                carpetaBase.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!carpetaChat.StartsWith(
                    carpetaBaseConSeparador,
                    OperatingSystem.IsWindows()
                        ? StringComparison.OrdinalIgnoreCase
                        : StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "La ruta de la carpeta de imágenes no es válida.");
            }

            return carpetaChat;
        }

        [HttpDelete("{idConversacion:int}")]
        [Authorize]
        public async Task<IActionResult> EliminarConversacionArchivada(
            int idConversacion)
        {
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
            {
                return Unauthorized();
            }

            var conversacion = await _context.Conversaciones
                .FirstOrDefaultAsync(c =>
                    c.IdConversacion == idConversacion &&
                    (c.IdUsuarioComprador == idUsuario ||
                     c.IdUsuarioVendedor == idUsuario));

            if (conversacion == null)
            {
                return NotFound(
                    "La conversación no existe o no tienes permiso para eliminarla.");
            }

            if (!conversacion.Archivada || conversacion.Activa)
            {
                return BadRequest(
                    "Solo puedes eliminar conversaciones archivadas.");
            }

            // Obtener los identificadores de los mensajes de la conversación.
            var idsMensajes = await _context.Mensajes
                .Where(m => m.IdConversacion == idConversacion)
                .Select(m => m.IdMensaje)
                .ToListAsync();

            // Eliminar primero las imágenes adicionales de esos mensajes.
            var imagenes = await _context.ImagenesMensajes
                .Where(i => idsMensajes.Contains(i.IdMensaje))
                .ToListAsync();

            _context.ImagenesMensajes.RemoveRange(imagenes);

            // Después, eliminar los mensajes.
            var mensajes = await _context.Mensajes
                .Where(m => m.IdConversacion == idConversacion)
                .ToListAsync();

            _context.Mensajes.RemoveRange(mensajes);

            // Guardar primero la eliminación de los registros.
            _context.Conversaciones.Remove(conversacion);

            await _context.SaveChangesAsync();

            // Después, intentar eliminar los archivos físicos de esta conversación.
            try
            {
                string carpetaChat = ObtenerRutaCarpetaChat(idConversacion);

                if (Directory.Exists(carpetaChat))
                {
                    Directory.Delete(carpetaChat, recursive: true);
                }
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"No se pudo eliminar la carpeta de imágenes del chat " +
                    $"{idConversacion}: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"No hay permisos para eliminar la carpeta de imágenes del chat " +
                    $"{idConversacion}: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"La ruta de imágenes del chat no es válida: {ex.Message}");
            }

            return NoContent();
        }
        private ConversacionResponseDto CrearRespuestaConversacion(
    Conversacion conversacion)
        {
            var producto = _context.Productos
                .FirstOrDefault(p => p.IdProducto == conversacion.IdProducto);

            var nombreOtroUsuario = _context.Usuarios
                .Where(u => u.IdUsuario == conversacion.IdUsuarioVendedor)
                .Select(u => u.Nombres + " " + u.Apellidos)
                .FirstOrDefault() ?? "Usuario";

            return new ConversacionResponseDto
            {
                IdConversacion = conversacion.IdConversacion,
                IdProducto = conversacion.IdProducto,
                NombreProducto = producto?.Nombre ?? "Producto",
                NombreOtroUsuario = nombreOtroUsuario,
                IdUsuarioComprador = conversacion.IdUsuarioComprador,
                IdUsuarioVendedor = conversacion.IdUsuarioVendedor,
                FechaCreacion = conversacion.FechaCreacion,
                UltimaActividad = conversacion.UltimaActividad,
                Archivada = conversacion.Archivada,
                Activa = conversacion.Activa
            };
        }
    }
}
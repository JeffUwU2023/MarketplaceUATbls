using MarketplaceUAT.Data.Context;
using MarketplaceUAT.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketplaceUAT.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ImagenesMensajeController : ControllerBase
    {
        private const int MaximoImagenesPorMensaje = 5;
        private const long TamanoMaximoArchivo = 20 * 1024 * 1024;

        private static readonly HashSet<string> ExtensionesPermitidas =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

        private readonly MarketplaceContext _context;
        private readonly IWebHostEnvironment _environment;

        public ImagenesMensajeController(
            MarketplaceContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private bool IntentarObtenerUsuario(out int idUsuario)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out idUsuario);
        }

        private async Task<bool> EsParticipanteDelMensaje(
            int idMensaje,
            int idUsuario)
        {
            return await _context.Mensajes
                .Where(m => m.IdMensaje == idMensaje && m.Activo)
                .Join(
                    _context.Conversaciones,
                    m => m.IdConversacion,
                    c => c.IdConversacion,
                    (m, c) => c)
                .AnyAsync(c =>
                    c.IdUsuarioComprador == idUsuario ||
                    c.IdUsuarioVendedor == idUsuario);
        }

        private async Task<bool> EsPropietarioDelMensaje(
            int idMensaje,
            int idUsuario)
        {
            return await _context.Mensajes.AnyAsync(m =>
                m.IdMensaje == idMensaje &&
                m.IdUsuario == idUsuario &&
                m.Activo);
        }

        private string ObtenerCarpetaImagenes(int idConversacion)
        {
            string carpetaBase = Path.Combine(
                _environment.WebRootPath ?? Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot"),
                "Images");

            string carpetaChat = Path.Combine(
                carpetaBase,
                "Chat",
                idConversacion.ToString());

            Directory.CreateDirectory(carpetaChat);

            return carpetaChat;
        }

        private static bool EsImagenValida(
            byte[] datos,
            string extension)
        {
            if (extension is ".jpg" or ".jpeg")
            {
                return datos.Length >= 3 &&
                       datos[0] == 0xFF &&
                       datos[1] == 0xD8 &&
                       datos[2] == 0xFF;
            }

            if (extension == ".png")
            {
                byte[] firma =
                {
                    0x89, 0x50, 0x4E, 0x47,
                    0x0D, 0x0A, 0x1A, 0x0A
                };

                return datos.Length >= firma.Length &&
                       datos.Take(firma.Length).SequenceEqual(firma);
            }

            if (extension == ".webp")
            {
                return datos.Length >= 12 &&
                       datos[0] == (byte)'R' &&
                       datos[1] == (byte)'I' &&
                       datos[2] == (byte)'F' &&
                       datos[3] == (byte)'F' &&
                       datos[8] == (byte)'W' &&
                       datos[9] == (byte)'E' &&
                       datos[10] == (byte)'B' &&
                       datos[11] == (byte)'P';
            }

            return false;
        }

        private bool IntentarObtenerNombreArchivo(
        string urlImagen,
        out string nombreArchivo,
        out int idConversacion)
        {
            nombreArchivo = string.Empty;
            idConversacion = 0;

            if (!Uri.TryCreate(
                    urlImagen,
                    UriKind.Absolute,
                    out Uri? uri))
            {
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttp &&
                uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            // Solo se aceptan imágenes de esta API.
            if (!uri.AbsolutePath.StartsWith(
                    "/Images/Chat/",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // La URL debe pertenecer al servidor actual.
            if (!uri.Host.Equals(
                    Request.Host.Host,
                    StringComparison.OrdinalIgnoreCase) ||
                uri.Port != Request.Host.Port)
            {
                return false;
            }

            string[] segmentos = uri.AbsolutePath.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries);

            // Ruta esperada: Images/Chat/{idConversacion}/{archivo}
            if (segmentos.Length != 4 ||
                !segmentos[0].Equals(
                    "Images",
                    StringComparison.OrdinalIgnoreCase) ||
                !segmentos[1].Equals(
                    "Chat",
                    StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(segmentos[2], out idConversacion) ||
                idConversacion <= 0)
            {
                return false;
            }

            nombreArchivo = segmentos[3];

            // Evita nombres de archivo con rutas o separadores.
            if (string.IsNullOrWhiteSpace(nombreArchivo) ||
                nombreArchivo != Path.GetFileName(nombreArchivo) ||
                nombreArchivo.Contains('\\') ||
                nombreArchivo.Contains(':'))
            {
                nombreArchivo = string.Empty;
                return false;
            }

            return true;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ImagenMensaje>> GetImagen(int id)
        {
            if (!IntentarObtenerUsuario(out int idUsuario))
                return Unauthorized();

            var imagen = await _context.ImagenesMensajes
                .FirstOrDefaultAsync(i =>
                    i.IdImagenMensaje == id &&
                    i.Activo);

            if (imagen == null)
                return NotFound("La imagen no existe.");

            if (!await EsParticipanteDelMensaje(
                    imagen.IdMensaje,
                    idUsuario))
            {
                return Forbid();
            }

            return Ok(imagen);
        }

        [HttpGet("Mensaje/{idMensaje}")]
        public async Task<ActionResult<IEnumerable<ImagenMensaje>>>
            GetImagenesPorMensaje(int idMensaje)
        {
            if (!IntentarObtenerUsuario(out int idUsuario))
                return Unauthorized();

            if (!await EsParticipanteDelMensaje(idMensaje, idUsuario))
                return NotFound("El mensaje no existe o no tienes acceso.");

            var imagenes = await _context.ImagenesMensajes
                .Where(i =>
                    i.IdMensaje == idMensaje &&
                    i.Activo)
                .ToListAsync();

            return Ok(imagenes);
        }

        public class AgregarImagenMensajeDto
        {
            public int IdMensaje { get; set; }
            public string UrlImagen { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<ActionResult<ImagenMensaje>> AgregarImagen(
            [FromBody] AgregarImagenMensajeDto dto)
        {
            if (!IntentarObtenerUsuario(out int idUsuario))
                return Unauthorized();

            if (dto == null ||
                dto.IdMensaje <= 0 ||
                string.IsNullOrWhiteSpace(dto.UrlImagen))
            {
                return BadRequest(
                    "El mensaje y la URL de la imagen son obligatorios.");
            }

            if (!await EsPropietarioDelMensaje(dto.IdMensaje, idUsuario))
                return Forbid();

            var mensaje = await _context.Mensajes
                .FirstOrDefaultAsync(m =>
                    m.IdMensaje == dto.IdMensaje &&
                    m.Activo);

            if (mensaje == null)
                return NotFound("El mensaje no existe.");

            var conversacion = await _context.Conversaciones
                .FirstOrDefaultAsync(c =>
                    c.IdConversacion == mensaje.IdConversacion);

            if (conversacion == null)
                return NotFound("La conversación no existe.");

            if (!conversacion.Activa || conversacion.Archivada)
            {
                return BadRequest(
                    "La conversación está archivada o inactiva.");
            }


            if (!IntentarObtenerNombreArchivo(
                    dto.UrlImagen.Trim(),
                    out string nombreArchivo,
                    out int idConversacionImagen))
            {
                return BadRequest(
                    "La URL de la imagen no tiene una ruta válida.");
            }

            if (idConversacionImagen != conversacion.IdConversacion)
            {
                return BadRequest(
                    "La imagen no pertenece a la conversación del mensaje.");
            }

            string extension = Path.GetExtension(nombreArchivo)
                .ToLowerInvariant();

            if (!ExtensionesPermitidas.Contains(extension))
                return BadRequest("El formato de la imagen no está permitido.");

            string rutaArchivo = Path.Combine(
                 ObtenerCarpetaImagenes(conversacion.IdConversacion),
                 nombreArchivo);

            if (!System.IO.File.Exists(rutaArchivo))
                return BadRequest("El archivo de imagen no existe.");

            var cantidad = await _context.ImagenesMensajes.CountAsync(i =>
                i.IdMensaje == dto.IdMensaje &&
                i.Activo);

            if (cantidad >= MaximoImagenesPorMensaje)
            {
                return BadRequest(
                    "Un mensaje no puede tener más de cinco imágenes.");
            }

            var imagen = new ImagenMensaje
            {
                IdMensaje = dto.IdMensaje,
                UrlImagen = dto.UrlImagen.Trim(),
                Activo = true
            };

            _context.ImagenesMensajes.Add(imagen);

            await _context.SaveChangesAsync();

            return Ok(imagen);
        }

        [HttpPost("Subir")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(TamanoMaximoArchivo + 1024 * 1024)]
        public async Task<IActionResult> SubirImagen(
            [FromQuery] int idConversacion,
            IFormFile archivo)
        {
            if (!IntentarObtenerUsuario(out int idUsuario))
                return Unauthorized();

            if (idConversacion <= 0)
                return BadRequest("El identificador de la conversación no es válido.");

            if (archivo == null || archivo.Length == 0)
                return BadRequest("No se recibió ninguna imagen.");

            if (archivo.Length > TamanoMaximoArchivo)
                return BadRequest("La imagen no puede superar los 20 MB.");

            string extension = Path.GetExtension(archivo.FileName)
                .ToLowerInvariant();

            if (!ExtensionesPermitidas.Contains(extension))
            {
                return BadRequest(
                    "Solo se permiten imágenes JPG, JPEG, PNG y WEBP.");
            }

            string tipoEsperado = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => string.Empty
            };

            if (!string.Equals(
                    archivo.ContentType,
                    tipoEsperado,
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("El tipo de archivo no es válido.");
            }

            // Verifica que el usuario participe en esta conversación específica.
            var conversacion = await _context.Conversaciones
                .FirstOrDefaultAsync(c =>
                    c.IdConversacion == idConversacion &&
                    (c.IdUsuarioComprador == idUsuario ||
                     c.IdUsuarioVendedor == idUsuario));

            if (conversacion == null)
                return NotFound("La conversación no existe o no tienes acceso.");

            if (!conversacion.Activa || conversacion.Archivada)
            {
                return BadRequest(
                    "No se pueden subir imágenes a una conversación archivada o inactiva.");
            }

            byte[] datos;

            await using (var memoria = new MemoryStream())
            {
                await archivo.CopyToAsync(memoria);
                datos = memoria.ToArray();
            }

            if (!EsImagenValida(datos, extension))
            {
                return BadRequest(
                    "El contenido no corresponde a una imagen válida.");
            }

            // Guarda la imagen dentro de la carpeta de esta conversación.
            string carpeta = ObtenerCarpetaImagenes(idConversacion);

            string nombreArchivo = $"{Guid.NewGuid():N}{extension}";

            string rutaArchivo = Path.Combine(carpeta, nombreArchivo);

            try
            {
                await System.IO.File.WriteAllBytesAsync(
                    rutaArchivo,
                    datos);
            }
            catch (IOException)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "No fue posible guardar la imagen.");
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "El servidor no tiene permiso para guardar la imagen.");
            }

            // Devuelve la URL pública de la imagen.
            string urlImagen =
                $"{Request.Scheme}://{Request.Host}/Images/Chat/{idConversacion}/{nombreArchivo}";

            return Ok(new
            {
                nombreArchivo,
                urlImagen
            });
        }

        [HttpDelete("{idImagenMensaje}")]
        public async Task<IActionResult> EliminarImagen(
            int idImagenMensaje)
        {
            if (!IntentarObtenerUsuario(out int idUsuario))
                return Unauthorized();

            var imagen = await _context.ImagenesMensajes
                .FirstOrDefaultAsync(i =>
                    i.IdImagenMensaje == idImagenMensaje &&
                    i.Activo);

            if (imagen == null)
                return NotFound("La imagen no existe.");

            if (!await EsPropietarioDelMensaje(
                    imagen.IdMensaje,
                    idUsuario))
            {
                return Forbid();
            }

            // Desactiva el registro, pero conserva el archivo físico.
            // Así se evita borrar accidentalmente archivos compartidos.
            imagen.Activo = false;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

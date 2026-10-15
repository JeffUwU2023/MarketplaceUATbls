using MarketplaceUAT.API.DTOs;
using MarketplaceUAT.Data.Context;
using MarketplaceUAT.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketplaceUAT.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagenesProductoController : ControllerBase
    {
    private const long TamanoMaximoArchivo = 20 * 1024 * 1024;

        private static readonly HashSet<string> ExtensionesPermitidas =
            new(StringComparer.OrdinalIgnoreCase)
            {
        ".jpg", ".jpeg", ".png", ".webp"
            };
        private readonly MarketplaceContext _context;

        public ImagenesProductoController(MarketplaceContext context)
        {
            _context = context;
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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ImagenProductoResponseDto>>> GetImagenes()
        {
            var imagenes = await _context.ImagenesProducto
                .Where(i => i.Activo)
                .Select(i => new ImagenProductoResponseDto
                {
                    IdImagen = i.IdImagen,
                    IdProducto = i.IdProducto,
                    UrlImagen = i.UrlImagen,
                    EsPrincipal = i.EsPrincipal,
                    Activo = i.Activo
                })
                .ToListAsync();

            return imagenes;
        }

        [HttpGet("Producto/{idProducto}")]
        public async Task<ActionResult<IEnumerable<ImagenProductoResponseDto>>> GetImagenesPorProducto(int idProducto)
        {
            var imagenes = await _context.ImagenesProducto
                .Where(i => i.IdProducto == idProducto && i.Activo)
                .Select(i => new ImagenProductoResponseDto
                {
                    IdImagen = i.IdImagen,
                    IdProducto = i.IdProducto,
                    UrlImagen = i.UrlImagen,
                    EsPrincipal = i.EsPrincipal,
                    Activo = i.Activo
                })
                .ToListAsync();

            return imagenes;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ImagenProductoResponseDto>> GetImagen(int id)
        {
            var imagen = await _context.ImagenesProducto
                .Where(i => i.IdImagen == id && i.Activo)
                .Select(i => new ImagenProductoResponseDto
                {
                    IdImagen = i.IdImagen,
                    IdProducto = i.IdProducto,
                    UrlImagen = i.UrlImagen,
                    EsPrincipal = i.EsPrincipal,
                    Activo = i.Activo
                })
                .FirstOrDefaultAsync();

            if (imagen == null)
            {
                return NotFound();
            }

            return imagen;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ImagenProductoResponseDto>> CrearImagen(
            ImagenProductoCreateDto dto)
        {
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
            {
                return Unauthorized();
            }
            var productoExiste = await _context.Productos
               .AnyAsync(p =>
              p.IdProducto == dto.IdProducto &&
              p.IdUsuario == idUsuario &&
              p.Activo);

            if (!productoExiste)
            {
                return NotFound("El producto no existe o está inactivo.");
            }

            var imagen = new ImagenProducto
            {
                IdProducto = dto.IdProducto,
                UrlImagen = dto.UrlImagen,
                EsPrincipal = dto.EsPrincipal,
                Activo = true
            };

            _context.ImagenesProducto.Add(imagen);

            await _context.SaveChangesAsync();

            var respuesta = new ImagenProductoResponseDto
            {
                IdImagen = imagen.IdImagen,
                IdProducto = imagen.IdProducto,
                UrlImagen = imagen.UrlImagen,
                EsPrincipal = imagen.EsPrincipal,
                Activo = imagen.Activo
            };

            return CreatedAtAction(
                nameof(GetImagen),
                new { id = imagen.IdImagen },
                respuesta);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> ActualizarImagen(
        int id,
        ImagenProductoUpdateDto dto)
        {
            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
            {
                return Unauthorized();
            }

            var imagenExistente = await _context.ImagenesProducto
                 .FirstOrDefaultAsync(i => i.IdImagen == id && i.Activo);

            if (imagenExistente == null)
            {
                return NotFound();
            }

            var productoActual = await _context.Productos
    .FirstOrDefaultAsync(p =>
        p.IdProducto == imagenExistente.IdProducto &&
        p.IdUsuario == idUsuario &&
        p.Activo);

            if (productoActual == null)
            {
                return NotFound();
            }

            var productoExiste = await _context.Productos
    .AnyAsync(p =>
        p.IdProducto == dto.IdProducto &&
        p.IdUsuario == idUsuario &&
        p.Activo);

            if (!productoExiste)
            {
                return NotFound("El producto no existe o está inactivo.");
            }

            imagenExistente.IdProducto = dto.IdProducto;
            imagenExistente.UrlImagen = dto.UrlImagen;
            imagenExistente.EsPrincipal = dto.EsPrincipal;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> EliminarImagen(int id)
        {
            var imagen = await _context.ImagenesProducto
                .FirstOrDefaultAsync(i => i.IdImagen == id && i.Activo);

            if (imagen == null)
            {
                return NotFound();
            }

            var claimIdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
            {
                return Unauthorized();
            }

            var productoActual = await _context.Productos
                .FirstOrDefaultAsync(p =>
                    p.IdProducto == imagen.IdProducto &&
                    p.IdUsuario == idUsuario &&
                    p.Activo);

            if (productoActual == null)
            {
                return NotFound();
            }

            imagen.Activo = false;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("Subir")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(TamanoMaximoArchivo + 1024 * 1024)]
        public async Task<IActionResult> SubirImagen(
        [FromQuery] int idProducto,
        IFormFile archivo)
        {
            // 1. Obtener el ID del usuario autenticado.
            var claimIdUsuario =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claimIdUsuario, out int idUsuario))
            {
                return Unauthorized();
            }

            // 2. Validar el ID del producto.
            if (idProducto <= 0)
            {
                return BadRequest("El ID del producto no es válido.");
            }

            // 3. Comprobar que el producto pertenece al usuario.
            bool productoExiste = await _context.Productos
                .AnyAsync(p =>
                    p.IdProducto == idProducto &&
                    p.IdUsuario == idUsuario &&
                    p.Activo);

            if (!productoExiste)
            {
                return NotFound(
                    "El producto no existe, está inactivo o no te pertenece.");
            }

            // 4. Verificar que se haya recibido un archivo.
            if (archivo == null || archivo.Length == 0)
            {
                return BadRequest("No se recibió ninguna imagen.");
            }

            // 5. Validar el tamaño.
            if (archivo.Length > TamanoMaximoArchivo)
            {
                return BadRequest(
                    "La imagen no puede superar los 20 MB.");
            }

            // 6. Validar la extensión.
            string extension = Path
                .GetExtension(archivo.FileName)
                .ToLowerInvariant();

            if (!ExtensionesPermitidas.Contains(extension))
            {
                return BadRequest(
                    "Solo se permiten imágenes JPG, JPEG, PNG y WEBP.");
            }

            // 7. Validar el tipo MIME declarado.
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
                return BadRequest(
                    "El tipo de archivo no es válido.");
            }

            // 8. Leer y comprobar el contenido real.
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

            // 9. Crear la carpeta específica del producto.
            string carpetaProducto = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "Images",
                "Productos",
                idProducto.ToString());

            Directory.CreateDirectory(carpetaProducto);

            // 10. Generar un nombre de archivo único.
            string nombreArchivo =
                $"{Guid.NewGuid():N}{extension}";

            string rutaCompleta = Path.Combine(
                carpetaProducto,
                nombreArchivo);

            try
            {
                await System.IO.File.WriteAllBytesAsync(
                    rutaCompleta,
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

            // 11. Devolver la ruta relativa que se guardará en la BD.
            string urlImagen =
                $"Productos/{idProducto}/{nombreArchivo}";

            return Ok(new
            {
                UrlImagen = urlImagen
            });
        }
    }
}
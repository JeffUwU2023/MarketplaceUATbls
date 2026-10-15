using System.Security.Claims;
using System.Text.RegularExpressions;
using MarketplaceUAT.API.DTOs;
using MarketplaceUAT.API.Services;
using MarketplaceUAT.Data.Context;
using MarketplaceUAT.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceUAT.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly MarketplaceContext _context;
        private readonly JwtService _jwtService;
        private readonly IWebHostEnvironment _environment;
        public UsuariosController(
        MarketplaceContext context,
        JwtService jwtService,
        IWebHostEnvironment environment)
        {
            _context = context;
            _jwtService = jwtService;
            _environment = environment;
        }

        private static string NormalizarCorreo(string correo)
        {
            return correo.Trim().ToLowerInvariant();
        }

        private static bool EsCorreoValidoParaUsuario(
            string tipoUsuario,
            string? matricula,
            string correo)
        {
            correo = NormalizarCorreo(correo);

            if (tipoUsuario == "Alumno")
            {
                if (string.IsNullOrWhiteSpace(matricula) ||
                    !Regex.IsMatch(matricula, @"^[0-9]{10}$"))
                {
                    return false;
                }

                string correoEsperado =
                    $"a{matricula}@alumnos.uat.edu.mx";

                return correo == correoEsperado;
            }

            if (tipoUsuario == "Docente")
            {
                if (!string.IsNullOrWhiteSpace(matricula))
                {
                    return false;
                }

                return Regex.IsMatch(
                    correo,
                    @"^[^@\s]+@docentes\.uat\.edu\.mx$");
            }

            return false;
        }

        private static UsuarioResponseDto CrearRespuesta(Usuario usuario)
        {
            return new UsuarioResponseDto
            {
                IdUsuario = usuario.IdUsuario,
                Nombres = usuario.Nombres,
                Apellidos = usuario.Apellidos,
                TipoUsuario = usuario.TipoUsuario,
                Matricula = usuario.Matricula,
                Correo = usuario.Correo,
                UrlFotoPerfil = usuario.UrlFotoPerfil,
                FechaRegistro = usuario.FechaRegistro,
                Activo = usuario.Activo
            };
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<UsuarioResponseDto>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == id && u.Activo);

            if (usuario == null)
            {
                return NotFound();
            }

            return Ok(CrearRespuesta(usuario));
        }

        [HttpPost]
        public async Task<ActionResult<UsuarioResponseDto>> CrearUsuario(
            [FromBody] UsuarioCreateDto dto)
        {
            dto.Nombres = dto.Nombres.Trim();
            dto.Apellidos = dto.Apellidos.Trim();
            dto.TipoUsuario = dto.TipoUsuario.Trim();
            dto.Correo = NormalizarCorreo(dto.Correo);

            if (dto.TipoUsuario != "Alumno" &&
                dto.TipoUsuario != "Docente")
            {
                return BadRequest(
                    "El tipo de usuario debe ser Alumno o Docente.");
            }

            if (dto.TipoUsuario == "Alumno")
            {
                if (string.IsNullOrWhiteSpace(dto.Matricula) ||
                    !Regex.IsMatch(dto.Matricula, @"^[0-9]{10}$"))
                {
                    return BadRequest(
                        "La matrícula del alumno debe contener exactamente 10 dígitos.");
                }

                dto.Matricula = dto.Matricula.Trim();
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(dto.Matricula))
                {
                    return BadRequest(
                        "Los docentes no deben registrar matrícula.");
                }

                dto.Matricula = null;
            }

            if (!EsCorreoValidoParaUsuario(
                dto.TipoUsuario,
                dto.Matricula,
                dto.Correo))
            {
                return BadRequest(
                    dto.TipoUsuario == "Alumno"
                        ? "El correo del alumno debe coincidir con su matrícula y tener el formato a + matrícula + @alumnos.uat.edu.mx."
                        : "El correo del docente debe pertenecer al dominio @docentes.uat.edu.mx.");
            }

            bool correoExiste = await _context.Usuarios
                .AnyAsync(u => u.Correo == dto.Correo);

            if (correoExiste)
            {
                return Conflict("El correo ya está registrado.");
            }

            if (dto.TipoUsuario == "Alumno")
            {
                bool matriculaExiste = await _context.Usuarios
                    .AnyAsync(u => u.Matricula == dto.Matricula);

                if (matriculaExiste)
                {
                    return Conflict("La matrícula ya está registrada.");
                }
            }

            var usuario = new Usuario
            {
                Nombres = dto.Nombres,
                Apellidos = dto.Apellidos,
                TipoUsuario = dto.TipoUsuario,
                Matricula = dto.Matricula,
                Correo = dto.Correo,
                FechaRegistro = DateTime.Now,
                Activo = true
            };

            var passwordHasher = new PasswordHasher<Usuario>();

            usuario.PasswordHash = passwordHasher.HashPassword(
                usuario,
                dto.Password);

            _context.Usuarios.Add(usuario);

            await _context.SaveChangesAsync();

            var respuesta = CrearRespuesta(usuario);

            return CreatedAtAction(
                nameof(GetUsuario),
                new { id = usuario.IdUsuario },
                respuesta);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> ActualizarUsuario(
            int id,
            [FromBody] UsuarioUpdateDto dto)
        {
            var idUsuarioAutenticado = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(
                    idUsuarioAutenticado,
                    out var idUsuario) ||
                idUsuario != id)
            {
                return Forbid();
            }

            var usuarioExistente = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == id && u.Activo);

            if (usuarioExistente == null)
            {
                return NotFound();
            }

            dto.Nombres = dto.Nombres.Trim();
            dto.Apellidos = dto.Apellidos.Trim();
            dto.Correo = NormalizarCorreo(dto.Correo);

            if (!EsCorreoValidoParaUsuario(
                usuarioExistente.TipoUsuario,
                usuarioExistente.Matricula,
                dto.Correo))
            {
                return BadRequest(
                    usuarioExistente.TipoUsuario == "Alumno"
                        ? "El correo debe corresponder a la matrícula del alumno y usar @alumnos.uat.edu.mx."
                        : "El correo del docente debe pertenecer al dominio @docentes.uat.edu.mx.");
            }

            bool correoExiste = await _context.Usuarios
                .AnyAsync(u =>
                    u.IdUsuario != id &&
                    u.Correo == dto.Correo);

            if (correoExiste)
            {
                return Conflict(
                    "El correo ya está registrado en otra cuenta.");
            }

            usuarioExistente.Nombres = dto.Nombres;
            usuarioExistente.Apellidos = dto.Apellidos;
            usuarioExistente.Correo = dto.Correo;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                var passwordHasher = new PasswordHasher<Usuario>();

                usuarioExistente.PasswordHash =
                    passwordHasher.HashPassword(
                        usuarioExistente,
                        dto.Password);
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var idUsuarioAutenticado = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(
                    idUsuarioAutenticado,
                    out var idUsuario) ||
                idUsuario != id)
            {
                return Forbid();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == id && u.Activo);

            if (usuario == null)
            {
                return NotFound();
            }

            usuario.Activo = false;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("Login")]
        public async Task<ActionResult<LoginResponseDto>> Login(
            [FromBody] UsuarioLoginDto dto)
        {
            string correo = NormalizarCorreo(dto.Correo);

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.Correo == correo && u.Activo);

            if (usuario == null)
            {
                return Unauthorized(
                    "Correo o contraseña incorrectos.");
            }

            var passwordHasher = new PasswordHasher<Usuario>();

            var resultado = passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.PasswordHash,
                dto.Password);

            if (resultado == PasswordVerificationResult.Failed)
            {
                return Unauthorized(
                    "Correo o contraseña incorrectos.");
            }

            var respuesta = CrearRespuesta(usuario);

            var token = _jwtService.GenerarToken(
                usuario.IdUsuario,
                usuario.Correo);

            return Ok(new LoginResponseDto
            {
                Usuario = respuesta,
                Token = token
            });
        }

        [HttpPost("{id}/FotoPerfil")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(21 * 1024 * 1024)]
        public async Task<IActionResult> SubirFotoPerfil(
            int id,
            IFormFile archivo)
        {
            var idUsuarioAutenticado = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(
                    idUsuarioAutenticado,
                    out var idUsuario) ||
                idUsuario != id)
            {
                return Forbid();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == id && u.Activo);

            if (usuario == null)
            {
                return NotFound("El usuario no existe o está inactivo.");
            }

            if (archivo == null || archivo.Length == 0)
            {
                return BadRequest("Debes seleccionar una imagen.");
            }

            const long tamanoMaximo = 20 * 1024 * 1024;

            if (archivo.Length > tamanoMaximo)
            {
                return BadRequest(
                    "La fotografía no puede superar los 20 MB.");
            }

            string extension = Path.GetExtension(archivo.FileName)
                .ToLowerInvariant();

            var extensionesPermitidas = new[]
            {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

            if (!extensionesPermitidas.Contains(extension))
            {
                return BadRequest(
                    "Solo se permiten imágenes JPG, PNG o WEBP.");
            }

            string? tipoEsperado = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => null
            };

            if (tipoEsperado == null ||
                !string.Equals(
                    archivo.ContentType,
                    tipoEsperado,
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "El tipo de archivo no coincide con la imagen.");
            }

            byte[] encabezado = new byte[12];

            await using (var streamValidacion = archivo.OpenReadStream())
            {
                int bytesLeidos = await streamValidacion.ReadAsync(
                    encabezado,
                    0,
                    encabezado.Length);

                bool firmaValida = extension switch
                {
                    ".jpg" or ".jpeg" =>
                        bytesLeidos >= 3 &&
                        encabezado[0] == 0xFF &&
                        encabezado[1] == 0xD8 &&
                        encabezado[2] == 0xFF,

                    ".png" =>
                        bytesLeidos >= 8 &&
                        encabezado[0] == 0x89 &&
                        encabezado[1] == 0x50 &&
                        encabezado[2] == 0x4E &&
                        encabezado[3] == 0x47 &&
                        encabezado[4] == 0x0D &&
                        encabezado[5] == 0x0A &&
                        encabezado[6] == 0x1A &&
                        encabezado[7] == 0x0A,

                    ".webp" =>
                        bytesLeidos >= 12 &&
                        encabezado[0] == 0x52 &&
                        encabezado[1] == 0x49 &&
                        encabezado[2] == 0x46 &&
                        encabezado[3] == 0x46 &&
                        encabezado[8] == 0x57 &&
                        encabezado[9] == 0x45 &&
                        encabezado[10] == 0x42 &&
                        encabezado[11] == 0x50,

                    _ => false
                };

                if (!firmaValida)
                {
                    return BadRequest(
                        "El contenido del archivo no corresponde a una imagen válida.");
                }
            }

            string carpetaBase = Path.Combine(
           _environment.WebRootPath ?? Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot"),
            "Images",
            "Perfiles",
            id.ToString());

            Directory.CreateDirectory(carpetaBase);

            string nombreArchivo = $"{Guid.NewGuid():N}{extension}";

            string rutaArchivo = Path.Combine(
                carpetaBase,
                nombreArchivo);

            await using (var stream = new FileStream(
                rutaArchivo,
                FileMode.CreateNew,
                FileAccess.Write))
            {
                await archivo.CopyToAsync(stream);
            }

            usuario.UrlFotoPerfil = $"/Images/Perfiles/{id}/{nombreArchivo}";

            await _context.SaveChangesAsync();

            return Ok(CrearRespuesta(usuario));
        }

        [HttpDelete("{id}/FotoPerfil")]
        [Authorize]
        public async Task<IActionResult> EliminarFotoPerfil(int id)
        {
            var idUsuarioAutenticado = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(idUsuarioAutenticado, out var idUsuario) ||
                idUsuario != id)
            {
                return Forbid();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == id && u.Activo);

            if (usuario == null)
            {
                return NotFound("El usuario no existe o está inactivo.");
            }

            if (string.IsNullOrWhiteSpace(usuario.UrlFotoPerfil))
            {
                return Ok(new { mensaje = "El usuario no tiene fotografía de perfil." });
            }

            string? urlFotoAnterior = usuario.UrlFotoPerfil;
            string? rutaArchivo = null;

            string rutaUrl = urlFotoAnterior.Replace('\\', '/');
            string prefijoUsuario = $"/Images/Perfiles/{id}/";
            const string prefijoAnterior = "/Images/Perfiles/";

            string nombreArchivo = "";

            if (rutaUrl.StartsWith(
                    prefijoUsuario,
                    StringComparison.OrdinalIgnoreCase))
            {
                nombreArchivo = rutaUrl.Substring(prefijoUsuario.Length);

                if (!string.IsNullOrWhiteSpace(nombreArchivo) &&
                    Path.GetFileName(nombreArchivo) == nombreArchivo)
                {
                    rutaArchivo = Path.Combine(
                        _environment.WebRootPath ?? Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot"),
                        "Images",
                        "Perfiles",
                        id.ToString(),
                        nombreArchivo);
                }
            }
            else if (rutaUrl.StartsWith(
                         prefijoAnterior,
                         StringComparison.OrdinalIgnoreCase))
            {
                // Compatibilidad con fotografías guardadas en la carpeta anterior.
                nombreArchivo = rutaUrl.Substring(prefijoAnterior.Length);

                if (!string.IsNullOrWhiteSpace(nombreArchivo) &&
                    Path.GetFileName(nombreArchivo) == nombreArchivo)
                {
                    rutaArchivo = Path.Combine(
                        _environment.WebRootPath ?? Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot"),
                        "Images",
                        "Perfiles",
                        nombreArchivo);
                }
            }

            // Quitar la referencia de la base de datos.
            usuario.UrlFotoPerfil = null;
            await _context.SaveChangesAsync();

            // Eliminar el archivo si existe.
            if (rutaArchivo != null && System.IO.File.Exists(rutaArchivo))
            {
                System.IO.File.Delete(rutaArchivo);
            }

            return Ok(new
            {
                mensaje = "La fotografía de perfil se eliminó correctamente."
            });
        }
    }
}

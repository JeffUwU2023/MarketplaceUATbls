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
using Microsoft.Data.SqlClient;
using System.Text.Json;
using System.Data;

namespace MarketplaceUAT.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly MarketplaceContext _context;
        private readonly JwtService _jwtService;
        private readonly RefreshTokenService _refreshTokenService;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public UsuariosController(
            MarketplaceContext context,
            JwtService jwtService,
            RefreshTokenService refreshTokenService,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _refreshTokenService = refreshTokenService;
            _environment = environment;
            _configuration = configuration;
        }

        private int ObtenerDuracionRefreshToken()
        {
            int dias = _configuration.GetValue<int>(
                "Jwt:RefreshTokenExpirationDays");

            if (dias <= 0)
            {
                throw new InvalidOperationException(
                    "La duración del Refresh Token debe ser mayor que cero.");
            }

            return dias;
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
        public async Task<ActionResult<UsuarioPerfilDto>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios
                .Where(u => u.IdUsuario == id && u.Activo)
                .Select(u => new UsuarioPerfilDto
                {
                    IdUsuario = u.IdUsuario,
                    Nombres = u.Nombres,
                    Apellidos = u.Apellidos,
                    TipoUsuario = u.TipoUsuario,
                    Correo = u.Correo,
                    UrlFotoPerfil = u.UrlFotoPerfil,
                    FechaRegistro = u.FechaRegistro
                })
                .FirstOrDefaultAsync();

            if (usuario == null)
            {
                return NotFound();
            }

            return Ok(usuario);
        }

        [HttpPost]
        public async Task<ActionResult<UsuarioResponseDto>> CrearUsuario(
            [FromBody] UsuarioCreateDto dto)
        {
            dto.Nombres = dto.Nombres.Trim();
            dto.Apellidos = dto.Apellidos.Trim();
            dto.TipoUsuario = dto.TipoUsuario.Trim();
            dto.Correo = NormalizarCorreo(dto.Correo);

            if (string.IsNullOrWhiteSpace(dto.Nombres) ||
                string.IsNullOrWhiteSpace(dto.Apellidos))
            {
                return BadRequest(
                    "Los nombres y apellidos no pueden estar vacíos.");
            }

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

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sqlEx &&
                      (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                _context.Entry(usuario).State = EntityState.Detached;

                return Conflict(
                    "El correo o la matrícula ya están registrados.");
            }

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

            if (dto == null)
            {
                return BadRequest("Los datos del usuario son obligatorios.");
            }

            dto.Nombres = (dto.Nombres ?? string.Empty).Trim();
            dto.Apellidos = (dto.Apellidos ?? string.Empty).Trim();
            dto.Correo = NormalizarCorreo(dto.Correo ?? string.Empty);

            if (string.IsNullOrWhiteSpace(dto.Nombres) ||
                string.IsNullOrWhiteSpace(dto.Apellidos))
            {
                return BadRequest(
                    "Los nombres y apellidos no pueden estar vacíos.");
            }

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
                if (string.IsNullOrWhiteSpace(dto.PasswordActual))
                {
                    return BadRequest(
                        "Debes introducir tu contraseña actual para cambiarla.");
                }

                var passwordHasher = new PasswordHasher<Usuario>();

                var resultado = passwordHasher.VerifyHashedPassword(
                    usuarioExistente,
                    usuarioExistente.PasswordHash,
                    dto.PasswordActual);

                if (resultado == PasswordVerificationResult.Failed)
                {
                    return BadRequest("La contraseña actual es incorrecta.");
                }

                usuarioExistente.PasswordHash =
                    passwordHasher.HashPassword(
                        usuarioExistente,
                        dto.Password);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sqlEx &&
                      (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                _context.Entry(usuarioExistente).State = EntityState.Detached;

                return Conflict(
                    "El correo ya está registrado en otra cuenta.");
            }

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

            if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.PasswordHash = passwordHasher.HashPassword(
                    usuario,
                    dto.Password);
            }

            var respuesta = CrearRespuesta(usuario);

            var token = _jwtService.GenerarToken(
                usuario.IdUsuario,
                usuario.Correo,
                usuario.EsAdministrador);

            // Generar Refresh Token.
            string refreshToken =
                _refreshTokenService.GenerarRefreshToken();

            // Preparar su registro.
            int diasRefreshToken = ObtenerDuracionRefreshToken();

            var fechaExpiracion = DateTime.UtcNow.AddDays(
                diasRefreshToken);

            Guid idFamilia = Guid.NewGuid();

            var nuevoRefreshToken = _refreshTokenService.CrearRegistro(
                usuario.IdUsuario,
                refreshToken,
                fechaExpiracion,
                idFamilia);

            _context.RefreshTokens.Add(nuevoRefreshToken);

            // Guardar cambios en la base de datos.
            await _context.SaveChangesAsync();

            return Ok(new LoginResponseDto
            {
                Usuario = respuesta,
                Token = token,
                RefreshToken = refreshToken
            });
        }


        [HttpPost("Refresh")]
        public async Task<ActionResult<RefreshTokenResponseDto>> Refresh(
            [FromBody] RefreshTokenRequestDto dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                return BadRequest("El Refresh Token es obligatorio.");
            }

            string tokenHash = _refreshTokenService.CalcularHash(
                dto.RefreshToken);

            // Iniciar una transacción que serializa las renovaciones.
            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            // Bloquear el registro mientras se procesa la renovación.
            var refreshTokenExistente = await _context.RefreshTokens
                .FromSqlInterpolated($@"
            SELECT *
            FROM dbo.RefreshTokens WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
            WHERE TokenHash = {tokenHash}")
                .AsTracking()
                .FirstOrDefaultAsync();

            if (refreshTokenExistente == null)
            {
                return Unauthorized("El Refresh Token no es válido.");
            }

            DateTime ahora = DateTime.UtcNow;

            // Comprobar si el token ya fue revocado.
            if (refreshTokenExistente.FechaRevocacion != null)
            {
                // Si no tiene reemplazo, fue revocado por Logout.
                if (string.IsNullOrWhiteSpace(
                    refreshTokenExistente.TokenReemplazo))
                {
                    return Unauthorized(
                        "El Refresh Token fue revocado.");
                }

                // Comprobar si todavía está dentro de la ventana
                // permitida para recuperar la respuesta anterior.
                if (refreshTokenExistente.FechaLimiteReintento != null &&
                    refreshTokenExistente.FechaLimiteReintento >= ahora &&
                    !string.IsNullOrWhiteSpace(
                        refreshTokenExistente.RespuestaRenovacionCifrada))
                {
                    string respuestaJsonGuardada =
                          _refreshTokenService.DesprotegerRespuesta(
                           refreshTokenExistente.RespuestaRenovacionCifrada);

                    var respuestaGuardada =
                        JsonSerializer.Deserialize<RefreshTokenResponseDto>(
                           respuestaJsonGuardada);

                    if (respuestaGuardada == null ||
                        string.IsNullOrWhiteSpace(respuestaGuardada.Token) ||
                        string.IsNullOrWhiteSpace(respuestaGuardada.RefreshToken))
                    {
                        return StatusCode(
                            StatusCodes.Status500InternalServerError,
                            "No fue posible recuperar la respuesta de renovación.");
                    }

                    await transaccion.CommitAsync();

                    return Ok(respuestaGuardada);
                }

                // La ventana terminó: detectar reutilización.
                var tokensActivos = await _context.RefreshTokens
                    .Where(r =>
                        r.IdFamilia == refreshTokenExistente.IdFamilia &&
                        r.FechaRevocacion == null)
                    .ToListAsync();

                foreach (var token in tokensActivos)
                {
                    token.FechaRevocacion = ahora;
                }

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return Unauthorized(
                    "Se detectó la reutilización de un Refresh Token. La sesión ha sido revocada.");
            }

            // Comprobar expiración.
            if (refreshTokenExistente.FechaExpiracion <= ahora)
            {
                return Unauthorized("El Refresh Token ha expirado.");
            }

            // Comprobar que el usuario siga activo.
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == refreshTokenExistente.IdUsuario &&
                    u.Activo);

            if (usuario == null)
            {
                return Unauthorized(
                    "La cuenta no existe o está desactivada.");
            }

            // Generar un nuevo JWT.
            string nuevoToken = _jwtService.GenerarToken(
                usuario.IdUsuario,
                usuario.Correo,
                usuario.EsAdministrador);

            // Generar un nuevo Refresh Token.
            string nuevoRefreshToken =
                _refreshTokenService.GenerarRefreshToken();

            int diasRefreshToken = ObtenerDuracionRefreshToken();

            DateTime fechaExpiracion = ahora.AddDays(
                diasRefreshToken);

            var nuevoRegistro = _refreshTokenService.CrearRegistro(
                usuario.IdUsuario,
                nuevoRefreshToken,
                fechaExpiracion,
                refreshTokenExistente.IdFamilia);

            // Preparar la respuesta.
            var respuesta = new RefreshTokenResponseDto
            {
                Token = nuevoToken,
                RefreshToken = nuevoRefreshToken
            };

            // Serializar y cifrar la respuesta.
            string respuestaJson = JsonSerializer.Serialize(respuesta);

            string respuestaCifrada =
                _refreshTokenService.ProtegerRespuesta(respuestaJson);

            // Revocar el token anterior y guardar la respuesta
            // para permitir reintentos durante 10 segundos.
            refreshTokenExistente.FechaRevocacion = ahora;

            refreshTokenExistente.TokenReemplazo =
                _refreshTokenService.CalcularHash(nuevoRefreshToken);

            refreshTokenExistente.RespuestaRenovacionCifrada =
                respuestaCifrada;

            refreshTokenExistente.FechaLimiteReintento =
                ahora.AddSeconds(10);

            // Insertar el nuevo registro.
            _context.RefreshTokens.Add(nuevoRegistro);

            // Guardar todos los cambios dentro de la transacción.
            await _context.SaveChangesAsync();

            await transaccion.CommitAsync();

            return Ok(respuesta);
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout(
    [FromBody] RefreshTokenRequestDto dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                return BadRequest("El Refresh Token es obligatorio.");
            }

            string tokenHash = _refreshTokenService.CalcularHash(
                dto.RefreshToken);

            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var refreshToken = await _context.RefreshTokens
                .FromSqlInterpolated($@"
            SELECT *
            FROM dbo.RefreshTokens WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
            WHERE TokenHash = {tokenHash}")
                .AsTracking()
                .FirstOrDefaultAsync();

            if (refreshToken == null)
            {
                await transaccion.CommitAsync();
                return NoContent();
            }

            if (refreshToken.FechaRevocacion == null)
            {
                refreshToken.FechaRevocacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }

            await transaccion.CommitAsync();

            return NoContent();
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

            string? urlFotoAnterior = usuario.UrlFotoPerfil;

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

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Si no se pudo guardar la nueva ruta en la base de datos,
                // eliminar el archivo nuevo para evitar dejarlo huérfano.
                if (System.IO.File.Exists(rutaArchivo))
                {
                    System.IO.File.Delete(rutaArchivo);
                }

                throw;
            }


            // La foto nueva ya está guardada en la base de datos.
            // Ahora validamos la ruta de la foto anterior antes de borrarla.
            if (!string.IsNullOrWhiteSpace(urlFotoAnterior))
            {
                string rutaUrlAnterior = urlFotoAnterior.Replace('\\', '/');
                string prefijoUsuario = $"/Images/Perfiles/{id}/";
                const string prefijoAnterior = "/Images/Perfiles/";

                string? nombreAnterior = null;
                bool esRutaAnterior = false;

                if (rutaUrlAnterior.StartsWith(
                        prefijoUsuario,
                        StringComparison.OrdinalIgnoreCase))
                {
                    nombreAnterior =
                        rutaUrlAnterior.Substring(prefijoUsuario.Length);
                }
                else if (rutaUrlAnterior.StartsWith(
                             prefijoAnterior,
                             StringComparison.OrdinalIgnoreCase))
                {
                    // Compatibilidad con fotos guardadas en la carpeta anterior.
                    nombreAnterior =
                        rutaUrlAnterior.Substring(prefijoAnterior.Length);

                    esRutaAnterior = true;
                }

                if (!string.IsNullOrWhiteSpace(nombreAnterior))
                {
                    string? rutaAnterior = ObtenerRutaFotoPerfilSegura(
                        id,
                        nombreAnterior,
                        permitirRutaAnterior: esRutaAnterior);

                    if (rutaAnterior != null &&
                        System.IO.File.Exists(rutaAnterior))
                    {
                        try
                        {
                            System.IO.File.Delete(rutaAnterior);
                        }
                        catch (IOException)
                        {
                            // La foto nueva ya está guardada.
                            // No invalidamos la operación por este fallo.
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // La referencia de la base de datos ya apunta
                            // a la foto nueva.
                        }
                    }
                }
            }

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

            string urlFotoAnterior = usuario.UrlFotoPerfil;
            string? rutaArchivo = null;

            string rutaUrl = urlFotoAnterior.Replace('\\', '/');
            string prefijoUsuario = $"/Images/Perfiles/{id}/";
            const string prefijoAnterior = "/Images/Perfiles/";

            string nombreArchivo = "";
            bool esRutaAnterior = false;

            if (rutaUrl.StartsWith(
                    prefijoUsuario,
                    StringComparison.OrdinalIgnoreCase))
            {
                nombreArchivo = rutaUrl.Substring(prefijoUsuario.Length);
            }
            else if (rutaUrl.StartsWith(
                         prefijoAnterior,
                         StringComparison.OrdinalIgnoreCase))
            {
                // Compatibilidad con fotografías guardadas en la carpeta anterior.
                nombreArchivo = rutaUrl.Substring(prefijoAnterior.Length);
                esRutaAnterior = true;
            }

            if (!string.IsNullOrWhiteSpace(nombreArchivo))
            {
                rutaArchivo = ObtenerRutaFotoPerfilSegura(
                    id,
                    nombreArchivo,
                    permitirRutaAnterior: esRutaAnterior);
            }

            // Quitar la referencia de la base de datos.
            usuario.UrlFotoPerfil = null;
            await _context.SaveChangesAsync();


            // Eliminar el archivo si existe.
            if (rutaArchivo != null && System.IO.File.Exists(rutaArchivo))
            {
                try
                {
                    System.IO.File.Delete(rutaArchivo);
                }
                catch (IOException)
                {
                    // La referencia ya se quitó de la base de datos.
                    // El archivo puede eliminarse manualmente más adelante.
                }
                catch (UnauthorizedAccessException)
                {
                    // La referencia ya se quitó de la base de datos.
                    // No se debe presentar la operación como fallida
                    // solo porque el servidor no pudo borrar el archivo.
                }
            }

            return Ok(new
            {
                mensaje = "La referencia a la fotografía se eliminó correctamente."
            });
        }

        private string? ObtenerRutaFotoPerfilSegura(
            int idUsuario,
            string nombreArchivo,
            bool permitirRutaAnterior = false)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivo) ||
                Path.GetFileName(nombreArchivo) != nombreArchivo ||
                nombreArchivo.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return null;
            }

            string carpetaWeb = _environment.WebRootPath
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            string carpetaPerfiles = Path.GetFullPath(
                Path.Combine(carpetaWeb, "Images", "Perfiles"));

            string carpetaUsuario = Path.GetFullPath(
                Path.Combine(carpetaPerfiles, idUsuario.ToString()));

            string rutaArchivo = permitirRutaAnterior
                ? Path.GetFullPath(Path.Combine(carpetaPerfiles, nombreArchivo))
                : Path.GetFullPath(Path.Combine(carpetaUsuario, nombreArchivo));

            string separador = Path.DirectorySeparatorChar.ToString();

            bool dentroDeCarpetaUsuario = rutaArchivo.StartsWith(
                carpetaUsuario + separador,
                StringComparison.OrdinalIgnoreCase);

            bool dentroDeCarpetaAnterior = permitirRutaAnterior &&
                rutaArchivo.StartsWith(
                    carpetaPerfiles + separador,
                    StringComparison.OrdinalIgnoreCase) &&
                !rutaArchivo.StartsWith(
                    carpetaUsuario + separador,
                    StringComparison.OrdinalIgnoreCase);

            if (!dentroDeCarpetaUsuario && !dentroDeCarpetaAnterior)
            {
                return null;
            }

            return rutaArchivo;
        }
    }
}

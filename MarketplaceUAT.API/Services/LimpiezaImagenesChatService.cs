using MarketplaceUAT.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceUAT.API.Services
{
    public class LimpiezaImagenesChatService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LimpiezaImagenesChatService> _logger;

        private static readonly TimeSpan IntervaloRevision =
            TimeSpan.FromHours(6);

        private static readonly TimeSpan AntiguedadMinima =
            TimeSpan.FromHours(24);

        public LimpiezaImagenesChatService(
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment environment,
            ILogger<LimpiezaImagenesChatService> logger)
        {
            _scopeFactory = scopeFactory;
            _environment = environment;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            // Espera un poco para permitir que la API termine de iniciar.
            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await LimpiarArchivosHuerfanosAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Ocurrió un error durante la limpieza de imágenes del chat.");
                }

                try
                {
                    await Task.Delay(
                        IntervaloRevision,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task LimpiarArchivosHuerfanosAsync(
            CancellationToken cancellationToken)
        {
            string carpetaChat = Path.GetFullPath(
                Path.Combine(
                    _environment.WebRootPath
                        ?? Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot"),
                    "Images",
                    "Chat"));

            if (!Directory.Exists(carpetaChat))
                return;

            // Evita recorrer la carpeta raíz si es un enlace simbólico.
            var atributosRaiz = File.GetAttributes(carpetaChat);

            if ((atributosRaiz & FileAttributes.ReparsePoint) != 0)
            {
                _logger.LogWarning(
                    "Se omitió la limpieza porque Images/Chat es un punto de reanálisis.");

                return;
            }

            // Reúne las URL de todos los registros, incluidos los inactivos.
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<MarketplaceContext>();

            var urlsImagenes = await context.ImagenesMensajes
                .AsNoTracking()
                .Select(i => i.UrlImagen)
                .ToListAsync(cancellationToken);

            var urlsAntiguasMensajes = await context.Mensajes
                .AsNoTracking()
                .Where(m => m.UrlImagen != null)
                .Select(m => m.UrlImagen!)
                .ToListAsync(cancellationToken);

            var rutasReferenciadas = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var url in urlsImagenes.Concat(urlsAntiguasMensajes))
            {
                if (IntentarObtenerRutaRelativa(url, out string ruta))
                {
                    rutasReferenciadas.Add(ruta);
                }
            }

            DateTime limiteAntiguedad =
                DateTime.UtcNow.Subtract(AntiguedadMinima);

            // Recorre las carpetas sin seguir puntos de reanálisis.
            var carpetasPendientes = new Stack<string>();

            carpetasPendientes.Push(carpetaChat);

            while (carpetasPendientes.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string carpetaActual = carpetasPendientes.Pop();

                string[] elementos;

                try
                {
                    elementos = Directory.GetFileSystemEntries(carpetaActual);
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "No se pudo recorrer la carpeta: {Carpeta}",
                        carpetaActual);

                    continue;
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "No hay permisos para recorrer la carpeta: {Carpeta}",
                        carpetaActual);

                    continue;
                }

                foreach (string elemento in elementos)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var atributos = File.GetAttributes(elemento);

                        // No sigue enlaces simbólicos ni otros puntos de reanálisis.
                        if ((atributos & FileAttributes.ReparsePoint) != 0)
                            continue;

                        // Si es una carpeta normal, la agrega al recorrido.
                        if ((atributos & FileAttributes.Directory) != 0)
                        {
                            carpetasPendientes.Push(elemento);
                            continue;
                        }

                        string extension = Path.GetExtension(elemento)
                            .ToLowerInvariant();

                        if (extension != ".jpg" &&
                            extension != ".jpeg" &&
                            extension != ".png" &&
                            extension != ".webp")
                        {
                            continue;
                        }

                        var informacion = new FileInfo(elemento);

                        // No toca archivos recientes.
                        if (informacion.LastWriteTimeUtc > limiteAntiguedad)
                            continue;

                        string rutaRelativa = Path.GetRelativePath(
                                carpetaChat,
                                elemento)
                            .Replace('\\', '/');

                        // Conserva cualquier archivo referenciado por la base de datos.
                        if (rutasReferenciadas.Contains(rutaRelativa))
                            continue;

                        System.IO.File.Delete(elemento);

                        _logger.LogInformation(
                            "Se eliminó un archivo huérfano del chat: {Archivo}",
                            rutaRelativa);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "No se pudo procesar el archivo: {Archivo}",
                            elemento);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "No hay permisos para procesar el archivo: {Archivo}",
                            elemento);
                    }
                }
            }
        }

        private static bool IntentarObtenerRutaRelativa(
    string? url,
    out string rutaRelativa)
        {
            rutaRelativa = string.Empty;

            if (string.IsNullOrWhiteSpace(url))
                return false;

            if (!Uri.TryCreate(
                    url,
                    UriKind.Absolute,
                    out var uri))
            {
                return false;
            }

            // Solo se permiten HTTP y HTTPS.
            if (uri.Scheme != Uri.UriSchemeHttp &&
                uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            // Rechaza URL que incluyan credenciales.
            if (!string.IsNullOrEmpty(uri.UserInfo))
                return false;

            // No se permiten parámetros ni fragmentos.
            if (!string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment))
            {
                return false;
            }

            // Rechaza segmentos de ruta codificados.
            string[] segmentos = uri.AbsolutePath.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries);

            if (segmentos.Any(segmento =>
                    Uri.UnescapeDataString(segmento) != segmento))
            {
                return false;
            }

            // Solo reconoce URLs con el formato:
            // Images/Chat/{idConversacion}/{archivo}
            if (segmentos.Length != 4 ||
                !segmentos[0].Equals(
                    "Images",
                    StringComparison.OrdinalIgnoreCase) ||
                !segmentos[1].Equals(
                    "Chat",
                    StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(
                    segmentos[2],
                    out int idConversacion) ||
                idConversacion <= 0 ||
                string.IsNullOrWhiteSpace(segmentos[3]) ||
                segmentos[3] != Path.GetFileName(segmentos[3]) ||
                segmentos[3].Contains('\\') ||
                segmentos[3].Contains('/') ||
                segmentos[3].Contains(':'))
            {
                return false;
            }

            rutaRelativa = $"{idConversacion}/{segmentos[3]}";

            return true;
        }
    }
}

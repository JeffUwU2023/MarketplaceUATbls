using MarketplaceUAT.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceUAT.API.BackgroundServices
{
    public class LimpiezaRefreshTokensService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LimpiezaRefreshTokensService> _logger;

        public LimpiezaRefreshTokensService(
            IServiceScopeFactory scopeFactory,
            ILogger<LimpiezaRefreshTokensService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var context = scope.ServiceProvider
                        .GetRequiredService<MarketplaceContext>();

                    int registrosActualizados = await context.RefreshTokens
                        .Where(r =>
                            r.FechaLimiteReintento != null &&
                            r.FechaLimiteReintento <= DateTime.UtcNow &&
                            r.RespuestaRenovacionCifrada != null)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(
                                r => r.RespuestaRenovacionCifrada,
                                (string?)null)
                            .SetProperty(
                                r => r.FechaLimiteReintento,
                                (DateTime?)null),
                            stoppingToken);

                    if (registrosActualizados > 0)
                    {
                        _logger.LogInformation(
                            "Se limpiaron {Cantidad} respuestas de renovación expiradas.",
                            registrosActualizados);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error al limpiar las respuestas de renovación.");
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken);
            }
        }
    }
}
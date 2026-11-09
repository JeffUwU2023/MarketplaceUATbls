using MarketplaceUAT.API.BackgroundServices;
using MarketplaceUAT.API.DataProtection;
using MarketplaceUAT.API.Services;
using MarketplaceUAT.Data.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<LimpiezaRefreshTokensService>();

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    System.Text.Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key no está configurada o tiene menos de 32 bytes."
    );
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException(
        "Falta configurar Jwt:Issuer."
    );
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "Falta configurar Jwt:Audience."
    );
}

builder.Services.AddDbContext<MarketplaceContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MarketplaceConnection")
    )
);

builder.Services.AddDataProtection()
    .SetApplicationName("MarketplaceUAT");

builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidAlgorithms = new[]
            {
                   Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256
            },

            ClockSkew = TimeSpan.Zero,

            RoleClaimType = System.Security.Claims.ClaimTypes.Role,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,

            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(jwtKey)
                
            )
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var claimIdUsuario = context.Principal?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(claimIdUsuario, out int idUsuario) ||
                    idUsuario <= 0)
                {
                    context.Fail("El token no contiene un usuario válido.");
                    return;
                }

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<MarketplaceContext>();

                var usuarioActivo = await db.Usuarios
                    .AnyAsync(u =>
                        u.IdUsuario == idUsuario &&
                        u.Activo);

                if (!usuarioActivo)
                {
                    context.Fail("La cuenta no existe o está desactivada.");
                }
            }
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Ingresa el token JWT."
        });

    options.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

builder.Services.AddHostedService<LimpiezaImagenesChatService>();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature =
            context.Features.Get<
                Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();

        if (exceptionFeature?.Error != null)
        {
            var logger = context.RequestServices
                .GetRequiredService<
                    ILogger<Program>>();

            logger.LogError(
                exceptionFeature.Error,
                "Ocurrió un error inesperado al procesar la solicitud.");
        }

        context.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            mensaje = "Error temporal de diagnóstico.",
            error = exceptionFeature?.Error?.GetBaseException().Message
        });
    });
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("./swagger/v1/swagger.json", "Mi API Marketplace v1");

    c.RoutePrefix = string.Empty;
});


if (app.Environment.IsDevelopment())
{

}

app.UseAuthentication();

app.Use(async (context, next) =>
{
    var ruta = context.Request.Path;

    if (!ruta.StartsWithSegments(
            new PathString("/Images/Chat"),
            StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    var segmentos = ruta.Value!.Split(
    '/',
    StringSplitOptions.RemoveEmptyEntries);

    if (segmentos.Length != 4 ||
        !int.TryParse(segmentos[2], out int idConversacion) ||
        idConversacion <= 0)
    {
        context.Response.StatusCode =
            StatusCodes.Status404NotFound;
        return;
    }

    if (context.User.Identity?.IsAuthenticated != true)
    {
        context.Response.StatusCode =
            StatusCodes.Status401Unauthorized;
        return;
    }

    var claim = context.User.FindFirst(
        System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    if (!int.TryParse(claim, out int idUsuario) ||
        idUsuario <= 0)
    {
        context.Response.StatusCode =
            StatusCodes.Status401Unauthorized;
        return;
    }

    var db = context.RequestServices
        .GetRequiredService<MarketplaceContext>();

    var conversacion = await db.Conversaciones
    .AsNoTracking()
    .FirstOrDefaultAsync(c =>
        c.IdConversacion == idConversacion &&
        (
            c.IdUsuarioComprador == idUsuario ||
            c.IdUsuarioVendedor == idUsuario
        ));

    if (conversacion == null)
    {
        context.Response.StatusCode =
            StatusCodes.Status404NotFound;
        return;
    }

    if (!conversacion.Activa)
    {
        context.Response.StatusCode =
            StatusCodes.Status404NotFound;
        return;
    }

    string nombreArchivo = segmentos[3];

    var imagenActiva = await (
    from imagen in db.ImagenesMensajes.AsNoTracking()
    join mensaje in db.Mensajes.AsNoTracking()
        on imagen.IdMensaje equals mensaje.IdMensaje
    where imagen.Activo
        && imagen.UrlImagen.EndsWith("/" + nombreArchivo)
        && mensaje.IdConversacion == idConversacion
        && mensaje.Activo
    select imagen.IdImagenMensaje
).AnyAsync();

    if (!imagenActiva)
    {
        context.Response.StatusCode =
            StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();




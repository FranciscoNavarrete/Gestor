using System.Text;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Domain.Entities;
using GestorPOS.Infrastructure;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Railway asigna el puerto en runtime vía la variable PORT — en local no está seteada,
// así que esto no toca el --urls que ya se usa para desarrollo.
var puertoRailway = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(puertoRailway))
    builder.WebHost.UseUrls($"http://+:{puertoRailway}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta configurar Jwt:Key.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Sin esto, ASP.NET renombra claims estándar del JWT (p.ej. "sub" -> nameidentifier),
        // lo que rompería la lectura de UsuarioId en TenantContext.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Un negocio desactivado (p. ej. por suscripción cancelada) no tiene que seguir entrando con una
        // sesión que ya tenía abierta: se revisa el negocio en cada request (con una caché corta).
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async contexto =>
            {
                var claimTenant = contexto.Principal?.FindFirst("tenant_id")?.Value;
                if (!Guid.TryParse(claimTenant, out var tenantId)) return;

                var cache = contexto.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                if (!cache.TryGetValue($"tenant-activo:{tenantId}", out bool activo))
                {
                    var db = contexto.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                    activo = await db.Tenants.IgnoreQueryFilters().Where(t => t.Id == tenantId).Select(t => t.Activo).FirstOrDefaultAsync();
                    cache.Set($"tenant-activo:{tenantId}", activo, TimeSpan.FromSeconds(60));
                }

                if (!activo) contexto.Fail("El negocio está desactivado.");
            }
        };
    });
builder.Services.AddMemoryCache();
builder.Services.AddAuthorization(options =>
{
    // El panel admin usa un JWT propio con el claim "admin_rol", separado del JWT de tenant.
    options.AddPolicy("AdminPanel", policy => policy.RequireClaim("admin_rol"));
    // Subset de endpoints admin (gestión de usuarios, feature flags) que un Vendedor no puede tocar.
    options.AddPolicy("AdminOperador", policy => policy.RequireClaim("admin_rol", nameof(AdminRol.Operador)));
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Crea el usuario operador inicial si no existe ningún admin todavía.
    if (!db.AdminUsuarios.Any())
    {
        var passwordInicial = app.Configuration["Admin:PasswordInicial"] ?? "Admin123!";
        db.AdminUsuarios.Add(AdminUsuario.Crear(
            email:        app.Configuration["Admin:Email"] ?? "admin@gestorpos.com",
            passwordHash: BCrypt.Net.BCrypt.HashPassword(passwordInicial),
            nombre:       "Operador",
            rol:          AdminRol.Operador));
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var exception = feature?.Error;

        // AppException y ArgumentException son violaciones de reglas de negocio/invariantes
        // de dominio (input inválido del cliente) -> 400 con el mensaje. El resto son bugs -> 500 genérico.
        var esErrorDeCliente = exception is AppException or ArgumentException;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = esErrorDeCliente
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;

        var mensaje = esErrorDeCliente ? exception!.Message : "Ocurrió un error inesperado.";
        await context.Response.WriteAsJsonAsync(new { error = mensaje });
    });
});

// Railway termina TLS en su proxy y reenvía como HTTP interno — sin esto,
// UseHttpsRedirection entraría en loop de redirects creyendo que cada request es HTTP.
// El proxy de Railway no es loopback, así que por defecto .NET ignoraba esos headers y la IP de cada request
// (la que queda en la constancia de aceptación de términos) era la del proxy interno. Se confía en el proxy
// de la plataforma: la app solo es alcanzable a través de él.
var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeaders.KnownNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaders);
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapControllers();

app.Run();

using System.Text;
using GestorPOS.Application.Common.Exceptions;
using GestorPOS.Infrastructure;
using GestorPOS.Infrastructure.Persistence;
using GestorPOS.WebAPI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
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
    });
builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Aplica migraciones pendientes en cada arranque — evita el paso manual de
// "dotnet-ef database update" en cada deploy. Es idempotente (no hace nada si ya está al día).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
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
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseMiddleware<AdminApiKeyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapControllers();

app.Run();

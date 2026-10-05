namespace GestorPOS.Application.Admin.Dtos;

public record TenantResumenDto(
    Guid Id, string Nombre, string Slug, bool Activo, DateTime FechaCreacion,
    Guid? VendedorId, string? VendedorNombre,
    int? FluxoClienteId = null, int? FluxoSuscripcionId = null, string? FluxoInitPoint = null,
    string? FluxoEstado = null, bool FluxoPrimerCobroAprobado = false, bool FluxoAjustePendiente = false);

/// <summary>Link de pago pendiente de un negocio; Link es null si ya no está pendiente.</summary>
public record LinkPagoDto(string Estado, string? Link);

public record TenantFeatureDto(Guid Id, string Clave, bool Habilitado);

/// <summary>Alta de un negocio hecha por el operador de GestorPOS. El email/password que se cargan
/// acá son las credenciales que se le entregan al cliente — no hay auto-registro público.
/// VendedorId es opcional: si quien crea el negocio es un Vendedor, el service lo autoasigna e
/// ignora lo que venga acá; si es Operador, puede elegir a qué vendedor atribuirlo (o ninguno).</summary>
public record CrearNegocioRequest(
    string NombreNegocio, string NombreAdmin, string Email, string Password,
    Guid? VendedorId = null, int? MpPlanId = null, string? CardToken = null);

public record FluxoPlanDto(
    int MpPlanId, string Nombre, decimal Monto, string Moneda, string TipoFrecuencia, int Frecuencia, int DiasGratis,
    decimal? MontoPrimerCobro = null);

public record AdminUsuarioDto(Guid Id, string Email, string Nombre, string Rol, bool Activo, DateTime CreadoUtc);

public record CrearAdminUsuarioRequest(string Email, string Password, string Nombre, string Rol);

/// <summary>Ventas de vendedores entre dos fechas (hora de Argentina). Una venta es un negocio dado de
/// alta cuya suscripción se confirmó alguna vez; las bajas posteriores no la descuentan.</summary>
public record ReporteVentasDto(
    DateOnly Desde, DateOnly Hasta,
    ReporteResumenDto Resumen,
    IReadOnlyList<ReporteVendedorDto> Vendedores,
    IReadOnlyList<ReporteVentaItemDto> Items);

/// <summary>Las primeras N ventas de cada mes (por vendedor) se pagan a una tarifa y el resto a otra;
/// por eso el resumen informa cuántas cayeron en cada tarifa, para mostrar "3 × $35.000 + 1 × $40.000".</summary>
public record ReporteResumenDto(
    int Ventas, int Pendientes, decimal Comision,
    int VentasPrimeras, decimal TarifaPrimeras, int VentasSiguientes, decimal TarifaSiguientes,
    decimal Bono = 0, int BonoVentas = 0, decimal BonoMonto = 0);

/// <summary>Bono: lo ganado en el período. VentasMes: ventas cobradas del vendedor en el mes de la fecha
/// "hasta", para mostrar cuánto falta para el bono.</summary>
public record ReporteVendedorDto(
    Guid VendedorId, string Nombre, int Ventas, int Pendientes, decimal Comision, decimal Bono = 0, int VentasMes = 0);

/// <summary>Estado: suscripto, baja (se cobró y después se canceló), esperando (autorizada, falta el primer
/// cobro), pendiente (el cliente no autorizó) o cancelada (nunca se cobró). Comision solo tiene valor para
/// las ventas con el primer cobro aprobado.</summary>
public record ReporteVentaItemDto(
    Guid TenantId, string Nombre, DateTime FechaAlta, Guid VendedorId, string VendedorNombre,
    string Estado, decimal? Comision, decimal? Bono = null);

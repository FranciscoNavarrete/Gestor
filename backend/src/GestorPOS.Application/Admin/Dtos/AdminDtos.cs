namespace GestorPOS.Application.Admin.Dtos;

public record TenantResumenDto(
    Guid Id, string Nombre, string Slug, bool Activo, DateTime FechaCreacion,
    Guid? VendedorId, string? VendedorNombre,
    int? FluxoClienteId = null, int? FluxoSuscripcionId = null, string? FluxoInitPoint = null,
    string? FluxoEstado = null, bool FluxoPrimerCobroAprobado = false, bool FluxoAjustePendiente = false,
    bool FluxoCobroRechazado = false, string? FluxoMotivoRechazo = null);

public record LiquidacionItemDto(Guid TenantId, string Nombre, DateTime FechaAltaUtc, int Orden, decimal Comision, decimal Bono);

/// <summary>Estado: Pendiente (cerrada, falta pagarla) o Pagada.</summary>
public record LiquidacionDto(
    Guid Id, Guid VendedorId, string VendedorNombre, int Anio, int Mes, string Estado, DateTime FechaCierreUtc,
    DateOnly? FechaPago, string? Nota, int Ventas, decimal TotalComision, decimal TotalBono, decimal Total,
    IReadOnlyList<LiquidacionItemDto> Items);

/// <summary>Un vendedor en el cierre de un mes. Ventas/Comision/Bono son de todas sus ventas cobradas del mes
/// (liquidadas y no); "SinLiquidar" es lo que todavía no está en ninguna liquidación; EsperandoCobro son ventas
/// del mes cuyo primer cobro aún no se aprobó (no se incluyen).</summary>
public record LiquidacionVendedorDto(
    Guid VendedorId, string Nombre, int Ventas, decimal Comision, decimal Bono,
    int VentasSinLiquidar, decimal ComisionSinLiquidar, decimal BonoSinLiquidar, int EsperandoCobro,
    IReadOnlyList<LiquidacionDto> Liquidaciones);

public record LiquidacionesMesDto(int Anio, int Mes, IReadOnlyList<LiquidacionVendedorDto> Vendedores);

public record PrevisualizacionLiquidacionDto(
    Guid VendedorId, string VendedorNombre, int Anio, int Mes, IReadOnlyList<LiquidacionItemDto> Items,
    decimal TotalComision, decimal TotalBono, decimal Total, int EsperandoCobro);

public record LiquidarRequest(Guid VendedorId, int Anio, int Mes);

public record PagarLiquidacionRequest(DateOnly? FechaPago, string? Nota);

/// <summary>Resumen financiero de la plataforma, armado con las suscripciones de Fluxo. Los importes son lo
/// programado (plan y fechas de cada suscripción), no lo efectivamente cobrado.</summary>
public record ResumenFinancieroDto(
    decimal IngresoMensual, int ClientesActivos, int EsperandoPrimerCobro, decimal PorCobrarPrimerosCobros,
    int AltasMes, int BajasMes, decimal PorcentajeBajas, decimal ACobrar30Dias,
    int CobrosRechazados, decimal MontoRechazado, int PausadosOSuspendidos,
    IReadOnlyList<ClienteEnRiesgoDto> EnRiesgo, IReadOnlyList<AltasMesDto> AltasPorMes);

/// <summary>Tipo: primer-cobro (el primer cobro fue rechazado), rechazado (un cobro mensual), pausado o suspendido.</summary>
public record ClienteEnRiesgoDto(
    Guid TenantId, string Nombre, string Tipo, string? Motivo, decimal Monto, DateTime? ProximoReintento);

public record AltasMesDto(int Anio, int Mes, int Altas);

/// <summary>Movimientos del panel de administración. Total es la cantidad que cumple los filtros; Items trae
/// como máximo los 500 más recientes.</summary>
public record MovimientosAdminDto(DateOnly Desde, DateOnly Hasta, int Total, IReadOnlyList<MovimientoAdminDto> Items);

public record MovimientoAdminDto(
    Guid Id, DateTime FechaUtc, Guid AdminId, string AdminNombre, string AdminRol,
    string Accion, string Entidad, Guid? EntidadId, string EntidadNombre, string? Detalle);

/// <summary>Historial de cobros de un negocio (viene de Mercado Pago, vía Fluxo).</summary>
public record CobrosNegocioDto(
    string Estado, decimal MontoMensual, DateTime? ProximoCobro, decimal? ProximoMonto, IReadOnlyList<CobroNegocioDto> Cobros);

public record CobroNegocioDto(
    DateTime? Fecha, decimal Monto, string Estado, string? Motivo, int Intento, DateTime? ProximoReintento, bool EsPrimerCobro);

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

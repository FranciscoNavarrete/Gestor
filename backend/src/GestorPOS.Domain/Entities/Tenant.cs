using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Nombre { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public bool Activo { get; private set; } = true;

    /// <summary>El negocio se creó pero todavía no se cumplió todo para usarlo: falta confirmar el primer pago
    /// y/o que el cliente autorice su suscripción. Mientras tanto no tiene acceso, y se activa solo.</summary>
    public bool PendienteActivacion { get; private set; }

    /// <summary>Ya se había contado como venta cobrada (comisión del vendedor) antes de que se le diera una suscripción
    /// nueva: la venta sigue contando aunque la suscripción nueva todavía no haya cobrado.</summary>
    public bool VentaPreviaCobrada { get; private set; }
    public string? Telefono { get; private set; }
    public byte[]? LogoData { get; private set; }
    public string? LogoContentType { get; private set; }
    public Guid? VendedorId { get; private set; }
    public int? FluxoClienteId { get; private set; }
    public int? FluxoSuscripcionId { get; private set; }

    private readonly List<TenantFeature> _features = new();
    public IReadOnlyCollection<TenantFeature> Features => _features.AsReadOnly();

    private Tenant() { }

    public static Tenant Crear(string nombre, string slug)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del negocio es obligatorio.", nameof(nombre));
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("El slug del negocio es obligatorio.", nameof(slug));

        return new Tenant
        {
            Nombre = nombre.Trim(),
            Slug = slug.Trim().ToLowerInvariant()
        };
    }

    public void Desactivar()
    {
        Activo = false;
        PendienteActivacion = false;
    }
    public void Activar() => Activo = true;

    public void EsperarActivacion()
    {
        Activo = false;
        PendienteActivacion = true;
    }

    public void ConfirmarActivacion()
    {
        Activo = true;
        PendienteActivacion = false;
    }

    public void MarcarVentaPreviaCobrada() => VentaPreviaCobrada = true;

    public void AsignarVendedor(Guid? vendedorId) => VendedorId = vendedorId;

    public void AsignarFluxo(int? clienteId, int? suscripcionId)
    {
        FluxoClienteId = clienteId;
        FluxoSuscripcionId = suscripcionId;
    }

    public void ActualizarDatos(string nombre, string? telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del negocio es obligatorio.", nameof(nombre));

        Nombre = nombre.Trim();
        Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
    }

    public void ActualizarLogo(byte[] datos, string contentType)
    {
        LogoData = datos;
        LogoContentType = contentType;
    }

    public void QuitarLogo()
    {
        LogoData = null;
        LogoContentType = null;
    }
}

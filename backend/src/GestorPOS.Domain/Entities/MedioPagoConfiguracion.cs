using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

public class MedioPagoConfiguracion : TenantEntity
{
    public string Nombre { get; private set; } = string.Empty;
    public bool Activo { get; private set; } = true;

    // El medio de pago sembrado como "Efectivo" al crear el tenant no se puede renombrar ni
    // desactivar: CajaService calcula el efectivo esperado comparando contra ese nombre exacto.
    public bool EsProtegido { get; private set; }

    private MedioPagoConfiguracion() { }

    public static MedioPagoConfiguracion Crear(Guid tenantId, string nombre, bool esProtegido = false)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del medio de pago es obligatorio.", nameof(nombre));

        return new MedioPagoConfiguracion
        {
            TenantId = tenantId,
            Nombre = nombre.Trim(),
            EsProtegido = esProtegido,
        };
    }

    public void Renombrar(string nombre)
    {
        if (EsProtegido)
            throw new InvalidOperationException("Este medio de pago no se puede renombrar.");
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del medio de pago es obligatorio.", nameof(nombre));
        Nombre = nombre.Trim();
    }

    public void Desactivar()
    {
        if (EsProtegido)
            throw new InvalidOperationException("Este medio de pago no se puede desactivar.");
        Activo = false;
    }

    public void Activar() => Activo = true;
}

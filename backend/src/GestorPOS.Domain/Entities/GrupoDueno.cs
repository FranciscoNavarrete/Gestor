using GestorPOS.Domain.Common;

namespace GestorPOS.Domain.Entities;

/// <summary>Agrupa los negocios de un mismo dueño (p. ej. quien tiene más de un local). Es un dato del panel
/// interno: no cambia cobros ni accesos, solo permite ver y filtrar juntos los negocios del dueño.</summary>
public class GrupoDueno : BaseEntity
{
    public string Nombre { get; private set; } = string.Empty;
    public string? Telefono { get; private set; }

    private GrupoDueno() { }

    public static GrupoDueno Crear(string nombre, string? telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del grupo es obligatorio.", nameof(nombre));

        return new GrupoDueno
        {
            Nombre = nombre.Trim(),
            Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim()
        };
    }
}

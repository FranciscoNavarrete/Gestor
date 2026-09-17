namespace GestorPOS.Infrastructure.Common;

/// <summary>Centraliza la conversión entre fechas "de calendario" (como las elige el usuario en un
/// selector de fecha, sin hora) y UTC (como se guarda todo en la base). Tratar una DateOnly como si
/// fuera directamente medianoche UTC corta el día ~3hs antes de tiempo para cualquier venta hecha de
/// noche en Argentina — por eso todo pasa por acá en vez de por ToDateTime(..., DateTimeKind.Utc).</summary>
public static class ZonaHoraria
{
    public static readonly TimeZoneInfo Argentina = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

    public static DateTime ConvertirAUtc(DateOnly fecha, TimeOnly hora)
    {
        var local = DateTime.SpecifyKind(fecha.ToDateTime(hora), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, Argentina);
    }

    public static DateOnly HoyEnArgentina() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Argentina));
}

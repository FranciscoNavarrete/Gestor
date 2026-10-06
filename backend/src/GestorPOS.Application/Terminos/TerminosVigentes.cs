namespace GestorPOS.Application.Terminos;

/// <summary>Los términos y condiciones en vigor. Al cambiar el texto de fondo hay que subir Version: cada negocio
/// vuelve a verlos y a aceptarlos en su próximo ingreso. Los importes no figuran acá a propósito, se informan en el alta.</summary>
public static class TerminosVigentes
{
    public const string Version = "1.0";

    public static readonly IReadOnlyList<string> Resumen =
    [
        "El cobro mensual es automático con la tarjeta registrada. Si la tarjeta es rechazada se reintenta, y el acceso puede suspenderse.",
        "Se puede cancelar cuando se quiera: el acceso sigue vigente hasta la próxima fecha de cobro. No hay reembolsos por períodos ya cobrados.",
        "Los datos del negocio y de sus clientes se usan solo para prestar el servicio.",
    ];

    public static readonly IReadOnlyList<TerminosSeccionDto> Secciones =
    [
        new("1. El servicio",
            "GestorPOS es un sistema de ventas y gestión para tu negocio: caja, stock, clientes, compras y reportes. " +
            "Lo usás desde el navegador o instalado como aplicación en tu dispositivo."),
        new("2. Alta y abono",
            "El alta se paga una sola vez y el abono se cobra cada mes, en los importes que se te informaron al contratar. " +
            "El abono mensual se paga siempre con tarjeta o con el link de pago de Mercado Pago, y empieza un período después del alta."),
        new("3. Cobro automático y rechazos",
            "El abono se cobra de forma automática en la tarjeta registrada. Si un cobro es rechazado, Mercado Pago lo reintenta " +
            "y se te avisa para que lo resuelvas. Mientras haya cobros sin resolver, el acceso puede suspenderse."),
        new("4. Cancelación",
            "Podés cancelar la suscripción cuando quieras. Al cancelar, el acceso sigue vigente hasta la próxima fecha de cobro. " +
            "No se reembolsan períodos ya cobrados. Los datos de tu negocio se conservan para que puedas volver."),
        new("5. Tus datos y privacidad",
            "Los datos de tu negocio y de tus clientes son tuyos. Los usamos solo para prestar el servicio y no los vendemos ni los " +
            "compartimos con terceros, salvo los que hacen falta para cobrarte (Mercado Pago). Podés pedir una copia o la baja de tus datos."),
        new("6. Responsabilidades",
            "Te comprometés a usar el sistema de buena fe y a cuidar tu usuario y contraseña. Hacemos copias de resguardo y " +
            "trabajamos para que el servicio esté siempre disponible, pero no podemos garantizar que no haya interrupciones."),
        new("7. Cambios en estos términos",
            "Si estos términos cambian, te los vamos a mostrar de nuevo la próxima vez que ingreses y vas a tener que aceptarlos para seguir usando el sistema."),
    ];

    public static TerminosDto ComoDto() => new(Version, Resumen, Secciones);
}

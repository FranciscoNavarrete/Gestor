using System.Globalization;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Application.Reportes;
using GestorPOS.Domain.Common;
using GestorPOS.Infrastructure.Common;
using GestorPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestorPOS.Infrastructure.Reportes;

public class ReportePdfService : IReportePdfService
{
    private static readonly CultureInfo Ci = CultureInfo.InvariantCulture;

    static ReportePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ReportePdfService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<byte[]> GenerarVentasPdfAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct = default)
    {
        var (nombreNegocio, logo) = await ObtenerDatosNegocioAsync(ct);

        var query = _db.Ventas.AsQueryable();
        if (desde is not null)
        {
            var desdeUtc = ZonaHoraria.ConvertirAUtc(desde.Value, TimeOnly.MinValue);
            query = query.Where(v => v.FechaCreacion >= desdeUtc);
        }
        if (hasta is not null)
        {
            var hastaUtc = ZonaHoraria.ConvertirAUtc(hasta.Value.AddDays(1), TimeOnly.MinValue);
            query = query.Where(v => v.FechaCreacion < hastaUtc);
        }

        var ventas = await query
            .OrderByDescending(v => v.FechaCreacion)
            .Select(v => new { v.FechaCreacion, v.Total, v.MedioPago })
            .ToListAsync(ct);

        var items = query.SelectMany(v => v.Items);
        var totalCosto = await items.SumAsync(i => (decimal?)(i.CostoUnitario * i.Cantidad), ct) ?? 0m;
        var totalVentas = ventas.Sum(v => v.Total);
        var gananciaNeta = totalVentas - totalCosto;

        var filas = ventas
            .Select(v => new[]
            {
                TimeZoneInfo.ConvertTimeFromUtc(v.FechaCreacion, ZonaHoraria.Argentina).ToString("dd/MM/yyyy HH:mm", Ci),
                v.MedioPago,
                $"${v.Total.ToString("N2", Ci)}",
            })
            .ToList();

        var subtitulo = ConstruirSubtituloRango(desde, hasta);

        var documento = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(30);
                pagina.DefaultTextStyle(x => x.FontSize(10));

                pagina.Header().Row(fila =>
                {
                    if (logo is not null)
                    {
                        fila.ConstantItem(40).Height(40).Image(logo).FitArea();
                        fila.ConstantItem(10);
                    }
                    fila.RelativeItem().Column(col =>
                    {
                        col.Item().Text(nombreNegocio).FontSize(18).Bold();
                        col.Item().PaddingTop(2).Text("Reporte de ventas").FontSize(13).SemiBold();
                        col.Item().PaddingTop(2).Text(subtitulo).FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                });

                pagina.Content().PaddingTop(16).Column(col =>
                {
                    col.Item().Row(fila =>
                    {
                        fila.RelativeItem().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().Text($"${totalVentas.ToString("N2", Ci)}").FontSize(16).Bold();
                            c.Item().Text($"Total vendido · {ventas.Count} venta{(ventas.Count == 1 ? "" : "s")}").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                        fila.ConstantItem(12);
                        fila.RelativeItem().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().Text($"${gananciaNeta.ToString("N2", Ci)}").FontSize(16).Bold();
                            c.Item().Text("Ganancia neta").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    col.Item().PaddingTop(16).Table(tabla =>
                    {
                        tabla.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                        });

                        tabla.Header(header =>
                        {
                            header.Cell().Element(CeldaEncabezado).Text("Fecha");
                            header.Cell().Element(CeldaEncabezado).Text("Medio de pago");
                            header.Cell().Element(CeldaEncabezado).AlignRight().Text("Total");
                        });

                        foreach (var fila in filas)
                        {
                            tabla.Cell().Element(CeldaCuerpo).Text(fila[0]);
                            tabla.Cell().Element(CeldaCuerpo).Text(fila[1]);
                            tabla.Cell().Element(CeldaCuerpo).AlignRight().Text(fila[2]);
                        }
                    });

                    if (filas.Count == 0)
                        col.Item().PaddingTop(20).AlignCenter().Text("No hay ventas en el período seleccionado.").FontColor(Colors.Grey.Darken1);
                });

                pagina.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        });

        return documento.GeneratePdf();
    }

    public async Task<byte[]> GenerarStockPdfAsync(string? busqueda, bool soloBajoStock, CancellationToken ct = default)
    {
        var (nombreNegocio, logo) = await ObtenerDatosNegocioAsync(ct);

        var query = _db.Productos.Where(p => p.Activo);
        if (soloBajoStock)
            query = query.Where(p => p.StockActual <= p.StockMinimo);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim().ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(termino) ||
                p.Sku.ToLower().Contains(termino) ||
                (p.CategoriaId != null && _db.Categorias.Any(c => c.Id == p.CategoriaId && c.Nombre.ToLower().Contains(termino))));
        }

        var productos = await query
            .OrderBy(p => p.Nombre)
            .Select(p => new
            {
                p.Nombre,
                p.Sku,
                CategoriaNombre = p.CategoriaId != null
                    ? _db.Categorias.Where(c => c.Id == p.CategoriaId).Select(c => c.Nombre).FirstOrDefault()
                    : null,
                p.StockActual,
                p.StockMinimo,
                p.Costo,
                p.EnStockMinimo,
            })
            .ToListAsync(ct);

        var valorizadoTotal = productos.Sum(p => p.StockActual * p.Costo);

        var documento = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(30);
                pagina.DefaultTextStyle(x => x.FontSize(10));

                pagina.Header().Row(fila =>
                {
                    if (logo is not null)
                    {
                        fila.ConstantItem(40).Height(40).Image(logo).FitArea();
                        fila.ConstantItem(10);
                    }
                    fila.RelativeItem().Column(col =>
                    {
                        col.Item().Text(nombreNegocio).FontSize(18).Bold();
                        col.Item().PaddingTop(2).Text("Reporte de stock actual").FontSize(13).SemiBold();
                        col.Item().PaddingTop(2).Text($"{productos.Count} producto{(productos.Count == 1 ? "" : "s")} · Valorizado total: ${valorizadoTotal.ToString("N2", Ci)}")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                });

                pagina.Content().PaddingTop(16).Table(tabla =>
                {
                    tabla.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                    });

                    tabla.Header(header =>
                    {
                        header.Cell().Element(CeldaEncabezado).Text("Producto");
                        header.Cell().Element(CeldaEncabezado).Text("Categoría");
                        header.Cell().Element(CeldaEncabezado).AlignRight().Text("Stock");
                        header.Cell().Element(CeldaEncabezado).AlignRight().Text("Mínimo");
                        header.Cell().Element(CeldaEncabezado).AlignRight().Text("Valorizado");
                    });

                    foreach (var p in productos)
                    {
                        var colorTexto = p.EnStockMinimo ? Colors.Red.Darken1 : Colors.Black;
                        tabla.Cell().Element(CeldaCuerpo).Text($"{p.Nombre}\n{p.Sku}").FontColor(colorTexto);
                        tabla.Cell().Element(CeldaCuerpo).Text(p.CategoriaNombre ?? "—").FontColor(colorTexto);
                        tabla.Cell().Element(CeldaCuerpo).AlignRight().Text($"{p.StockActual} u.").FontColor(colorTexto);
                        tabla.Cell().Element(CeldaCuerpo).AlignRight().Text($"{p.StockMinimo} u.").FontColor(colorTexto);
                        tabla.Cell().Element(CeldaCuerpo).AlignRight().Text($"${(p.StockActual * p.Costo).ToString("N2", Ci)}").FontColor(colorTexto);
                    }
                });

                pagina.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        });

        return documento.GeneratePdf();
    }

    public async Task<byte[]> GenerarDeudasPdfAsync(CancellationToken ct = default)
    {
        var (nombreNegocio, logo) = await ObtenerDatosNegocioAsync(ct);

        var deudas = await _db.Clientes
            .Select(c => new
            {
                c.Nombre,
                c.Telefono,
                CantidadVentasACuenta = _db.Ventas.Count(v => v.ClienteId == c.Id && v.MedioPago == CuentaCorriente.MedioPago),
                Saldo = (_db.Ventas.Where(v => v.ClienteId == c.Id && v.MedioPago == CuentaCorriente.MedioPago).Sum(v => (decimal?)v.Total) ?? 0m)
                    - (_db.PagosCuenta.Where(p => p.ClienteId == c.Id).Sum(p => (decimal?)p.Monto) ?? 0m),
            })
            .Where(x => x.Saldo > 0)
            .OrderByDescending(x => x.Saldo)
            .ToListAsync(ct);

        var totalAdeudado = deudas.Sum(d => d.Saldo);

        var documento = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(30);
                pagina.DefaultTextStyle(x => x.FontSize(10));

                pagina.Header().Row(fila =>
                {
                    if (logo is not null)
                    {
                        fila.ConstantItem(40).Height(40).Image(logo).FitArea();
                        fila.ConstantItem(10);
                    }
                    fila.RelativeItem().Column(col =>
                    {
                        col.Item().Text(nombreNegocio).FontSize(18).Bold();
                        col.Item().PaddingTop(2).Text("Reporte de deudas").FontSize(13).SemiBold();
                        col.Item().PaddingTop(2).Text($"{deudas.Count} cliente{(deudas.Count == 1 ? "" : "s")} con saldo pendiente")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                });

                pagina.Content().PaddingTop(16).Column(col =>
                {
                    col.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                    {
                        c.Item().Text($"${totalAdeudado.ToString("N2", Ci)}").FontSize(16).Bold().FontColor(Colors.Red.Darken1);
                        c.Item().Text("Total adeudado").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });

                    col.Item().PaddingTop(16).Table(tabla =>
                    {
                        tabla.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                        });

                        tabla.Header(header =>
                        {
                            header.Cell().Element(CeldaEncabezado).Text("Cliente");
                            header.Cell().Element(CeldaEncabezado).Text("Teléfono");
                            header.Cell().Element(CeldaEncabezado).AlignRight().Text("Ventas a cuenta");
                            header.Cell().Element(CeldaEncabezado).AlignRight().Text("Saldo");
                        });

                        foreach (var d in deudas)
                        {
                            tabla.Cell().Element(CeldaCuerpo).Text(d.Nombre ?? "Sin nombre");
                            tabla.Cell().Element(CeldaCuerpo).Text(d.Telefono);
                            tabla.Cell().Element(CeldaCuerpo).AlignRight().Text(d.CantidadVentasACuenta.ToString());
                            tabla.Cell().Element(CeldaCuerpo).AlignRight().Text($"${d.Saldo.ToString("N2", Ci)}").FontColor(Colors.Red.Darken1);
                        }
                    });

                    if (deudas.Count == 0)
                        col.Item().PaddingTop(20).AlignCenter().Text("No hay clientes con saldo pendiente.").FontColor(Colors.Grey.Darken1);
                });

                pagina.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        });

        return documento.GeneratePdf();
    }

    private async Task<(string Nombre, byte[]? Logo)> ObtenerDatosNegocioAsync(CancellationToken ct)
    {
        var tenant = await _db.Tenants
            .Where(t => t.Id == _tenantContext.TenantId)
            .Select(t => new { t.Nombre, t.LogoData })
            .FirstOrDefaultAsync(ct);

        return (tenant?.Nombre ?? "", tenant?.LogoData);
    }

    private static string ConstruirSubtituloRango(DateOnly? desde, DateOnly? hasta)
    {
        if (desde is null && hasta is null) return "Todas las ventas";
        if (desde is not null && hasta is not null) return $"Del {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}";
        if (desde is not null) return $"Desde el {desde:dd/MM/yyyy}";
        return $"Hasta el {hasta:dd/MM/yyyy}";
    }

    private static QuestPDF.Infrastructure.IContainer CeldaEncabezado(QuestPDF.Infrastructure.IContainer contenedor)
        => contenedor.DefaultTextStyle(x => x.SemiBold()).Padding(6).BorderBottom(1).BorderColor(Colors.Grey.Darken1);

    private static QuestPDF.Infrastructure.IContainer CeldaCuerpo(QuestPDF.Infrastructure.IContainer contenedor)
        => contenedor.ShowEntire().Padding(6).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
}

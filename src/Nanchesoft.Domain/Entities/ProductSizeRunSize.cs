using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductSizeRunSize : BaseEntity
{
    public Guid ProductSizeRunId { get; set; }
    public ProductSizeRun? ProductSizeRun { get; set; }

    // Posición horizontal: sustituye T1, T2, T3... de Orange.
    public int Sequence { get; set; }

    // Valor real usado para SKU/código de barras/talla técnica.
    public string SizeCode { get; set; } = string.Empty;

    // Valor visible en capturas/reportes: sustituye M1, M2, M3...
    public string DisplayLabel { get; set; } = string.Empty;

    // Etiqueta para código de barras: sustituye necesidades tipo N1..N30.
    public string BarcodeLabel { get; set; } = string.Empty;

    // Factor o agrupador adicional: sustituye F1..F30 cuando se use.
    public string FactorLabel { get; set; } = string.Empty;

    // Proporción/pares por posición: sustituye P1..P20.
    public decimal Proportion { get; set; }

    public bool IsVisible { get; set; } = true;
}

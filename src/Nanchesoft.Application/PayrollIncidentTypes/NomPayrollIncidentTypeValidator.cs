using System.Text.RegularExpressions;

namespace Nanchesoft.Application.PayrollIncidentTypes;

public static partial class NomPayrollIncidentTypeValidator
{
    public static readonly HashSet<string> IncidentCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "DEDUCCION", "PERCEPCION", "INFORMATIVA"
    };

    public static readonly HashSet<string> AffectTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "SUMA", "RESTA", "NO_AFECTA"
    };

    public static readonly HashSet<string> PayrollConceptTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "FALTA", "RETARDO", "HORAS_EXTRA", "BONO", "COMISION", "VACACIONES", "INCAPACIDAD", "PRESTAMO", "DESCUENTO_DANOS", "OTRO"
    };

    public static List<string> Validate(NomPayrollIncidentTypeRequest request)
    {
        var errors = new List<string>();
        var code = NormalizeCode(request.Code);
        var name = NormalizeText(request.Name);
        var category = NormalizeEnum(request.IncidentCategory);
        var affectType = NormalizeEnum(request.AffectType);
        var payrollConceptType = NormalizeEnum(request.PayrollConceptType);
        var color = NormalizeColor(request.Color, category);

        if (string.IsNullOrWhiteSpace(code))
            errors.Add("Code es obligatorio.");

        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Name es obligatorio.");

        if (!IncidentCategories.Contains(category))
            errors.Add("IncidentCategory no es valido.");

        if (!AffectTypes.Contains(affectType))
            errors.Add("AffectType no es valido.");

        if (!PayrollConceptTypes.Contains(payrollConceptType))
            errors.Add("PayrollConceptType no es valido.");

        if (!string.IsNullOrWhiteSpace(color) && !HexColorRegex().IsMatch(color))
            errors.Add("Color debe ser hexadecimal (#RGB o #RRGGBB).");

        if (string.Equals(category, "DEDUCCION", StringComparison.OrdinalIgnoreCase) && !string.Equals(affectType, "RESTA", StringComparison.OrdinalIgnoreCase))
            errors.Add("Las deducciones deben usar AffectType RESTA.");

        if (string.Equals(category, "PERCEPCION", StringComparison.OrdinalIgnoreCase) && !string.Equals(affectType, "SUMA", StringComparison.OrdinalIgnoreCase))
            errors.Add("Las percepciones deben usar AffectType SUMA.");

        return errors;
    }

    public static string NormalizeCode(string? value) => NormalizeText(value).ToUpperInvariant().Replace(' ', '_');
    public static string NormalizeText(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    public static string NormalizeEnum(string? value) => NormalizeCode(value);

    public static string NormalizeColor(string? value, string? category)
    {
        var color = NormalizeText(value).ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(color))
            return color;

        return NormalizeEnum(category) switch
        {
            "DEDUCCION" => "#DC2626",
            "PERCEPCION" => "#16A34A",
            "INFORMATIVA" => "#2563EB",
            _ => string.Empty
        };
    }

    [GeneratedRegex("^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})$")]
    private static partial Regex HexColorRegex();
}

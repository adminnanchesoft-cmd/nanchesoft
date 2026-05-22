using System.Globalization;
using System.Text;
using Nanchesoft.Api.Ai.KnowledgeBase;

namespace Nanchesoft.Api.Ai.IntentRecognition;

public enum AiIntentKind
{
    Unknown = 0,
    Training = 1,
    PayrollSummary = 2,
    EmployeeIncidents = 3,
    TodayIncidents = 4,
    EmployeeStatus = 5,
    EmployeeLoans = 6,
    DepartmentsCost = 7,
    OvertimeEmployees = 8,
    ConceptTotals = 9,
    EmployeesWithoutDepartment = 10,
    EmployeesWithoutSalary = 11,
    AttendanceFaltas = 12,
    Greeting = 13,
    SmallTalk = 14
}

public sealed record IntentResult(
    AiIntentKind Kind,
    string? TrainingTopicId,
    string? ConceptHint,
    double Confidence
);

public static class IntentRecognizer
{
    public static IntentResult Recognize(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return new IntentResult(AiIntentKind.Unknown, null, null, 0);
        }

        var normalized = Normalize(question);

        if (IsGreeting(normalized))
        {
            return new IntentResult(AiIntentKind.Greeting, null, null, 0.95);
        }

        // 1) Try training topics first.
        // Match strategy: a keyword matches if EITHER (a) the full string is a substring
        // OR (b) all tokens of the keyword appear (in order) in the normalized question.
        foreach (var topic in TrainingKnowledgeBase.Topics)
        {
            foreach (var kw in topic.Keywords)
            {
                var nk = Normalize(kw);
                if (normalized.Contains(nk, StringComparison.Ordinal))
                {
                    return new IntentResult(AiIntentKind.Training, topic.Id, null, 0.95);
                }
                if (AllTokensInOrder(normalized, nk))
                {
                    return new IntentResult(AiIntentKind.Training, topic.Id, null, 0.85);
                }
            }
        }

        // 2) Operational intents (order matters: most specific first)
        if (ContainsAny(normalized, "faltas", "ausencias", "ausentismo", "inasistencias"))
        {
            return new IntentResult(AiIntentKind.AttendanceFaltas, null, ExtractHint(normalized, "faltas"), 0.85);
        }

        if (ContainsAny(normalized, "horas extra", "tiempo extra", "tiempo extraordinario", "horas extras", "extras"))
        {
            return new IntentResult(AiIntentKind.OvertimeEmployees, null, null, 0.9);
        }

        if (ContainsAny(normalized, "prestamo", "prestamos", "credito empleado", "creditos empleado"))
        {
            return new IntentResult(AiIntentKind.EmployeeLoans, null, null, 0.9);
        }

        if (ContainsAny(normalized, "sin departamento", "no tiene departamento", "no tienen departamento", "sin depto"))
        {
            return new IntentResult(AiIntentKind.EmployeesWithoutDepartment, null, null, 0.95);
        }

        if (ContainsAny(normalized, "sin sueldo", "no tiene sueldo", "no tienen sueldo", "sin salario del periodo", "sueldo del periodo"))
        {
            return new IntentResult(AiIntentKind.EmployeesWithoutSalary, null, null, 0.9);
        }

        if (normalized.Contains("departamento") && ContainsAny(normalized, "cuesta", "cuestan", "costo", "costoso", "mas caro", "mayor costo"))
        {
            return new IntentResult(AiIntentKind.DepartmentsCost, null, null, 0.9);
        }

        if (ContainsAny(normalized, "incidencia hoy", "incidencias hoy", "que pasa hoy", "hoy incidencia", "hoy incidencias"))
        {
            return new IntentResult(AiIntentKind.TodayIncidents, null, null, 0.9);
        }

        if (ContainsAny(normalized, "incidencia", "incidencias"))
        {
            return new IntentResult(AiIntentKind.EmployeeIncidents, null, ExtractDepartmentHint(normalized), 0.85);
        }

        if (ContainsAny(normalized, "se descontó", "descontaron", "descuento por", "se descontaron"))
        {
            var hint = ExtractConceptHint(normalized);
            return new IntentResult(AiIntentKind.ConceptTotals, null, hint, 0.85);
        }

        // "tijeras" is a specific recurrent concept used in the example
        if (ContainsAny(normalized, "tijeras", "tijera"))
        {
            return new IntentResult(AiIntentKind.ConceptTotals, null, "tijera", 0.9);
        }

        if (ContainsAny(normalized, "empleados activos", "personal activo", "trabajadores activos", "plantilla activa", "empleados estatus", "empleados con estatus"))
        {
            return new IntentResult(AiIntentKind.EmployeeStatus, null, "active", 0.85);
        }

        if (ContainsAny(normalized, "empleados de baja", "empleados dados de baja", "personal de baja", "bajas"))
        {
            return new IntentResult(AiIntentKind.EmployeeStatus, null, "terminated", 0.85);
        }

        if (ContainsAny(normalized, "cuanto se pagara de nomina", "cuanto se pagara nomina", "cuanto costara la raya",
            "cuanto cuesta la nomina", "cuanto es la nomina", "cuanto se debe pagar de nomina",
            "cuanto se paga de nomina", "cuanto se pagara", "cuanto cuesta nomina", "cuanto costara nomina",
            "la raya", "raya semanal", "raya quincenal", "nomina periodo"))
        {
            return new IntentResult(AiIntentKind.PayrollSummary, null, null, 0.9);
        }

        if (normalized.Contains("nomina") && ContainsAny(normalized, "pagar", "costo", "cuesta", "costara", "total"))
        {
            return new IntentResult(AiIntentKind.PayrollSummary, null, null, 0.8);
        }

        // Generic "empleados" → status active
        if (ContainsAny(normalized, "que empleados", "cuales empleados", "lista empleados", "listame empleados", "muestrame empleados"))
        {
            return new IntentResult(AiIntentKind.EmployeeStatus, null, "active", 0.6);
        }

        return new IntentResult(AiIntentKind.Unknown, null, null, 0);
    }

    private static bool IsGreeting(string normalized)
    {
        var greetings = new[] { "hola", "buenos dias", "buenas tardes", "buenas noches", "que tal", "saludos" };
        return greetings.Any(g => normalized.StartsWith(g, StringComparison.Ordinal) || normalized == g);
    }

    private static string? ExtractDepartmentHint(string normalized)
    {
        var idx = normalized.IndexOf(" de ", StringComparison.Ordinal);
        if (idx < 0) return null;
        var rest = normalized[(idx + 4)..].Trim();
        if (string.IsNullOrWhiteSpace(rest)) return null;
        return rest.TrimEnd('?', '.', '!');
    }

    private static string? ExtractConceptHint(string normalized)
    {
        var markers = new[] { "se descontó por ", "descontaron por ", "descuento por ", "se descontaron por " };
        foreach (var m in markers)
        {
            var idx = normalized.IndexOf(m, StringComparison.Ordinal);
            if (idx >= 0)
            {
                var rest = normalized[(idx + m.Length)..].Trim().TrimEnd('?', '.', '!');
                if (!string.IsNullOrWhiteSpace(rest)) return rest;
            }
        }
        return null;
    }

    private static string? ExtractHint(string normalized, string anchor)
    {
        var idx = normalized.IndexOf(anchor, StringComparison.Ordinal);
        if (idx < 0) return null;
        var rest = normalized[(idx + anchor.Length)..].Trim();
        return string.IsNullOrWhiteSpace(rest) ? null : rest.TrimEnd('?', '.', '!');
    }

    private static bool ContainsAny(string text, params string[] tokens)
        => tokens.Any(t => text.Contains(Normalize(t), StringComparison.Ordinal));

    private static bool AllTokensInOrder(string haystack, string needle)
    {
        var tokens = needle.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length < 2) return false;
        var cursor = 0;
        foreach (var t in tokens)
        {
            var idx = haystack.IndexOf(t, cursor, StringComparison.Ordinal);
            if (idx < 0) return false;
            cursor = idx + t.Length;
        }
        return true;
    }

    public static string Normalize(string value)
    {
        var lowered = value.ToLowerInvariant();
        var formD = lowered.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }
        return sb.ToString();
    }
}

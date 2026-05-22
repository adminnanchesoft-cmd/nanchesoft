using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Api.Ai.Dtos;
using Nanchesoft.Api.Ai.IntentRecognition;
using Nanchesoft.Api.Ai.KnowledgeBase;
using Nanchesoft.Api.Ai.Tools;
using Nanchesoft.Domain.Entities;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Ai.Services;

public sealed class AiOrchestrator
{
    private readonly NanchesoftDbContext _db;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AiOrchestrator(NanchesoftDbContext db)
    {
        _db = db;
    }

    public async Task<AiChatResponse> HandleAsync(AiScope scope, AiChatRequest request, CancellationToken ct = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Question))
        {
            return new AiChatResponse
            {
                Intent = "unknown",
                Answer = "No encontré información suficiente para responder eso.",
                Suggestions = DefaultSuggestions
            };
        }

        var question = request.Question.Trim();

        if (scope.TenantId is null || scope.UserId is null)
        {
            return new AiChatResponse
            {
                Intent = "unknown",
                Answer = "Tu sesión no tiene contexto de empresa o usuario. Inicia sesión de nuevo para continuar.",
                Suggestions = DefaultSuggestions
            };
        }

        var conversation = await ResolveOrCreateConversationAsync(scope, request.ConversationId, question, ct);

        var intent = IntentRecognizer.Recognize(question);

        var built = await BuildAnswerAsync(scope, intent, question, ct);

        // Persist user + assistant messages
        var userSeq = conversation.MessageCount + 1;
        var userMsg = new AiMessage
        {
            TenantId = scope.TenantId!.Value,
            ConversationId = conversation.Id,
            Role = "user",
            Content = question,
            SequenceNumber = userSeq,
            CreatedBy = scope.UserId?.ToString()
        };
        var assistantSeq = userSeq + 1;
        var dataJson = built.Data is null ? null : JsonSerializer.Serialize(built.Data, JsonOptions);
        var assistantMsg = new AiMessage
        {
            TenantId = scope.TenantId!.Value,
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = built.Answer,
            Intent = intent.Kind.ToString(),
            Endpoint = built.Endpoint,
            DataJson = dataJson,
            SequenceNumber = assistantSeq
        };
        _db.AiMessages.Add(userMsg);
        _db.AiMessages.Add(assistantMsg);

        conversation.MessageCount = assistantSeq;
        conversation.LastActivityAt = DateTime.UtcNow;
        conversation.UpdatedAt = DateTime.UtcNow;
        conversation.LastIntent = intent.Kind.ToString();
        if (string.IsNullOrWhiteSpace(conversation.Title) || conversation.Title == "Nueva conversación")
        {
            conversation.Title = TitleFromQuestion(question);
        }

        await _db.SaveChangesAsync(ct);

        return new AiChatResponse
        {
            ConversationId = conversation.Id,
            ConversationTitle = conversation.Title,
            Intent = intent.Kind.ToString(),
            Module = "hr_payroll",
            Answer = built.Answer,
            Endpoint = built.Endpoint,
            Route = built.Route,
            Data = built.Data,
            Suggestions = built.Suggestions
        };
    }

    private async Task<AiConversation> ResolveOrCreateConversationAsync(AiScope scope, Guid? conversationId, string question, CancellationToken ct)
    {
        if (conversationId.HasValue)
        {
            var existing = await _db.AiConversations
                .FirstOrDefaultAsync(x => x.Id == conversationId.Value
                                          && x.TenantId == scope.TenantId
                                          && x.UserId == scope.UserId
                                          && x.IsActive, ct);
            if (existing is not null) return existing;
        }

        var conv = new AiConversation
        {
            TenantId = scope.TenantId!.Value,
            CompanyId = scope.CompanyId,
            BranchId = scope.BranchId,
            UserId = scope.UserId!.Value,
            Title = TitleFromQuestion(question),
            Module = "hr_payroll",
            Status = "active",
            LastActivityAt = DateTime.UtcNow,
            CreatedBy = scope.UserId?.ToString()
        };
        _db.AiConversations.Add(conv);
        await _db.SaveChangesAsync(ct);
        return conv;
    }

    private static string TitleFromQuestion(string question)
    {
        var trimmed = question.Trim().TrimEnd('?', '¿', '.', '!', '¡');
        if (trimmed.Length <= 60) return trimmed;
        return trimmed[..57] + "...";
    }

    private async Task<BuiltAnswer> BuildAnswerAsync(AiScope scope, IntentResult intent, string question, CancellationToken ct)
    {
        switch (intent.Kind)
        {
            case AiIntentKind.Greeting:
                return new BuiltAnswer
                {
                    Answer = "¡Hola! Soy Nanchesoft IA, tu asistente de Nóminas y Recursos Humanos. Puedo ayudarte con consultas en vivo (incidencias, nómina por pagar, plantilla activa) y también enseñarte cómo usar el sistema. Pregunta lo que necesites.",
                    Suggestions = DefaultSuggestions
                };

            case AiIntentKind.Training:
                {
                    var topic = TrainingKnowledgeBase.Topics.FirstOrDefault(t => t.Id == intent.TrainingTopicId);
                    if (topic is null)
                    {
                        return UnknownAnswer();
                    }
                    return new BuiltAnswer
                    {
                        Answer = topic.Answer,
                        Route = topic.Route,
                        Endpoint = $"knowledge:{topic.Id}",
                        Data = new
                        {
                            kind = "training",
                            topicId = topic.Id,
                            title = topic.Title,
                            route = topic.Route,
                            module = topic.Module
                        },
                        Suggestions = NextTrainingSuggestions(topic.Id)
                    };
                }

            case AiIntentKind.PayrollSummary:
                {
                    var data = await PayrollHrTools.GetPayrollSummaryAsync(_db, scope);
                    if (data.NotFound || data.Period is null)
                    {
                        return new BuiltAnswer { Answer = "No hay periodos de nómina abiertos en este momento." };
                    }
                    var p = data.Period;
                    var answer = p.HasRuns
                        ? $"En el periodo abierto {p.PeriodName} se pagará {FormatCurrency(p.Net)} neto a {p.EmployeeCount} empleado(s) (percepciones {FormatCurrency(p.Gross)}, deducciones {FormatCurrency(p.Deductions)})."
                        : $"El periodo abierto es {p.PeriodName} ({p.StartDate:dd/MM/yyyy} - {p.EndDate:dd/MM/yyyy}). Aún no se ha procesado la nómina; con los ajustes capturados se proyecta un neto de {FormatCurrency(p.Net)}.";
                    return new BuiltAnswer
                    {
                        Answer = answer,
                        Endpoint = "tool:GetPayrollSummary",
                        Route = "/payroll/reporte",
                        Data = new { kind = "payroll_summary", period = data.Period, byDepartment = data.ByDepartment }
                    };
                }

            case AiIntentKind.EmployeeIncidents:
                {
                    var data = await PayrollHrTools.GetEmployeeIncidentsAsync(_db, scope, intent.ConceptHint);
                    if (data.Rows.Count == 0)
                    {
                        return new BuiltAnswer
                        {
                            Answer = string.IsNullOrEmpty(intent.ConceptHint)
                                ? "No encontré incidencias registradas en el periodo abierto."
                                : $"No encontré incidencias del filtro \"{intent.ConceptHint}\" en el periodo abierto.",
                            Endpoint = "tool:GetEmployeeIncidents",
                            Route = "/human-resources/incidents"
                        };
                    }
                    var answer = $"Hay {data.EmployeesWithIncidents} empleado(s) con un total de {data.TotalIncidents} incidencia(s) en el periodo {data.PeriodName}.";
                    return new BuiltAnswer
                    {
                        Answer = answer,
                        Endpoint = "tool:GetEmployeeIncidents",
                        Route = "/human-resources/incidents",
                        Data = new { kind = "employee_incidents", periodName = data.PeriodName, rows = data.Rows }
                    };
                }

            case AiIntentKind.TodayIncidents:
                {
                    var data = await PayrollHrTools.GetTodayIncidentsAsync(_db, scope);
                    var answer = data.Total == 0
                        ? "Hoy no hay incidencias registradas."
                        : $"Hoy hay {data.Total} incidencia(s) registrada(s) entre {data.Rows.Count} empleado(s).";
                    return new BuiltAnswer
                    {
                        Answer = answer,
                        Endpoint = "tool:GetTodayIncidents",
                        Route = "/human-resources/incidents",
                        Data = new { kind = "today_incidents", referenceDate = data.ReferenceDate, rows = data.Rows }
                    };
                }

            case AiIntentKind.EmployeeStatus:
                {
                    var status = intent.ConceptHint ?? "active";
                    var rows = await PayrollHrTools.GetEmployeesByStatusAsync(_db, scope, status);
                    var label = status == "active" ? "activos" : status == "terminated" ? "dados de baja" : status;
                    if (rows.Count == 0)
                    {
                        return new BuiltAnswer
                        {
                            Answer = $"No encontré empleados {label} en este contexto.",
                            Endpoint = "tool:GetEmployeesByStatus",
                            Route = "/human-resources/employees"
                        };
                    }
                    var totalSalary = rows.Sum(x => x.PeriodSalary);
                    return new BuiltAnswer
                    {
                        Answer = $"Hay {rows.Count} empleado(s) {label}. Suma de sueldos del periodo: {FormatCurrency(totalSalary)}.",
                        Endpoint = "tool:GetEmployeesByStatus",
                        Route = "/human-resources/employees",
                        Data = new { kind = "employees_status", status, rows }
                    };
                }

            case AiIntentKind.EmployeeLoans:
                {
                    var rows = await PayrollHrTools.GetEmployeeLoansAsync(_db, scope);
                    if (rows.Count == 0)
                    {
                        return new BuiltAnswer
                        {
                            Answer = "No hay préstamos activos registrados.",
                            Endpoint = "tool:GetEmployeeLoans",
                            Route = "/payroll/loans"
                        };
                    }
                    var totalBalance = rows.Sum(x => x.BalanceAmount);
                    return new BuiltAnswer
                    {
                        Answer = $"Hay {rows.Count} empleado(s) con préstamos activos. Saldo total por recuperar: {FormatCurrency(totalBalance)}.",
                        Endpoint = "tool:GetEmployeeLoans",
                        Route = "/payroll/loans",
                        Data = new { kind = "employee_loans", rows }
                    };
                }

            case AiIntentKind.DepartmentsCost:
                {
                    var rows = await PayrollHrTools.GetDepartmentsCostAsync(_db, scope);
                    if (rows.Count == 0)
                    {
                        return new BuiltAnswer
                        {
                            Answer = "No hay empleados activos para calcular costo por departamento.",
                            Endpoint = "tool:GetDepartmentsCost",
                            Route = "/human-resources/departments"
                        };
                    }
                    var top = rows.First();
                    return new BuiltAnswer
                    {
                        Answer = $"El departamento con mayor costo de plantilla es {top.DepartmentName} con {FormatCurrency(top.TotalSalary)} en {top.EmployeeCount} empleado(s).",
                        Endpoint = "tool:GetDepartmentsCost",
                        Route = "/human-resources/departments",
                        Data = new { kind = "departments_cost", rows }
                    };
                }

            case AiIntentKind.OvertimeEmployees:
                {
                    var data = await PayrollHrTools.GetOvertimeEmployeesAsync(_db, scope);
                    if (data.Rows.Count == 0)
                    {
                        return new BuiltAnswer
                        {
                            Answer = "No encontré empleados con horas extra capturadas en el periodo abierto.",
                            Endpoint = "tool:GetOvertimeEmployees",
                            Route = "/payroll/days-hours"
                        };
                    }
                    return new BuiltAnswer
                    {
                        Answer = $"En el periodo {data.PeriodName} hay {data.EmployeesWithIncidents} empleado(s) con horas extra y {data.TotalIncidents} captura(s) en total.",
                        Endpoint = "tool:GetOvertimeEmployees",
                        Route = "/payroll/days-hours",
                        Data = new { kind = "overtime_employees", periodName = data.PeriodName, rows = data.Rows }
                    };
                }

            case AiIntentKind.AttendanceFaltas:
                {
                    var data = await PayrollHrTools.GetFaltasAsync(_db, scope);
                    if (data.Rows.Count == 0)
                    {
                        return new BuiltAnswer
                        {
                            Answer = "No se han registrado faltas/ausencias en el periodo abierto.",
                            Endpoint = "tool:GetFaltas",
                            Route = "/human-resources/incidents"
                        };
                    }
                    return new BuiltAnswer
                    {
                        Answer = $"En el periodo {data.PeriodName} hay {data.TotalIncidents} falta(s) entre {data.EmployeesWithIncidents} empleado(s).",
                        Endpoint = "tool:GetFaltas",
                        Route = "/human-resources/incidents",
                        Data = new { kind = "faltas", periodName = data.PeriodName, rows = data.Rows }
                    };
                }

            case AiIntentKind.ConceptTotals:
                {
                    var data = await PayrollHrTools.GetConceptTotalsAsync(_db, scope, intent.ConceptHint);
                    if (data.Rows.Count == 0)
                    {
                        return new BuiltAnswer
                        {
                            Answer = string.IsNullOrWhiteSpace(intent.ConceptHint)
                                ? "No encontré conceptos con movimientos en el periodo abierto."
                                : $"No encontré movimientos del concepto \"{intent.ConceptHint}\" en el periodo abierto.",
                            Endpoint = "tool:GetConceptTotals",
                            Route = "/payroll/concepts"
                        };
                    }
                    var top = data.Rows.First();
                    return new BuiltAnswer
                    {
                        Answer = $"En el periodo {data.PeriodName}, el concepto \"{top.ConceptName}\" suma {FormatCurrency(top.TotalAmount)} en {top.EmployeeCount} empleado(s). Total general filtrado: {FormatCurrency(data.Total)}.",
                        Endpoint = "tool:GetConceptTotals",
                        Route = "/payroll/concepts",
                        Data = new { kind = "concept_totals", periodName = data.PeriodName, rows = data.Rows, total = data.Total }
                    };
                }

            case AiIntentKind.EmployeesWithoutDepartment:
                {
                    var rows = await PayrollHrTools.GetEmployeesWithoutDepartmentAsync(_db, scope);
                    if (rows.Count == 0)
                    {
                        return new BuiltAnswer
                        {
                            Answer = "Todos los empleados activos tienen departamento asignado.",
                            Endpoint = "tool:GetEmployeesWithoutDepartment",
                            Route = "/human-resources/employees"
                        };
                    }
                    return new BuiltAnswer
                    {
                        Answer = $"Hay {rows.Count} empleado(s) activo(s) sin departamento. Revísalos en el catálogo de Colaboradores.",
                        Endpoint = "tool:GetEmployeesWithoutDepartment",
                        Route = "/human-resources/employees",
                        Data = new { kind = "missing_data", reason = "department", rows }
                    };
                }

            case AiIntentKind.EmployeesWithoutSalary:
                {
                    var rows = await PayrollHrTools.GetEmployeesWithoutSalaryAsync(_db, scope);
                    if (rows.Count == 0)
                    {
                        return new BuiltAnswer
                        {
                            Answer = "Todos los empleados activos tienen sueldo del periodo capturado.",
                            Endpoint = "tool:GetEmployeesWithoutSalary",
                            Route = "/human-resources/employees"
                        };
                    }
                    return new BuiltAnswer
                    {
                        Answer = $"Hay {rows.Count} empleado(s) activo(s) sin sueldo del periodo. Captúralos antes de procesar la nómina.",
                        Endpoint = "tool:GetEmployeesWithoutSalary",
                        Route = "/human-resources/employees",
                        Data = new { kind = "missing_data", reason = "salary", rows }
                    };
                }

            default:
                return UnknownAnswer();
        }
    }

    private static BuiltAnswer UnknownAnswer() => new()
    {
        Answer = "No encontré información suficiente para responder eso. Puedo ayudarte con consultas operativas (incidencias, nómina, plantilla) o enseñarte cómo usar Nanchesoft (alta de colaborador, captura de conceptos, etc.).",
        Suggestions = DefaultSuggestions
    };

    private static IReadOnlyList<string> NextTrainingSuggestions(string topicId) => topicId switch
    {
        "alta_colaborador" => new[]
        {
            "¿Cómo capturo incidencias?",
            "¿Cómo dar de alta departamentos?",
            "¿Cuántos empleados están activos?"
        },
        "capturar_incidencias" => new[]
        {
            "¿Cómo procesar la nómina?",
            "¿Cómo corregir incidencias?",
            "¿Qué empleados tienen incidencias?"
        },
        "crear_conceptos" => new[]
        {
            "¿Cómo agregar un concepto al recibo?",
            "¿Cómo capturar incidencias?",
            "¿Cómo procesar la nómina?"
        },
        "abrir_periodo" => new[]
        {
            "¿Cómo capturar incidencias?",
            "¿Cómo procesar la nómina?",
            "¿Cuánto se pagará de nómina?"
        },
        _ => DefaultSuggestions
    };

    public static readonly IReadOnlyList<string> DefaultSuggestions = new[]
    {
        "¿Cuánto se pagará de nómina?",
        "¿Qué empleados tienen incidencias?",
        "¿Cómo doy de alta un colaborador?",
        "¿Cómo capturo incidencias?",
        "¿Qué departamentos cuestan más?",
        "¿Qué empleados están activos?",
        "¿Qué empleados tienen préstamos?",
        "¿Cómo procesar la nómina?"
    };

    private static string FormatCurrency(decimal value)
        => value.ToString("C2", CultureInfo.GetCultureInfo("es-MX"));

    private sealed class BuiltAnswer
    {
        public string Answer { get; set; } = string.Empty;
        public string? Endpoint { get; set; }
        public string? Route { get; set; }
        public object? Data { get; set; }
        public IReadOnlyList<string> Suggestions { get; set; } = Array.Empty<string>();
    }
}

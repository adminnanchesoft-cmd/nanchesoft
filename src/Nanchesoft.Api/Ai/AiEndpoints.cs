using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nanchesoft.Api.Ai.Dtos;
using Nanchesoft.Api.Ai.KnowledgeBase;
using Nanchesoft.Api.Ai.Services;
using Nanchesoft.Persistence.Context;

namespace Nanchesoft.Api.Ai;

public static class AiEndpoints
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapAiModuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ia").WithTags("Nanchesoft IA");

        group.MapPost("/chat", ChatAsync);
        group.MapGet("/conversations", ListConversationsAsync);
        group.MapPost("/conversations", CreateConversationAsync);
        group.MapGet("/conversations/{id:guid}/messages", GetMessagesAsync);
        group.MapDelete("/conversations/{id:guid}", DeleteConversationAsync);
        group.MapGet("/suggestions", GetSuggestionsAsync);
        group.MapGet("/training", GetTrainingTopicsAsync);

        // Backward compatible /ask
        group.MapPost("/ask", async (HttpContext http, NanchesoftDbContext db, AiChatRequest request, CancellationToken ct) =>
        {
            var orchestrator = new AiOrchestrator(db);
            var scope = AiScope.FromHttp(http);
            var result = await orchestrator.HandleAsync(scope, request, ct);
            return Results.Ok(result);
        });

        return app;
    }

    private static async Task<IResult> ChatAsync(HttpContext http, NanchesoftDbContext db, AiChatRequest request, CancellationToken ct)
    {
        var orchestrator = new AiOrchestrator(db);
        var scope = AiScope.FromHttp(http);
        var result = await orchestrator.HandleAsync(scope, request, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> ListConversationsAsync(HttpContext http, NanchesoftDbContext db, CancellationToken ct)
    {
        var scope = AiScope.FromHttp(http);
        if (scope.TenantId is null || scope.UserId is null)
        {
            return Results.Ok(Array.Empty<AiConversationDto>());
        }

        var rows = await db.AiConversations.AsNoTracking()
            .Where(x => x.TenantId == scope.TenantId && x.UserId == scope.UserId && x.IsActive)
            .OrderByDescending(x => x.LastActivityAt)
            .Take(40)
            .Select(x => new AiConversationDto
            {
                Id = x.Id,
                Title = x.Title,
                Module = x.Module,
                Status = x.Status,
                LastIntent = x.LastIntent,
                CreatedAt = x.CreatedAt,
                LastActivityAt = x.LastActivityAt,
                MessageCount = x.MessageCount
            })
            .ToListAsync(ct);

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateConversationAsync(HttpContext http, NanchesoftDbContext db, CancellationToken ct)
    {
        var scope = AiScope.FromHttp(http);
        if (scope.TenantId is null || scope.UserId is null)
        {
            return Results.BadRequest(new { message = "Sesión sin tenant/usuario." });
        }

        var conv = new Nanchesoft.Domain.Entities.AiConversation
        {
            TenantId = scope.TenantId.Value,
            CompanyId = scope.CompanyId,
            BranchId = scope.BranchId,
            UserId = scope.UserId.Value,
            Title = "Nueva conversación",
            Module = "hr_payroll",
            Status = "active",
            LastActivityAt = DateTime.UtcNow,
            CreatedBy = scope.UserId?.ToString()
        };
        db.AiConversations.Add(conv);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new AiConversationDto
        {
            Id = conv.Id,
            Title = conv.Title,
            Module = conv.Module,
            Status = conv.Status,
            CreatedAt = conv.CreatedAt,
            LastActivityAt = conv.LastActivityAt,
            MessageCount = 0
        });
    }

    private static async Task<IResult> GetMessagesAsync(HttpContext http, NanchesoftDbContext db, Guid id, CancellationToken ct)
    {
        var scope = AiScope.FromHttp(http);
        if (scope.TenantId is null || scope.UserId is null)
        {
            return Results.Ok(Array.Empty<AiMessageDto>());
        }

        var conversation = await db.AiConversations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == scope.TenantId && x.UserId == scope.UserId, ct);
        if (conversation is null) return Results.NotFound();

        var rows = await db.AiMessages.AsNoTracking()
            .Where(x => x.ConversationId == id && x.TenantId == scope.TenantId)
            .OrderBy(x => x.SequenceNumber)
            .Take(200)
            .Select(x => new AiMessageDto
            {
                Id = x.Id,
                ConversationId = x.ConversationId,
                Role = x.Role,
                Content = x.Content,
                Intent = x.Intent,
                Endpoint = x.Endpoint,
                SequenceNumber = x.SequenceNumber,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(ct);

        // Re-attach Data from DataJson per row
        var jsonMap = await db.AiMessages.AsNoTracking()
            .Where(x => x.ConversationId == id && x.TenantId == scope.TenantId && x.DataJson != null)
            .Select(x => new { x.Id, x.DataJson })
            .ToListAsync(ct);

        foreach (var r in rows)
        {
            var hit = jsonMap.FirstOrDefault(j => j.Id == r.Id);
            if (hit?.DataJson is null) continue;
            try
            {
                r.Data = JsonSerializer.Deserialize<JsonElement>(hit.DataJson, JsonOpts);
            }
            catch { /* ignore corrupted json */ }
        }

        return Results.Ok(rows);
    }

    private static async Task<IResult> DeleteConversationAsync(HttpContext http, NanchesoftDbContext db, Guid id, CancellationToken ct)
    {
        var scope = AiScope.FromHttp(http);
        if (scope.TenantId is null || scope.UserId is null)
        {
            return Results.BadRequest(new { message = "Sesión sin tenant/usuario." });
        }
        var conv = await db.AiConversations.FirstOrDefaultAsync(
            x => x.Id == id && x.TenantId == scope.TenantId && x.UserId == scope.UserId, ct);
        if (conv is null) return Results.NotFound();

        conv.IsActive = false;
        conv.Status = "archived";
        conv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static IResult GetSuggestionsAsync()
    {
        var groups = new List<AiSuggestionGroupDto>
        {
            new()
            {
                Title = "Consultas operativas",
                Items = new List<string>
                {
                    "¿Cuánto se pagará de nómina?",
                    "¿Qué empleados tienen incidencias?",
                    "¿Cuántas faltas hubo en el periodo?",
                    "¿Qué empleados tienen horas extra?",
                    "¿Qué empleados están activos?",
                    "¿Qué empleados tienen préstamos?",
                    "¿Qué departamentos cuestan más?",
                    "¿Qué empleados no tienen departamento?",
                    "¿Qué empleados no tienen sueldo del periodo?",
                    "¿Qué incidencias hay hoy?"
                }
            },
            new()
            {
                Title = "Capacitación del sistema",
                Items = new List<string>
                {
                    "¿Cómo doy de alta un colaborador?",
                    "¿Cómo capturo incidencias?",
                    "¿Cómo crear conceptos de nómina?",
                    "¿Cómo abrir un periodo?",
                    "¿Cómo procesar la nómina?",
                    "¿Cómo revisar una nómina?",
                    "¿Cómo capturar préstamos?",
                    "¿Cómo dispersar la nómina al banco?"
                }
            }
        };
        return Results.Ok(groups);
    }

    private static IResult GetTrainingTopicsAsync()
    {
        var topics = TrainingKnowledgeBase.Topics
            .Select(t => new { id = t.Id, title = t.Title, module = t.Module, route = t.Route, keywords = t.Keywords })
            .ToList();
        return Results.Ok(topics);
    }
}

using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class CrmActivity : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string Type { get; set; } = "visit";
    public string Status { get; set; } = "planned";

    public string RelatedEntityType { get; set; } = string.Empty;
    public Guid? RelatedEntityId { get; set; }

    public Guid? OwnerUserId { get; set; }

    public DateTime ScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? DurationMin { get; set; }

    public decimal? GeoLat { get; set; }
    public decimal? GeoLng { get; set; }
    public string? CheckInMethod { get; set; }

    public string? OutcomeTag { get; set; }
    public string? NoteRich { get; set; }
    public string? VoiceNoteUrl { get; set; }
    public string? PhotoUrlsCsv { get; set; }
}

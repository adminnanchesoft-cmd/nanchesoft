using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class City : BaseEntity
{
    public Guid StateId { get; set; }
    public State? State { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

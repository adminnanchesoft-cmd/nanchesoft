using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class Country : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Iso2 { get; set; } = string.Empty;

    public ICollection<State> States { get; set; } = new List<State>();
}

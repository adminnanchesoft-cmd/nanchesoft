namespace Nanchesoft.Domain.Entities;

public class AccountingImportDetail
{
    public Guid Id { get; set; }
    public Guid ImportId { get; set; }
    public int ExcelRow { get; set; }
    public string? AccountCode { get; set; }
    public string? AccountName { get; set; }
    public string? Company { get; set; }
    public bool? Applies { get; set; }
    public string Status { get; set; } = "pending";
    public string? ErrorMessage { get; set; }
}

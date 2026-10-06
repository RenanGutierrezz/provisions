using RWMS.Models.Enums;

namespace RWMS.Models.ViewModels.AccountRequest;

public class AccountRequestListViewModel
{
    public List<AccountRequestItemViewModel> Requests { get; set; } = [];
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
}

public class AccountRequestItemViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Message { get; set; }
    public AccountRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedByName { get; set; }
}

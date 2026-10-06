using RWMS.Models.Enums;

namespace RWMS.Models.Domain;

public class AccountRequest
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Message { get; set; }
    public AccountRequestStatus Status { get; set; } = AccountRequestStatus.Pending;

    public string? ReviewedById { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}

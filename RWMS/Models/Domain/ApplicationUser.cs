using Microsoft.AspNetCore.Identity;

namespace RWMS.Models.Domain;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
    public bool EmailNotificationsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string FullName => $"{FirstName} {LastName}".Trim();
    public string DisplayName => !string.IsNullOrWhiteSpace(CompanyName) ? CompanyName : FullName;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<CustomerProduct> CustomerProducts { get; set; } = new List<CustomerProduct>();
}

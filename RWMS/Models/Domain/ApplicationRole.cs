using Microsoft.AspNetCore.Identity;

namespace RWMS.Models.Domain;

public class ApplicationRole : IdentityRole
{
    public string Description { get; set; } = string.Empty;
}

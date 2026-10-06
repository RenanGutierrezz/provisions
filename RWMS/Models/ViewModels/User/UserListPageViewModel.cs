namespace RWMS.Models.ViewModels.User;

public class UserListPageViewModel
{
    public List<UserListViewModel> Users { get; set; } = [];
    public string? Search { get; set; }
    public string? Role { get; set; }
    public int TotalCount { get; set; }
    public int ClientCount { get; set; }
    public int ManagerCount { get; set; }
    public int OwnerCount { get; set; }
}

public class UserListViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

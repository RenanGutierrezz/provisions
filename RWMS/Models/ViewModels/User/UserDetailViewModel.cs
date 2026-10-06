using RWMS.Models.Enums;
using RWMS.Services.Interfaces;

namespace RWMS.Models.ViewModels.User;

public class UserDetailViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<OrderGuideItem> OrderGuide { get; set; } = [];
    public List<UserOrderSummary> RecentOrders { get; set; } = [];
    public int TotalOrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}

public class UserOrderSummary
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
}

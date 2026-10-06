using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RWMS.Data;
using RWMS.Models.Domain;
using RWMS.Models.Enums;
using RWMS.Models.ViewModels.User;
using RWMS.Services.Interfaces;

namespace RWMS.Services.Implementations;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IOrderGuideService _orderGuide;

    public UserService(UserManager<ApplicationUser> userManager, ApplicationDbContext db, IOrderGuideService orderGuide)
    {
        _userManager = userManager;
        _db = db;
        _orderGuide = orderGuide;
    }

    public async Task<UserListPageViewModel> GetAllUsersAsync(string? search, string? role, CancellationToken ct = default)
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync(ct);

        var usersWithRoles = new List<UserListViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            usersWithRoles.Add(new UserListViewModel
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email ?? "",
                Role = roles.FirstOrDefault() ?? "No Role",
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
        }

        var totalCount = usersWithRoles.Count;
        var clientCount = usersWithRoles.Count(u => u.Role == "Customer");
        var managerCount = usersWithRoles.Count(u => u.Role == "Manager");
        var ownerCount = usersWithRoles.Count(u => u.Role == "Owner");

        if (!string.IsNullOrWhiteSpace(search))
        {
            usersWithRoles = usersWithRoles
                .Where(u => u.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                         || u.Email.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            usersWithRoles = usersWithRoles
                .Where(u => u.Role.Equals(role, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return new UserListPageViewModel
        {
            Users = usersWithRoles,
            Search = search,
            Role = role,
            TotalCount = totalCount,
            ClientCount = clientCount,
            ManagerCount = managerCount,
            OwnerCount = ownerCount
        };
    }

    public async Task<UserDetailViewModel> GetUserDetailAsync(string id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "No Role";

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == id && o.IsActive)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new UserOrderSummary
            {
                Id = o.Id,
                Status = o.Status,
                RequestedDeliveryDate = o.RequestedDeliveryDate,
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt,
                ItemCount = o.OrderItems.Count
            })
            .ToListAsync(ct);

        var guide = role == "Customer"
            ? await _orderGuide.GetOrderGuideAsync(id, ct)
            : [];

        return new UserDetailViewModel
        {
            Id = user.Id,
            Name = user.DisplayName,
            Email = user.Email ?? "",
            Phone = user.PhoneNumber,
            CompanyName = user.CompanyName,
            Address = user.Address,
            Notes = user.Notes,
            Role = role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            OrderGuide = guide,
            RecentOrders = orders.Take(10).ToList(),
            TotalOrderCount = orders.Count,
            TotalSpent = orders.Where(o => o.Status != OrderStatus.Rejected).Sum(o => o.TotalAmount)
        };
    }
}

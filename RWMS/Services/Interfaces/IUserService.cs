using RWMS.Models.ViewModels.User;

namespace RWMS.Services.Interfaces;

public interface IUserService
{
    Task<UserListPageViewModel> GetAllUsersAsync(string? search, string? role, CancellationToken ct = default);
    Task<UserDetailViewModel> GetUserDetailAsync(string id, CancellationToken ct = default);
}

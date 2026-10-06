using RWMS.Models.Domain;

namespace RWMS.Services.Interfaces;

public interface IInviteService
{
    Task<string> CreateInviteAsync(string email, string role, CancellationToken ct = default);
    Task<Invite> ValidateAsync(string token, CancellationToken ct = default);
    Task RedeemAsync(string token, CancellationToken ct = default);
}

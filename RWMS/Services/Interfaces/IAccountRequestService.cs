using RWMS.Models.ViewModels.AccountRequest;

namespace RWMS.Services.Interfaces;

public interface IAccountRequestService
{
    Task SubmitAsync(SubmitRequestViewModel model, CancellationToken ct = default);
    Task<AccountRequestListViewModel> GetAllAsync(CancellationToken ct = default);
    Task<string> ApproveAsync(int id, string reviewerId, CancellationToken ct = default);
    Task RejectAsync(int id, string reviewerId, CancellationToken ct = default);
}

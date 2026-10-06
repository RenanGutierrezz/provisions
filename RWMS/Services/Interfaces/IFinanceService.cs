using RWMS.Models.ViewModels.Finance;

namespace RWMS.Services.Interfaces;

public interface IFinanceService
{
    Task<FinanceReportViewModel> GetReportAsync(DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<OwnerDashboardViewModel> GetDashboardAsync(CancellationToken ct = default);
}

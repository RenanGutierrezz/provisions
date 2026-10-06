using Microsoft.EntityFrameworkCore;
using RWMS.Data;
using RWMS.Models.Domain;
using RWMS.Models.Enums;
using RWMS.Models.ViewModels.AccountRequest;
using RWMS.Services.Interfaces;

namespace RWMS.Services.Implementations;

public class AccountRequestService : IAccountRequestService
{
    private readonly ApplicationDbContext _db;

    public AccountRequestService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task SubmitAsync(SubmitRequestViewModel model, CancellationToken ct = default)
    {
        var existing = await _db.AccountRequests
            .AnyAsync(r => r.Email == model.Email && r.Status == AccountRequestStatus.Pending, ct);

        if (existing)
            throw new InvalidOperationException("A request with this email is already pending review.");

        _db.AccountRequests.Add(new AccountRequest
        {
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            CompanyName = model.CompanyName?.Trim(),
            Email = model.Email.Trim().ToLowerInvariant(),
            Phone = model.Phone?.Trim(),
            Message = model.Message?.Trim()
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<AccountRequestListViewModel> GetAllAsync(CancellationToken ct = default)
    {
        var requests = await _db.AccountRequests
            .AsNoTracking()
            .Include(r => r.ReviewedBy)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new AccountRequestItemViewModel
            {
                Id = r.Id,
                FullName = r.FirstName + " " + r.LastName,
                CompanyName = r.CompanyName,
                Email = r.Email,
                Phone = r.Phone,
                Message = r.Message,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                ReviewedAt = r.ReviewedAt,
                ReviewedByName = r.ReviewedBy != null ? r.ReviewedBy.DisplayName : null
            })
            .ToListAsync(ct);

        return new AccountRequestListViewModel
        {
            Requests = requests,
            PendingCount = requests.Count(r => r.Status == AccountRequestStatus.Pending),
            ApprovedCount = requests.Count(r => r.Status == AccountRequestStatus.Approved),
            RejectedCount = requests.Count(r => r.Status == AccountRequestStatus.Rejected)
        };
    }

    public async Task<string> ApproveAsync(int id, string reviewerId, CancellationToken ct = default)
    {
        var request = await _db.AccountRequests.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Account request {id} not found.");

        if (request.Status != AccountRequestStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be approved.");

        request.Status = AccountRequestStatus.Approved;
        request.ReviewedById = reviewerId;
        request.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return request.Email;
    }

    public async Task RejectAsync(int id, string reviewerId, CancellationToken ct = default)
    {
        var request = await _db.AccountRequests.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Account request {id} not found.");

        if (request.Status != AccountRequestStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be rejected.");

        request.Status = AccountRequestStatus.Rejected;
        request.ReviewedById = reviewerId;
        request.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RWMS.Data;
using RWMS.Models.Domain;
using RWMS.Services.Interfaces;

namespace RWMS.Services.Implementations;

public class InviteService : IInviteService
{
    private readonly ApplicationDbContext _db;

    public InviteService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string> CreateInviteAsync(string email, string role, CancellationToken ct = default)
    {
        // Invalidate any existing unused invite for this email
        var existing = await _db.Invites
            .Where(i => i.Email == email && !i.IsUsed)
            .ToListAsync(ct);

        _db.Invites.RemoveRange(existing);

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        _db.Invites.Add(new Invite
        {
            TokenHash = hash,
            Email = email,
            Role = role,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        await _db.SaveChangesAsync(ct);

        return token;
    }

    public async Task<Invite> ValidateAsync(string token, CancellationToken ct = default)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        var invite = await _db.Invites
            .FirstOrDefaultAsync(i => i.TokenHash == hash, ct);

        if (invite is null || invite.IsUsed || invite.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired invitation.");

        return invite;
    }

    public async Task RedeemAsync(string token, CancellationToken ct = default)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        var invite = await _db.Invites
            .FirstOrDefaultAsync(i => i.TokenHash == hash && !i.IsUsed, ct);

        if (invite is null || invite.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired invitation.");

        invite.IsUsed = true;
        await _db.SaveChangesAsync(ct);
    }
}

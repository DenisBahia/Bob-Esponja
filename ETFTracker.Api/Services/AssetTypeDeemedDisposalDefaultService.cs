using Microsoft.EntityFrameworkCore;
using ETFTracker.Api.Data;
using ETFTracker.Api.Dtos;
using ETFTracker.Api.Models;

namespace ETFTracker.Api.Services;

public interface IAssetTypeDeemedDisposalDefaultService
{
    Task<List<AssetTypeDeemedDisposalDefaultDto>> GetDefaultsAsync(int userId, CancellationToken ct = default);
    Task<AssetTypeDeemedDisposalDefaultDto> UpsertAsync(int userId, AssetTypeDeemedDisposalDefaultDto dto, CancellationToken ct = default);
}

public class AssetTypeDeemedDisposalDefaultService : IAssetTypeDeemedDisposalDefaultService
{
    private readonly AppDbContext _db;

    public AssetTypeDeemedDisposalDefaultService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<AssetTypeDeemedDisposalDefaultDto>> GetDefaultsAsync(int userId, CancellationToken ct = default)
    {
        var rows = await _db.AssetTypeDeemedDisposalDefaults
            .Where(a => a.UserId == userId)
            .ToListAsync(ct);

        return rows.Select(a => new AssetTypeDeemedDisposalDefaultDto
        {
            AssetType = a.AssetType,
            DeemedDisposalDue = a.DeemedDisposalDue
        }).ToList();
    }

    public async Task<AssetTypeDeemedDisposalDefaultDto> UpsertAsync(int userId, AssetTypeDeemedDisposalDefaultDto dto, CancellationToken ct = default)
    {
        var existing = await _db.AssetTypeDeemedDisposalDefaults
            .FirstOrDefaultAsync(a => a.UserId == userId && a.AssetType == dto.AssetType, ct);

        if (existing == null)
        {
            existing = new AssetTypeDeemedDisposalDefault
            {
                UserId = userId,
                AssetType = dto.AssetType,
                DeemedDisposalDue = dto.DeemedDisposalDue,
                UpdatedAt = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc)
            };
            _db.AssetTypeDeemedDisposalDefaults.Add(existing);
        }
        else
        {
            existing.DeemedDisposalDue = dto.DeemedDisposalDue;
            existing.UpdatedAt = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);
        }

        await _db.SaveChangesAsync(ct);

        return new AssetTypeDeemedDisposalDefaultDto
        {
            AssetType = existing.AssetType,
            DeemedDisposalDue = existing.DeemedDisposalDue
        };
    }
}


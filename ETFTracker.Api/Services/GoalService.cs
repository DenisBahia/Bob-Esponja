using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ETFTracker.Api.Data;
using ETFTracker.Api.Dtos;
using ETFTracker.Api.Models;

namespace ETFTracker.Api.Services;

public interface IGoalService
{
    Task<UserGoalDto?> GetGoalAsync(int userId, CancellationToken ct = default);
    Task<UserGoalDto> UpsertGoalAsync(int userId, UpsertGoalRequestDto dto, CancellationToken ct = default);
}

public class GoalService : IGoalService
{
    private readonly AppDbContext _context;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public GoalService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserGoalDto?> GetGoalAsync(int userId, CancellationToken ct = default)
    {
        var goal = await _context.UserGoals
            .FirstOrDefaultAsync(g => g.UserId == userId, ct);

        return goal == null ? null : MapToDto(goal);
    }

    public async Task<UserGoalDto> UpsertGoalAsync(int userId, UpsertGoalRequestDto dto, CancellationToken ct = default)
    {
        var goal = await _context.UserGoals
            .FirstOrDefaultAsync(g => g.UserId == userId, ct);

        var json = JsonSerializer.Serialize(dto.DataPoints);
        var now  = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);

        if (goal == null)
        {
            goal = new UserGoal
            {
                UserId          = userId,
                SourceVersionId = dto.SourceVersionId,
                SavedAt         = now,
                GoalPointsJson  = json,
            };
            _context.UserGoals.Add(goal);
        }
        else
        {
            goal.SourceVersionId = dto.SourceVersionId;
            goal.SavedAt         = now;
            goal.GoalPointsJson  = json;
        }

        await _context.SaveChangesAsync(ct);
        return MapToDto(goal);
    }

    private static UserGoalDto MapToDto(UserGoal goal)
    {
        List<GoalDataPointDto> points;
        try
        {
            points = JsonSerializer.Deserialize<List<GoalDataPointDto>>(goal.GoalPointsJson, JsonOpts) ?? new();
        }
        catch
        {
            points = new();
        }

        return new UserGoalDto
        {
            Id              = goal.Id,
            SourceVersionId = goal.SourceVersionId,
            SavedAt         = goal.SavedAt,
            DataPoints      = points,
        };
    }
}


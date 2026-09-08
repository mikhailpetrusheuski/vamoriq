namespace Vamoriq.Services.Interfaces;

public interface IStreakService
{
    Task<int> GetCurrentStreakAsync(string todayLocalDate, CancellationToken ct = default);
    Task<int> GetLongestStreakAsync(CancellationToken ct = default);
    Task<int> GetTotalCompletedAsync(CancellationToken ct = default);
}

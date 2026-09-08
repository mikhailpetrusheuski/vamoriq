namespace Vamoriq.Services.Interfaces;

public interface IPhotoStorageService
{
    Task<string?> SaveProofPhotoAsync(string missionId, string tempFilePath, CancellationToken ct = default);
    Task CleanupOldPhotosAsync(int maxAgeDays = 60, CancellationToken ct = default);
}

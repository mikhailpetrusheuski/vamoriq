using Vamoriq.Services.Interfaces;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Vamoriq.Services.Services;

public class PhotoStorageService : IPhotoStorageService
{
    private const int MaxLongestSide = 1024;
    private const int JpegQuality = 80;

    private readonly ILogger<PhotoStorageService> _logger;
    private readonly string _appDataDirectory;

    public PhotoStorageService(ILogger<PhotoStorageService> logger, string appDataDirectory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appDataDirectory = appDataDirectory ?? throw new ArgumentNullException(nameof(appDataDirectory));
    }

    public async Task<string?> SaveProofPhotoAsync(string missionId, string tempFilePath, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(missionId) || string.IsNullOrWhiteSpace(tempFilePath))
                return null;

            if (!File.Exists(tempFilePath))
            {
                _logger.LogWarning("Temp file does not exist: {Path}", tempFilePath);
                return null;
            }

            var proofsDir = Path.Combine(_appDataDirectory, "proofs");
            Directory.CreateDirectory(proofsDir);

            var targetPath = Path.Combine(proofsDir, $"{missionId}.jpg");

            await Task.Run(() => File.Copy(tempFilePath, targetPath, overwrite: true), ct).ConfigureAwait(false);

            await TryCompressAsync(targetPath, ct).ConfigureAwait(false);

            try
            {
                File.Delete(tempFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temp file: {Path}", tempFilePath);
            }

            _logger.LogDebug("Proof photo saved: {Path}", targetPath);
            return targetPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save proof photo for mission {MissionId}", missionId);
            return null;
        }
    }

    private async Task TryCompressAsync(string filePath, CancellationToken ct)
    {
        try
        {
            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                using var original = SKBitmap.Decode(filePath);
                if (original is null) return;

                var longestSide = Math.Max(original.Width, original.Height);
                if (longestSide <= MaxLongestSide) return;

                var scale = (float)MaxLongestSide / longestSide;
                var newWidth = (int)(original.Width * scale);
                var newHeight = (int)(original.Height * scale);

                using var resized = original.Resize(new SKImageInfo(newWidth, newHeight), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
                if (resized is null) return;

                using var image = SKImage.FromBitmap(resized);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
                using var stream = File.OpenWrite(filePath);
                stream.SetLength(0);
                data.SaveTo(stream);

                _logger.LogDebug("Photo compressed: {W}x{H} -> {NW}x{NH}", original.Width, original.Height, newWidth, newHeight);
            }, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Photo compression failed, keeping original copy");
        }
    }

    public async Task CleanupOldPhotosAsync(int maxAgeDays = 60, CancellationToken ct = default)
    {
        try
        {
            var proofsDir = Path.Combine(_appDataDirectory, "proofs");
            if (!Directory.Exists(proofsDir)) return;

            var cutoff = DateTime.Now.AddDays(-maxAgeDays);
            var files = Directory.GetFiles(proofsDir);

            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var info = new FileInfo(file);
                    if (info.CreationTime < cutoff)
                    {
                        info.Delete();
                        _logger.LogDebug("Deleted old proof photo: {Path}", file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old proof file: {Path}", file);
                }
            }

            await Task.CompletedTask;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old photos");
        }
    }
}

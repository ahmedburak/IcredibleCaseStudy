using FileManager.Services.Interfaces;
using System.Security.Cryptography;

namespace FileManager.Services;

/// <summary>
/// Checksum service implementation using SHA256
/// </summary>
public class ChecksumService(Serilog.ILogger logger) : IChecksumService
{
    public async Task<string> CalculateFileChecksumAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            using var sha256 = SHA256.Create();
            using var fileStream = File.OpenRead(filePath);

            var hashBytes = await Task.Run(() => sha256.ComputeHash(fileStream));
            var checksum = Convert.ToHexString(hashBytes).ToLowerInvariant();

            logger.Debug("Calculated checksum for file {FilePath}: {Checksum}", filePath, checksum);

            return checksum;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to calculate checksum for file {FilePath}", filePath);
            throw;
        }
    }

    public string CalculateChecksum(byte[] data)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(data);

            var hashBytes = SHA256.HashData(data);
            var checksum = Convert.ToHexString(hashBytes).ToLowerInvariant();

            logger.Debug("Calculated checksum for data array (Size: {Size} bytes): {Checksum}",
                data.Length, checksum);

            return checksum;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to calculate checksum for data array");
            throw;
        }
    }

    public async Task<bool> VerifyFileChecksumAsync(string filePath, string expectedChecksum)
    {
        try
        {
            var actualChecksum = await CalculateFileChecksumAsync(filePath);
            var isValid = string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);

            logger.Debug("Checksum verification for file {FilePath}: Expected={Expected}, Actual={Actual}, Valid={Valid}",
                filePath, expectedChecksum, actualChecksum, isValid);

            return isValid;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to verify checksum for file {FilePath}", filePath);
            return false;
        }
    }

    public bool VerifyChecksum(byte[] data, string expectedChecksum)
    {
        try
        {
            var actualChecksum = CalculateChecksum(data);
            var isValid = string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);

            logger.Debug("Checksum verification for data array: Expected={Expected}, Actual={Actual}, Valid={Valid}",
                expectedChecksum, actualChecksum, isValid);

            return isValid;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to verify checksum for data array");
            return false;
        }
    }
}
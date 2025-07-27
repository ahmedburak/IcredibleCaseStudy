namespace FileManager.Services.Interfaces;

/// <summary>
/// Checksum service interface for file integrity verification
/// </summary>
public interface IChecksumService
{
    /// <summary>
    /// Calculate SHA256 checksum for file
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>SHA256 checksum as hex string</returns>
    Task<string> CalculateFileChecksumAsync(string filePath);

    /// <summary>
    /// Calculate SHA256 checksum for byte array
    /// </summary>
    /// <param name="data">Data to calculate checksum for</param>
    /// <returns>SHA256 checksum as hex string</returns>
    string CalculateChecksum(byte[] data);

    /// <summary>
    /// Verify file checksum
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="expectedChecksum">Expected checksum</param>
    /// <returns>True if checksum matches</returns>
    Task<bool> VerifyFileChecksumAsync(string filePath, string expectedChecksum);

    /// <summary>
    /// Verify data checksum
    /// </summary>
    /// <param name="data">Data to verify</param>
    /// <param name="expectedChecksum">Expected checksum</param>
    /// <returns>True if checksum matches</returns>
    bool VerifyChecksum(byte[] data, string expectedChecksum);
}
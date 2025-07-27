using FileManager.Services.Interfaces;

namespace FileManager.Services;

public class ManagerService(Serilog.ILogger _logger, StorageSettings storageSettings) : IManagerService
{
    public async Task UploadFileAsync(IFileService fileService)
    {
        string defaultImage = "mangal.jpg";
        Console.Write("Enter file path (default: mangal.jpg): ");
        var filePath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            filePath = defaultImage;
        }

        if (!File.Exists(filePath))
        {
            Console.WriteLine("File not found or invalid path.");
            return;
        }

        var fileName = Path.GetFileName(filePath);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Uploading file: {fileName}...");
        Console.ResetColor();

        var fileMetadata = await fileService.UploadFileAsync(filePath, fileName);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"File uploaded successfully!");
        Console.WriteLine($"File ID: {fileMetadata.Id}");
        Console.WriteLine($"File Name: {fileMetadata.FileName}");
        Console.WriteLine($"File Size: {fileMetadata.FileSize:N0} bytes");
        Console.WriteLine($"Total Chunks: {fileMetadata.TotalChunks}");
        Console.WriteLine($"Chunk Size: {fileMetadata.ChunkSize:N0} bytes");
        Console.WriteLine($"Checksum: {fileMetadata.Checksum}");
        Console.ResetColor();
    }

    public async Task DownloadFileAsync(IFileService fileService)
    {
        Console.Write("Enter file ID: ");
        var fileIdInput = Console.ReadLine();

        if (!Guid.TryParse(fileIdInput, out var fileId))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid file ID format.");
            Console.ResetColor();
            return;
        }

        var fileMetadata = await fileService.GetFileMetadataAsync(fileId);
        if (fileMetadata == null || fileMetadata.Status != Database.Enums.FileStatus.Completed)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("File not found.");
            Console.ResetColor();
            return;
        }

        Console.Write($"Enter output path for '{fileMetadata.FileName}' (default: {storageSettings.FileSystemDownloadRoot}): ");
        var outputPath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.Combine(storageSettings.FileSystemDownloadRoot, fileMetadata.FileName);
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Downloading file: {fileMetadata.FileName}...");
        Console.ResetColor();

        var success = await fileService.DownloadFileAsync(fileId, outputPath);

        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"File downloaded successfully to: {outputPath}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failed to download file.");
            Console.ResetColor();
        }
    }

    public async Task ListFilesAsync(IFileService fileService)
    {
        Console.WriteLine("Listing all files...");

        var files = await fileService.ListFilesAsync();

        if (files.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No files found.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"{"ID",-36} {"Name",-30} {"Size",-12} {"Chunks",-8} {"Status",-10} {"Uploaded",-20}");
        Console.WriteLine(new string('-', 120));

        foreach (var file in files)
        {
            Console.WriteLine($"{file.Id,-36} {file.FileName,-30} {file.FileSize,-12:N0} {file.TotalChunks,-8} {file.Status,-10} {file.UploadedAt,-20:yyyy-MM-dd HH:mm:ss}");
        }
    }

    public async Task DeleteFileAsync(IFileService fileService)
    {
        Console.Write("Enter file ID: ");
        var fileIdInput = Console.ReadLine();

        if (!Guid.TryParse(fileIdInput, out var fileId))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid file ID format.");
            Console.ResetColor();
            return;
        }

        var fileMetadata = await fileService.GetFileMetadataAsync(fileId);
        if (fileMetadata == null || fileMetadata.Status != Database.Enums.FileStatus.Completed)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("File not found.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"Are you sure you want to delete '{fileMetadata.FileName}'? (y/N): ");
        Console.ResetColor();
        var confirmation = Console.ReadLine();

        if (!string.Equals(confirmation, "y", StringComparison.OrdinalIgnoreCase))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Delete operation cancelled.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"Deleting file: {fileMetadata.FileName}...");
        Console.ResetColor();

        var success = await fileService.DeleteFileAsync(fileId);

        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("File deleted successfully.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failed to delete file.");
            Console.ResetColor();
        }
    }

    public async Task VerifyFileIntegrityAsync(IFileService fileService)
    {
        Console.Write("Enter file ID: ");
        var fileIdInput = Console.ReadLine();

        if (!Guid.TryParse(fileIdInput, out var fileId))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid file ID format.");
            Console.ResetColor();
            return;
        }

        var fileMetadata = await fileService.GetFileMetadataAsync(fileId);
        if (fileMetadata == null || fileMetadata.Status != Database.Enums.FileStatus.Completed)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("File not found.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"Verifying integrity of file: {fileMetadata.FileName}...");

        var isValid = await fileService.VerifyFileIntegrityAsync(fileId);

        if (isValid)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("File integrity verification passed. All chunks are valid.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("File integrity verification failed. Some chunks may be corrupted or missing.");
            Console.ResetColor();
        }
    }
}
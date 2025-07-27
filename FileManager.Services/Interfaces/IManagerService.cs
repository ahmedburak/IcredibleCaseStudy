namespace FileManager.Services.Interfaces
{
    public interface IManagerService
    {
        Task DeleteFileAsync(IFileService fileService);
        Task DownloadFileAsync(IFileService fileService);
        Task ListFilesAsync(IFileService fileService);
        Task UploadFileAsync(IFileService fileService);
        Task VerifyFileIntegrityAsync(IFileService fileService);
    }
}
using AsYouLikeIt.FileProviders;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AsYouLikeIt.FileProviders.Services
{
    public interface IFileService
    {
        string ImplementationIdentifier { get; }

        //Task<bool> DirectoryExistsAsync(string absoluteDirectoryPath);

        Task<List<string>> ListSubDirectoriesAsync(string absoluteDirectoryPath);

        Task<List<string>> ListFilesAsync(string absoluteDirectoryPath);

        Task<List<IFileMetadata>> ListFilesWithMetadataAsync(string absoluteDirectoryPath);

        Task<IFileMetadata> GetFileMetadataAsync(string absoluteFilePath);

        Task DeleteDirectoryAndContentsAsync(string absoluteDirectoryPath);

        Task<bool> ExistsAsync(string absoluteFilePath);

        Task<byte[]> ReadAllBytesAsync(string absoluteFilePath);

        Task<Stream> GetStreamAsync(string absoluteFilePath);

        Task WriteAllBytesAsync(string absoluteFilePath, IEnumerable<byte> data);

        /// <summary>
        /// Uses a stream and buffer to stream the file in chunks.
        /// </summary>
        /// <param name="absoluteFilePath"></param>
        /// <param name="stream"></param>
        /// <param name="bufferSize">Defaults to 4 MB</param>
        /// <returns></returns>
        Task WriteStreamAsync(string absoluteFilePath, Stream stream, int bufferSize = 4 * 1024 * 1024);

        Task WriteAllTextAsync(string absoluteFilePath, string content);

        Task DeleteAsync(string absoluteFilePath);

        Task<string> ReadAllTextAsync(string absoluteFilePath);

        //Task<string[]> GetAllFilePathsOfType(string absoluteDirectoryPath, string fileType);
    }
}

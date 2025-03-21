﻿using System.Collections.Generic;
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

        Task DeleteDirectoryAndContentsAsync(string absoluteDirectoryPath);

        Task<bool> ExistsAsync(string absoluteFilePath);

        Task<byte[]> ReadAllBytesAsync(string absoluteFilePath);

        Task<Stream> GetStreamAsync(string absoluteFilePath);

        Task WriteAllBytesAsync(string absoluteFilePath, IEnumerable<byte> data);

        Task WriteAllTextAsync(string absoluteFilePath, string content);

        Task DeleteAsync(string absoluteFilePath);

        Task<string> ReadAllTextAsync(string absoluteFilePath);

        //Task<string[]> GetAllFilePathsOfType(string absoluteDirectoryPath, string fileType);
    }
}

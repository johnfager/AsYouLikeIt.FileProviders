using AsYouLikeit.FileProviders;
using AsYouLikeIt.Sdk.Common.Exceptions;
using AsYouLikeIt.Sdk.Common.Extensions;
using AsYouLikeIt.Sdk.Common.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsYouLikeIt.FileProviders.Services
{
    public class FileSystemService : IFileService
    {

        public const string IDENTIFIER = nameof(FileSystemService);

        private readonly ILogger<FileSystemService> _logger;
        private readonly EnvironmentContext _environmentContext;

        public string ImplementationIdentifier => IDENTIFIER;

        public FileSystemService(EnvironmentContext environmentContext, ILogger<FileSystemService> logger)
        {
            _logger = logger;
            _environmentContext = environmentContext;
        }

        public Task<List<string>> ListSubDirectoriesAsync(string absoluteDirectoryPath)
        {
            var fileSystemPath = GetFilePath(absoluteDirectoryPath);
            var dir = new DirectoryInfo(fileSystemPath);

            var directories = new List<string>();

            if (dir.Exists)
            {
                var dirs = dir.GetDirectories();
                foreach (var d in dirs)
                {
                    directories.Add(d.Name);
                }
            }

            return Task.FromResult(directories);
        }


        public Task<List<string>> ListFilesAsync(string absoluteDirectoryPath)
        {
            var fileSystemPath = GetFilePath(absoluteDirectoryPath);
            var dir = new DirectoryInfo(fileSystemPath);

            var files = new List<string>();

            if (dir.Exists)
            {
                var fileInfos = dir.GetFiles();
                foreach (var f in fileInfos)
                {
                    files.Add(f.Name);
                }
            }

            return Task.FromResult(files);
        }

        public Task<List<IFileMetadata>> ListFilesWithMetadataAsync(string absoluteDirectoryPath)
        {
            var fileSystemPath = GetFilePath(absoluteDirectoryPath);
            var dir = new DirectoryInfo(fileSystemPath);

            var files = new List<IFileMetadata>();

            if (dir.Exists)
            {
                var fileInfos = dir.GetFiles();
                foreach (var f in fileInfos)
                {
                    var file = new FileMetdataBase
                    {
                        FullPath = f.FullName, // full path of the file
                        FullDirectoryPath = f.Directory?.FullName, // directory path
                        AbsoluteDirectoryPath = GetAbsolutePath(f.Directory?.FullName), // absolute directory path for display/storage purposes, e.g. "/content/uploads"
                        AbsoluteFilePath = GetAbsolutePath(f.FullName), // absolute file path for display/storage purposes, e.g. "/content/uploads/myfile.txt"
                        FileName = f.Name, // file name
                        Extension = GetFileExtenstion(f.Name), // file extension (e.g., ".txt", ".jpg"). This is typically used for file type identification and handling.
                        Size = f.Length, // size in bytes
                        LastModified = new DateTimeOffset(f.LastWriteTimeUtc, TimeSpan.Zero) // last modified date
                    };
                    files.Add(file);
                }
            }

            return Task.FromResult(files);
        }

        public Task<IFileMetadata> GetFileMetadataAsync(string absoluteFilePath)
        {
            var fileSystemPath = GetFilePath(absoluteFilePath);
            if (!File.Exists(fileSystemPath))
            {
                throw new DataNotFoundException($"File not found: {fileSystemPath}");
            }

            var f = new FileInfo(fileSystemPath);
            var file = new FileMetdataBase
            {
                FullPath = f.FullName, // full path of the file
                FullDirectoryPath = f.Directory?.FullName, // directory path
                AbsoluteDirectoryPath = GetAbsolutePath(f.Directory?.FullName), // absolute directory path for display/storage purposes, e.g. "/content/uploads"
                AbsoluteFilePath = GetAbsolutePath(f.FullName), // absolute file path for display/storage purposes, e.g. "/content/uploads/myfile.txt"
                FileName = f.Name, // file name
                Extension = GetFileExtenstion(f.Name), // file extension (e.g., ".txt", ".jpg"). This is typically used for file type identification and handling.
                Size = f.Length, // size in bytes
                LastModified = new DateTimeOffset(f.LastWriteTimeUtc, TimeSpan.Zero) // last modified date
            };
            return Task.FromResult<IFileMetadata>(file);
        }


        public Task DeleteDirectoryAndContentsAsync(string absoluteDirectoryPath)
        {
            var fileSystemPath = GetFilePath(absoluteDirectoryPath);
            var dir = new DirectoryInfo(fileSystemPath);
            if (dir.Exists)
            {
                dir.Delete(recursive: true);
            }
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string absoluteFilePath)
        {
            var fileSystemPath = GetFilePath(absoluteFilePath);
            return Task.FromResult(File.Exists(fileSystemPath));
        }

        public Task WriteAllBytesAsync(string absoluteFilePath, IEnumerable<byte> data)
        {
            var fileSystemPath = GetFilePath(absoluteFilePath);
            var fileInfo = new FileInfo(fileSystemPath);
            if (fileSystemPath != null && !fileInfo.Directory.Exists)
            {
                Directory.CreateDirectory(fileInfo.Directory.FullName);
            }
            return File.WriteAllBytesAsync(fileSystemPath, data.ToArray());
        }

        public Task WriteAllTextAsync(string absoluteFilePath, string contents)
        {
            var fileSystemPath = GetFilePath(absoluteFilePath);
            var fileInfo = new FileInfo(fileSystemPath);
            if (fileSystemPath != null && !fileInfo.Directory.Exists)
            {
                Directory.CreateDirectory(fileInfo.Directory.FullName);
            }
            return File.WriteAllTextAsync(fileSystemPath, contents);
        }

        public async Task<Stream> GetStreamAsync(string absoluteFilePath)
        {
            var fileSystemPath = GetFilePathValidateExists(absoluteFilePath);
            return new MemoryStream(await File.ReadAllBytesAsync(fileSystemPath));
        }

        public Task<byte[]> ReadAllBytesAsync(string absoluteFilePath)
        {
            var fileSystemPath = GetFilePathValidateExists(absoluteFilePath);
            return File.ReadAllBytesAsync(fileSystemPath);
        }

        public async Task<string> ReadAllTextAsync(string absoluteFilePath)
        {
            var fileSystemPath = GetFilePathValidateExists(absoluteFilePath);
            var bytes = await File.ReadAllBytesAsync(fileSystemPath);
            return Encoding.UTF8.GetString(bytes);
        }

        public Task DeleteAsync(string absoluteFilePath)
        {
            var fileSystemPath = GetFilePathValidateExists(absoluteFilePath);
            File.Delete(fileSystemPath);
            return Task.CompletedTask;
        }

        //public Task<string[]> GetAllFilePathsOfType(string absoluteDirectoryPath, string fileType)
        //{
        //    var files = Directory.GetFiles(GetFilePath(absoluteDirectoryPath), $"*.{fileType}", SearchOption.AllDirectories);
        //    return Task.FromResult(files);
        //}

        #region helpers

        private string GetAbsolutePath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return string.Empty;
            }

            if (!fullPath.StartsWith(_environmentContext.ContentRootPath, StringComparison.OrdinalIgnoreCase))
            {
                // if the full path does not start with the content root path, return it as is, this should not happen in normal circumstances
                _logger.LogWarning($"The full path '{fullPath}' does not start with the content root path '{_environmentContext.ContentRootPath}'");
                return fullPath;
            }

            var length = _environmentContext.ContentRootPath.Length;
            var absolutePath = fullPath.Substring(length).SwitchBackSlashToForwardSlash().StripAllLeadingAndTrailingSlashes(); // ensure we are using the correct slash for the environment context
            return absolutePath;
        }

        private string GetFilePath(string absoluteFilePath) =>
            _environmentContext.UseForwardSlashes
            ? Path.GetFullPath(Format.PathMergeForwardSlashes(_environmentContext.ContentRootPath, absoluteFilePath.SwitchBackSlashToForwardSlash()))
            : Path.GetFullPath(Format.PathMerge(_environmentContext.ContentRootPath, absoluteFilePath.SwitchForwardSlashToBackSlash()));

        private string GetFilePathValidateExists(string absoluteFilePath)
        {
            var fileName = GetFilePath(absoluteFilePath);
            if (!File.Exists(fileName))
            {
                throw new DataNotFoundException(fileName);
            }
            else
            {
                return fileName;
            }
        }

        private string GetFileExtenstion(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return string.Empty;
            }
            var lastDotIndex = fileName.LastIndexOf('.');
            if (lastDotIndex < 0 || lastDotIndex == fileName.Length - 1)
            {
                return string.Empty; // No extension found
            }
            return fileName.Substring(lastDotIndex)?.ToLowerInvariant();
        }

        #endregion
    }
}

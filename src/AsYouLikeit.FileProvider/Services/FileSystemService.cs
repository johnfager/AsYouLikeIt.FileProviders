﻿using AsYouLikeIt.Sdk.Common.Exceptions;
using AsYouLikeIt.Sdk.Common.Extensions;
using AsYouLikeIt.Sdk.Common.Utilities;
using Microsoft.Extensions.Logging;
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

        private string GetFilePath(string absoluteFilePath) =>
            _environmentContext.UseForwardSlashed
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

        #endregion
    }
}

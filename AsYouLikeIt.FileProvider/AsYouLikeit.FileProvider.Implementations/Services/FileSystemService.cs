using AsYouLikeIt.Sdk.Common.Exceptions;
using AsYouLikeIt.Sdk.Common.Utilities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsYouLikeit.FileProviders.Services
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

        public Task<bool> DirectoryExistsAsync(string absoluteDirectoryPath)
        {
            return Task.FromResult(Directory.Exists(GetFilePath(absoluteDirectoryPath)));
        }

        public Task DeleteDirectoryAndContentsAsync(string absoluteDirectoryPath)
        {
            var dir = new DirectoryInfo(GetFilePath(absoluteDirectoryPath));
            if(dir.Exists)
            {
                dir.Delete(recursive: true);
            }
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string absoluteFilePath)
        {
            return Task.FromResult(File.Exists(GetFilePath(absoluteFilePath)));
        }

        public Task WriteAllBytesAsync(string absoluteFilePath, IEnumerable<byte> data)
        {
            var pathToFile = Path.GetDirectoryName(absoluteFilePath);

            if (pathToFile != null)
            {
                Directory.CreateDirectory(pathToFile);
            }

            return File.WriteAllBytesAsync(GetFilePath(absoluteFilePath), data.ToArray());
        }

        public Task WriteAllTextAsync(string absoluteFilePath, string contents)
        {
            var pathToFile = Path.GetDirectoryName(absoluteFilePath);

            if (pathToFile != null)
            {
                Directory.CreateDirectory(pathToFile);
            }

            return File.WriteAllTextAsync(GetFilePath(absoluteFilePath), contents);
        }

        public async Task<Stream> GetStreamAsync(string absoluteFilePath)
        {
            return new MemoryStream(await File.ReadAllBytesAsync(GetFilePathValidateExists(absoluteFilePath)));
        }

        public Task<byte[]> ReadAllBytesAsync(string absoluteFilePath)
        {
            return File.ReadAllBytesAsync(GetFilePathValidateExists(absoluteFilePath));
        }

        public async Task<string> ReadAllTextAsync(string absoluteFilePath)
        {
            var bytes = await File.ReadAllBytesAsync(GetFilePathValidateExists(absoluteFilePath));
            return Encoding.UTF8.GetString(bytes);
        }

        public Task DeleteAsync(string absoluteFilePath)
        {
            File.Delete(GetFilePathValidateExists(absoluteFilePath));
            return Task.CompletedTask;
        }

        //public Task<string[]> GetAllFilePathsOfType(string absoluteDirectoryPath, string fileType)
        //{
        //    var files = Directory.GetFiles(GetFilePath(absoluteDirectoryPath), $"*.{fileType}", SearchOption.AllDirectories);
        //    return Task.FromResult(files);
        //}

        #region helpers

        private string GetFilePath(string absoluteFilePath) => Format.PathMerge(_environmentContext.ContentRootPath, absoluteFilePath);

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

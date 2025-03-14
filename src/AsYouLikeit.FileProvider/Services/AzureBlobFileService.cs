using AsYouLikeIt.Sdk.Common.Exceptions;
using AsYouLikeIt.Sdk.Common.Extensions;
using AsYouLikeIt.Sdk.Common.Utilities;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsYouLikeIt.FileProviders.Services
{
    public class AzureBlobFileService : IFileService
    {
        public const string IDENTIFIER = nameof(AzureBlobFileService);

        private readonly StorageAccountConfig _storageAccountConfig;
        private readonly string _connString;
        private readonly ILogger<AzureBlobFileService> _logger;

        public string ImplementationIdentifier => IDENTIFIER;

        public AzureBlobFileService(StorageAccountConfig storageAccountConfig, ILogger<AzureBlobFileService> logger)
        {
            _storageAccountConfig = storageAccountConfig;
            var endpointSuffix = string.IsNullOrWhiteSpace(_storageAccountConfig.EndpointSuffix) ? "core.windows.net" : _storageAccountConfig.EndpointSuffix;
            _connString = $"DefaultEndpointsProtocol=https;AccountName={_storageAccountConfig.StorageAccountName};AccountKey={_storageAccountConfig.AccessKey};EndpointSuffix={endpointSuffix}";
            _logger = logger;
        }

        public async Task<bool> DirectoryExistsAsync(string absoluteDirectoryPath)
        {
            return await (await GetBlobClientAsync(absoluteDirectoryPath)).ExistsAsync();
        }

        public async Task DeleteDirectoryAndContentsAsync(string absoluteDirectoryPath)
        {
            var blobPath = this.GetBlobPath(absoluteDirectoryPath, rootPathIsOk: true);
            var blobContainerClient = await GetBlobContainerClientAsync(absoluteDirectoryPath);
            await foreach (BlobItem blobItem in blobContainerClient.GetBlobsAsync(prefix: blobPath.Path))
            {
                await blobContainerClient.DeleteBlobAsync(blobItem.Name, DeleteSnapshotsOption.IncludeSnapshots);
                Console.WriteLine($"Deleted blob: {blobItem.Name}");
            }
        }

        public async Task<List<string>> ListSubDirectoriesAsync(string absoluteDirectoryPath)
        {
            var blobPath = this.GetBlobPath(absoluteDirectoryPath, rootPathIsOk: true);
            var blobContainerClient = await GetBlobContainerClientAsync(absoluteDirectoryPath, rootPathIsOk: true);

            var directories = new List<string>();

            await foreach (var blobHierarchyItem in blobContainerClient.GetBlobsByHierarchyAsync(prefix: blobPath.Path + "/", delimiter: "/"))
            {
                if (blobHierarchyItem.IsPrefix && !string.Equals(blobHierarchyItem.Prefix, blobPath.Path))
                {
                    string childDirectory = blobHierarchyItem.Prefix.Substring(blobPath.Path.Length).StripAllLeadingAndTrailingSlashes();
                    directories.Add(childDirectory);
                }
            }

            return directories;
        }

        public async Task<List<string>> ListFilesAsync(string absoluteDirectoryPath)
        {
            var blobPath = this.GetBlobPath(absoluteDirectoryPath, rootPathIsOk: true);
            var blobContainerClient = await GetBlobContainerClientAsync(absoluteDirectoryPath, rootPathIsOk: true);

            var files = new List<string>();

            await foreach (var blobHierarchyItem in blobContainerClient.GetBlobsByHierarchyAsync(prefix: blobPath.Path + "/", delimiter: "/"))
            {
                if (!blobHierarchyItem.IsPrefix)
                {
                    string fileName = blobHierarchyItem.Blob.Name.Substring(blobPath.Path.Length).StripAllLeadingAndTrailingSlashes();
                    files.Add(fileName);
                }
            }

            return files;
        }


        public async Task<bool> ExistsAsync(string absoluteFilePath)
        {
            var blobClient = await GetBlobClientAsync(absoluteFilePath);
            return await blobClient.ExistsAsync();
        }

        public async Task WriteAllBytesAsync(string absoluteFilePath, IEnumerable<byte> data)
        {
            var blobClientAndBlobPath = await GetBlobClientAndBlobPathAsync(absoluteFilePath);
            using (var stream = new MemoryStream(data.ToArray()))
            {
                await blobClientAndBlobPath.BlobClient.UploadAsync(stream, overwrite: true);
            }
            var metadata = new Dictionary<string, string>
            {
                { "absoluteFilePath", absoluteFilePath },
                { "originalFileNameCase", blobClientAndBlobPath.BlobPath.OriginalFileNameCase }
            };
            await blobClientAndBlobPath.BlobClient.SetMetadataAsync(metadata);
        }

        public Task WriteAllTextAsync(string absoluteFilePath, string content)
        {
            var data = Encoding.UTF8.GetBytes(content);
            return WriteAllBytesAsync(absoluteFilePath, data);
        }

        public async Task<Stream> GetStreamAsync(string absoluteFilePath)
        {
            var blobClient = await GetBlobClientAsync(absoluteFilePath);
            if (!await blobClient.ExistsAsync())
            {
                throw new DataNotFoundException(absoluteFilePath);
            }

            //Read operation: Read the contents of the blob.
            var msRead = new MemoryStream();
            await blobClient.DownloadToAsync(msRead);
            msRead.Position = 0;
            return msRead;
        }

        public async Task<byte[]> ReadAllBytesAsync(string absoluteFilePath)
        {
            var blobClient = await GetBlobClientAsync(absoluteFilePath);
            if (!await blobClient.ExistsAsync())
            {
                throw new DataNotFoundException(absoluteFilePath);
            }

            //Read operation: Read the contents of the blob.
            byte[] bytes = null;
            using (var msRead = new MemoryStream())
            {
                await blobClient.DownloadToAsync(msRead);
                msRead.Position = 0;
                bytes = msRead.ToArray();
            }
            return bytes;
        }

        public async Task DeleteAsync(string absoluteFilePath)
        {
            var blobClient = await GetBlobClientAsync(absoluteFilePath);
            if (!await blobClient.ExistsAsync())
            {
                throw new DataNotFoundException(absoluteFilePath);
            }
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<string> ReadAllTextAsync(string absoluteFilePath)
        {
            var bytes = await ReadAllBytesAsync(absoluteFilePath);
            return Encoding.UTF8.GetString(bytes);
        }

        #region helpers

        private async Task<BlobContainerClient> GetBlobContainerClientAsync(string pathPrefix, bool rootPathIsOk = false)
        {
            var blobPath = GetBlobPath(pathPrefix, rootPathIsOk);

            var blobServiceClient = new BlobServiceClient(_connString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(blobPath.ContainerName);
            await blobContainerClient.CreateIfNotExistsAsync();

            //var metadata = new Dictionary<string, string>
            //{
            //    { "absoluteFilePath", pathPrefix }
            //};
            //await blobContainerClient.SetMetadataAsync(metadata);

            return blobContainerClient;
        }
        private async Task<(BlobClient BlobClient, BlobPath BlobPath)> GetBlobClientAndBlobPathAsync(string absolutePath)
        {
            var blobPath = GetBlobPath(absolutePath);

            var blobContainerClient = new BlobContainerClient(_connString, blobPath.ContainerName);
            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var blobClient = blobContainerClient.GetBlobClient(blobPath.Path);
            return (blobClient, blobPath);
        }

        private async Task<BlobClient> GetBlobClientAsync(string absolutePath) => (await GetBlobClientAndBlobPathAsync(absolutePath)).BlobClient;

        private BlobPath GetBlobPath(string absoluteFilePath, bool rootPathIsOk = false)
        {
            var originalFileName = absoluteFilePath.SplitStringAndTrim("/").ToList().Last();
            var blobFilePath = absoluteFilePath.MakeBlobNameSafe(makeLower: _storageAccountConfig.UseLowerCase);
            _logger.LogDebug($"blobFilePath:\t{blobFilePath}");
            var segments = blobFilePath.SplitStringAndTrim("/").ToList();

            if (segments.Count == 1 && rootPathIsOk)
            {
                return new BlobPath() { ContainerName = segments.First(), Path = string.Empty, OriginalPathCase = absoluteFilePath, OriginalFileNameCase = originalFileName };
            }
            else if (segments.Count < 2)
            {
                throw new FriendlyArgumentException(nameof(absoluteFilePath), $"{nameof(absoluteFilePath)} '{absoluteFilePath}' is not valid.");
            }
       
            return new BlobPath() { ContainerName = segments.First(), Path = Format.PathMergeForwardSlashes(segments.Skip(1).ToArray()), OriginalPathCase = absoluteFilePath, OriginalFileNameCase = originalFileName };
        }

        private struct BlobPath
        {
            public string ContainerName;

            public string Path;

            public string OriginalPathCase;

            public string OriginalFileNameCase;
        }

        #endregion
    }
}

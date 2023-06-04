using AsYouLikeIt.Sdk.Common.Exceptions;
using AsYouLikeIt.Sdk.Common.Extensions;
using AsYouLikeIt.Sdk.Common.Utilities;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace AsYouLikeit.FileProviders.Services
{
    public class AzureBlobFileService : IFileService
    {
        public const string IDENTIFIER = nameof(AzureBlobFileService);

        private readonly StorageAccountConfig _resourceAssetConfig;
        private readonly string _connString;
        private readonly ILogger<AzureBlobFileService> _logger;

        public string ImplementationIdentifier => IDENTIFIER;

        public AzureBlobFileService(StorageAccountConfig resourceAssetConfig, ILogger<AzureBlobFileService> logger)
        {
            _resourceAssetConfig = resourceAssetConfig;
            _connString = $"DefaultEndpointsProtocol=https;AccountName={resourceAssetConfig.StorageAccountName};AccountKey={resourceAssetConfig.AccessKey};EndpointSuffix=core.windows.net";
            _logger = logger;
        }

        public async Task<bool> DirectoryExistsAsync(string absoluteDirectoryPath)
        {
            return await (await GetBlobClientAsync(absoluteDirectoryPath)).ExistsAsync();
        }

        public async Task DeleteDirectoryAndContentsAsync(string absoluteDirectoryPath)
        {
            var blobPath = this.GetBlobPath(absoluteDirectoryPath);
            var blobContainerClient = await GetBlobContainerClientAsync(absoluteDirectoryPath);
            await foreach (BlobItem blobItem in blobContainerClient.GetBlobsAsync(prefix: blobPath.Path))
            {
                await blobContainerClient.DeleteBlobAsync(blobItem.Name, DeleteSnapshotsOption.IncludeSnapshots);
                Console.WriteLine($"Deleted blob: {blobItem.Name}");
            }
        }

        public async Task<List<string>> ListSubDirectoriesAsync(string absoluteDirectoryPath)
        {
            var blobPath = this.GetBlobPath(absoluteDirectoryPath);
            var blobContainerClient = await GetBlobContainerClientAsync(absoluteDirectoryPath);

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

        public async Task<bool> ExistsAsync(string absoluteFilePath)
        {
            var blobClient = await GetBlobClientAsync(absoluteFilePath);
            return await blobClient.ExistsAsync();
        }

        public async Task WriteAllBytesAsync(string absoluteFilePath, IEnumerable<byte> data)
        {
            var blobClient = await GetBlobClientAsync(absoluteFilePath);
            using (var stream = new MemoryStream(data.ToArray()))
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }
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

        public void Dispose()
        {
        }

        #region helpers

        private async Task<BlobContainerClient> GetBlobContainerClientAsync(string pathPrefix)
        {
            var blobPath = GetBlobPath(pathPrefix);

            var blobServiceClient = new BlobServiceClient(_connString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(blobPath.ContainerName);
            await blobContainerClient.CreateIfNotExistsAsync();

            return blobContainerClient;
        }

        private async Task<BlobClient> GetBlobClientAsync(string absolutePath)
        {
            var blobPath = GetBlobPath(absolutePath);

            var blobContainerClient = new BlobContainerClient(_connString, blobPath.ContainerName);
            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            return blobContainerClient.GetBlobClient(blobPath.Path);
        }

        private BlobPath GetBlobPath(string absoluteFilePath)
        {
            absoluteFilePath = absoluteFilePath.SwitchBackSlashToForwardSlash();
            var segments = absoluteFilePath.SplitStringAndTrim("/").ToList();
            if (segments.Count < 2)
            {
                throw new FriendlyArgumentException(nameof(absoluteFilePath), $"{nameof(absoluteFilePath)} '{absoluteFilePath}' is not valid.");
            }
            return new BlobPath() { ContainerName = segments.First(), Path = Format.PathMergeForwardSlashes(segments.Skip(1).ToArray()) };
        }

        private struct BlobPath
        {
            public string ContainerName;

            public string Path;
        }

        #endregion
    }
}

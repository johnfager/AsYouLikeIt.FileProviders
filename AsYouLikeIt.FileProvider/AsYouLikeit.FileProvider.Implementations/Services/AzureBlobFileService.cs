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

namespace AsYouLikeit.FileProviders.Services
{
    public class AzureBlobFileService : IFileService
    {
        public const string IDENTIFIER = nameof(AzureBlobFileService);

        private readonly StorageAccountConfig _resourceAssetConfig;
        private readonly string _connString;
        private readonly ILogger<AzureBlobFileService> _logger;

        private BlobClient _blobClient;

        public string ImplementationIdentifier => IDENTIFIER;

        public AzureBlobFileService(StorageAccountConfig resourceAssetConfig, ILogger<AzureBlobFileService> logger)
        {
            _resourceAssetConfig = resourceAssetConfig;
            _connString = $"DefaultEndpointsProtocol=https;AccountName={resourceAssetConfig.StorageAccountName};AccountKey={resourceAssetConfig.AccessKey};EndpointSuffix=core.windows.net";
            _logger = logger;
        }

        public async Task<bool> DirectoryExistsAsync(string absoluteDirectoryPath)
        {
            return (await (await GetBlobClientAsync(absoluteDirectoryPath)).ExistsAsync()).Value;
        }

        public async Task<bool> ExistsAsync(string absoluteFilePath)
        {
            return (await (await GetBlobClientAsync(absoluteFilePath)).ExistsAsync()).Value;
        }

        public async Task WriteAllBytesAsync(string absoluteFilePath, IEnumerable<byte> data)
        {
            var blobClient = await GetBlobClientAsync(absoluteFilePath);
            await blobClient.DeleteIfExistsAsync();
            await blobClient.UploadAsync(new MemoryStream(data.ToArray()));
        }

        public async Task<Stream> GetStreamAsync(string absoluteFilePath)
        {
            var blobClient = await GetBlobClientAsync(absoluteFilePath);
            if (!(await blobClient.ExistsAsync()).Value)
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
            if (!(await blobClient.ExistsAsync()).Value)
            {
                throw new DataNotFoundException(absoluteFilePath);
            }

            //Read operation: Read the contents of the blob.
            byte[] bytes = null;
            var msRead = new MemoryStream();
            using (msRead)
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
            if (!(await blobClient.ExistsAsync()).Value)
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

        private async Task<BlobClient> GetBlobClientAsync(string absolutePath)
        {
            if (_blobClient != null)
            {
                var blobPath = GetBlobPath(absolutePath);

                var blobContainerClient = new BlobContainerClient(_connString, blobPath.ContainerName);
                await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                _blobClient = blobContainerClient.GetBlobClient(blobPath.Path);
            }
            return _blobClient;
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

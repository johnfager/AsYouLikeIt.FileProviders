using AsYouLikeit.FileProviders;
using AsYouLikeit.FileProviders.Services;
using AsYouLikeIt.Sdk.Common.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace AsYouLikeIt.FileProvider.Tests
{
    public class FileServiceTest
    {
        private readonly IFileService _fileService;
        private readonly StorageAccountConfig _storageAccountConfig;

        public FileServiceTest()
        {
            // Build the configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.development.json")
                .Build();

            // Bind the configuration to the strongly typed class
            _storageAccountConfig = new StorageAccountConfig();
            configuration.GetSection(nameof(StorageAccountConfig)).Bind(_storageAccountConfig);

            var loggerMoq = new Mock<ILogger<AzureBlobFileService>>();

            _fileService = new AzureBlobFileService(_storageAccountConfig, loggerMoq.Object);
        }

        [Fact]
        public void ValidateStorageAccount()
        {
            Assert.NotNull(_storageAccountConfig);
            Assert.NotNull(_storageAccountConfig.StorageAccountName);
            Assert.NotNull(_storageAccountConfig.AccessKey);
        }

        [Fact]
        public void ValidateService()
        {
            Assert.NotNull(_fileService);
        }

        [Fact]
        public async Task ValidateFileServiceWriteTextAsync()
        {
            var str1 = "My file contents 1";
            var str2 = "Second file contents.\nEnd of line.";

            var filePath = "testcontainer/validation/test.txt";

            await _fileService.WriteAllTextAsync(filePath, str1);
            Assert.True(await _fileService.ExistsAsync(filePath));

            var contents1 = await _fileService.ReadAllTextAsync(filePath);
            Assert.Equal(str1, contents1);

            await _fileService.WriteAllTextAsync(filePath, str2);
            Assert.True(await _fileService.ExistsAsync(filePath));

            var contents2 = await _fileService.ReadAllTextAsync(filePath);
            Assert.Equal(str2, contents2);

            await _fileService.DeleteAsync(filePath);

            Assert.False(await _fileService.ExistsAsync(filePath));

        }


        [Fact]
        public async Task ValidateMultipleFileServiceWriteTextAsync()
        {
            var str1 = "My file contents 1";
            var str2 = "Second file contents.\nEnd of line.";

            var filePath = "testcontainer/validation/test.txt";
            var filePath2 = "testcontainer/validation/test2.txt";

            await _fileService.WriteAllTextAsync(filePath, str1);
            Assert.True(await _fileService.ExistsAsync(filePath));

            var contents1 = await _fileService.ReadAllTextAsync(filePath);
            Assert.Equal(str1, contents1);

            await _fileService.WriteAllTextAsync(filePath2, str2);
            Assert.True(await _fileService.ExistsAsync(filePath2));

            var contents2 = await _fileService.ReadAllTextAsync(filePath2);
            Assert.Equal(str2, contents2);

            await _fileService.DeleteAsync(filePath);
            Assert.False(await _fileService.ExistsAsync(filePath));

            Assert.True(await _fileService.ExistsAsync(filePath2));

            await _fileService.DeleteAsync(filePath2);
            Assert.False(await _fileService.ExistsAsync(filePath2));
        }


        [Fact]
        public async Task ValidationFileServiceDirectoryAsync()
        {
            var str1 = "My file contents 1";
            var str2 = "Second file contents.\nEnd of line.";

            var directoryPath = "testcontainer/validation";

            var filePath = Format.PathMergeForwardSlashes(directoryPath, "test.txt");
            var filePath2 = Format.PathMergeForwardSlashes(directoryPath, "test2.txt");

            await _fileService.WriteAllTextAsync(filePath, str1);
            Assert.True(await _fileService.ExistsAsync(filePath));

            await _fileService.WriteAllTextAsync(filePath2, str2);
            Assert.True(await _fileService.ExistsAsync(filePath2));

            await _fileService.DeleteDirectoryAndContentsAsync(directoryPath);

            Assert.False(await _fileService.ExistsAsync(filePath));
            Assert.False(await _fileService.ExistsAsync(filePath2));
        }
    }
}
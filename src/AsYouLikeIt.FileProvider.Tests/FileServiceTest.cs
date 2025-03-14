using AsYouLikeIt.FileProviders;
using AsYouLikeIt.FileProviders.Services;
using AsYouLikeIt.Sdk.Common.Extensions;
using AsYouLikeIt.Sdk.Common.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;

namespace AsYouLikeIt.FileProvider.Tests
{

    public class AzureBlobFileServiceTests : FileServiceTest
    {
        public AzureBlobFileServiceTests()
        {
            // Build the configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.development.json")
                .Build();

            // Bind the configuration to the strongly typed class
            var storageAccountConfig = new StorageAccountConfig();
            configuration.GetSection(nameof(StorageAccountConfig)).Bind(storageAccountConfig);

            var loggerMoq = new Mock<ILogger<AzureBlobFileService>>();

            _fileService = new AzureBlobFileService(storageAccountConfig, loggerMoq.Object);
        }

        [Fact]
        public async Task ValidateProjectXmlCase()
        {
            var filePath = "testprojectdefinitions/projectdefs/test1/ProjectSpecification.xml";
            Assert.True(await _fileService.ExistsAsync(filePath));
            var xmlData = await _fileService.ReadAllTextAsync(filePath);
            Assert.True(!string.IsNullOrWhiteSpace(xmlData));   
        }

    }

    public class FileSystemServiceTests : FileServiceTest
    {
        public FileSystemServiceTests()
        {
            var environmentContext = new EnvironmentContext() { ContentRootPath = Format.PathMerge(AppContext.BaseDirectory, "test-file-store") };
            var loggerMoq = new Mock<ILogger<FileSystemService>>();
            _fileService = new FileSystemService(environmentContext, loggerMoq.Object);
        }
    }


    public abstract class FileServiceTest
    {
        protected IFileService _fileService;

        //protected abstract IServiceProvider GetServiceProvider();


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

        [Fact]
        public async Task ValidationFileServiceBytesAsync()
        {
            var bytes1 = GenerateRandomBytes();
            var bytes2 = GenerateRandomBytes();

            var directoryPath = "testcontainer/validation-bin";

            var filePath1 = Format.PathMergeForwardSlashes(directoryPath, "bytes.bin");
            var filePath2 = Format.PathMergeForwardSlashes(directoryPath, "bytes2.bin");

            await _fileService.WriteAllBytesAsync(filePath1, bytes1);
            Assert.True(await _fileService.ExistsAsync(filePath1));

            var contents1 = await _fileService.ReadAllBytesAsync(filePath1);
            Assert.True(bytes1.SequenceEqual(contents1));

            await _fileService.WriteAllBytesAsync(filePath2, bytes2);
            Assert.True(await _fileService.ExistsAsync(filePath2));

            var contents2 = await _fileService.ReadAllBytesAsync(filePath2);
            Assert.True(bytes2.SequenceEqual(contents2));

            await _fileService.DeleteDirectoryAndContentsAsync(directoryPath);

            Assert.False(await _fileService.ExistsAsync(filePath1));
            Assert.False(await _fileService.ExistsAsync(filePath2));
        }

        [Fact]
        public async Task TestDirectoriesAsync()
        {
            var bytes1 = GenerateRandomBytes();
            var bytes2 = GenerateRandomBytes();
            var bytes3 = GenerateRandomBytes();

            var directoryPath = "testcontainer/validation-dirs";

            var filePath1 = Format.PathMergeForwardSlashes(directoryPath, "A/bytes.bin");
            var filePath2 = Format.PathMergeForwardSlashes(directoryPath, "B/bytes2.bin");
            var filePath3 = Format.PathMergeForwardSlashes(directoryPath, "C/bytes3.bin");

            await _fileService.WriteAllBytesAsync(filePath1, bytes1);
            await _fileService.WriteAllBytesAsync(filePath2, bytes2);
            await _fileService.WriteAllBytesAsync(filePath3, bytes3);

            var dirs = await _fileService.ListSubDirectoriesAsync(directoryPath);

            Assert.True(dirs.Count == 3);
            Assert.True(dirs[0].EqualsCaseInsensitive("A"));
            Assert.True(dirs[1].EqualsCaseInsensitive("B"));
            Assert.True(dirs[2].EqualsCaseInsensitive("C"));

            await _fileService.DeleteDirectoryAndContentsAsync(directoryPath);
        }

        [Fact]
        public async Task TestFilesAsync()
        {
            var bytes1 = GenerateRandomBytes();
            var bytes2 = GenerateRandomBytes();
            var bytes3 = GenerateRandomBytes();

            var directoryPath = "testcontainer/validation-dirs";

            var filePath1 = Format.PathMergeForwardSlashes(directoryPath, "bytes.bin");
            var filePath2 = Format.PathMergeForwardSlashes(directoryPath, "bytes2.bin");
            var filePath3 = Format.PathMergeForwardSlashes(directoryPath, "bytes3.bin");

            await _fileService.WriteAllBytesAsync(filePath1, bytes1);
            await _fileService.WriteAllBytesAsync(filePath2, bytes2);
            await _fileService.WriteAllBytesAsync(filePath3, bytes3);

            // create a sub directory and file
            var filePathSub = Format.PathMergeForwardSlashes(directoryPath, "A/bytes.bin");
            await _fileService.WriteAllBytesAsync(filePathSub, bytes1);

            var files = await _fileService.ListFilesAsync(directoryPath);

            Assert.Equal(3, files.Count);
            Assert.True(files[0].EqualsCaseInsensitive("bytes.bin"));
            Assert.True(files[1].EqualsCaseInsensitive("bytes2.bin"));
            Assert.True(files[2].EqualsCaseInsensitive("bytes3.bin"));

            await _fileService.DeleteDirectoryAndContentsAsync(directoryPath);
        }


        [Fact]
        public async Task TestFiles2Async()
        {
            var bytes1 = GenerateRandomBytes();
            var bytes2 = GenerateRandomBytes();
            var bytes3 = GenerateRandomBytes();

            var directoryPath = "booker/xml/monthly/";

            var filePath1 = Format.PathMergeForwardSlashes(directoryPath, "bytes.bin");
            var filePath2 = Format.PathMergeForwardSlashes(directoryPath, "bytes2.bin");
            var filePath3 = Format.PathMergeForwardSlashes(directoryPath, "bytes3.bin");

            await _fileService.WriteAllBytesAsync(filePath1, bytes1);
            await _fileService.WriteAllBytesAsync(filePath2, bytes2);
            await _fileService.WriteAllBytesAsync(filePath3, bytes3);

            // create a sub directory and file
            var filePathSub = Format.PathMergeForwardSlashes(directoryPath, "A/bytes.bin");
            await _fileService.WriteAllBytesAsync(filePathSub, bytes1);

            var files = await _fileService.ListFilesAsync(directoryPath);

            Assert.Equal(3, files.Count);
            Assert.True(files[0].EqualsCaseInsensitive("bytes.bin"));
            Assert.True(files[1].EqualsCaseInsensitive("bytes2.bin"));
            Assert.True(files[2].EqualsCaseInsensitive("bytes3.bin"));

            await _fileService.DeleteDirectoryAndContentsAsync(directoryPath);
        }

        [Fact]
        public async Task TestRootDirectory()
        {
            var bytes1 = GenerateRandomBytes();
            var bytes2 = GenerateRandomBytes();
            var bytes3 = GenerateRandomBytes();

            var directoryPath = "booker";

            var filePath1 = Format.PathMergeForwardSlashes(directoryPath, "bytes.bin");
            var filePath2 = Format.PathMergeForwardSlashes(directoryPath, "bytes2.bin");
            var filePath3 = Format.PathMergeForwardSlashes(directoryPath, "bytes3.bin");

            await _fileService.WriteAllBytesAsync(filePath1, bytes1);
            await _fileService.WriteAllBytesAsync(filePath2, bytes2);
            await _fileService.WriteAllBytesAsync(filePath3, bytes3);

            // create a sub directory and file
            var filePathSub = Format.PathMergeForwardSlashes(directoryPath, "A/bytes.bin");
            await _fileService.WriteAllBytesAsync(filePathSub, bytes1);

            var files = await _fileService.ListFilesAsync(directoryPath);

            Assert.Equal(3, files.Count);
            Assert.True(files[0].EqualsCaseInsensitive("bytes.bin"));
            Assert.True(files[1].EqualsCaseInsensitive("bytes2.bin"));
            Assert.True(files[2].EqualsCaseInsensitive("bytes3.bin"));

            await _fileService.DeleteDirectoryAndContentsAsync(directoryPath);
        }




        #region helpers

        public byte[] GenerateRandomBytes()
        {
            byte[] randomBytes = new byte[256];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return randomBytes;
        }

        #endregion

    }
}
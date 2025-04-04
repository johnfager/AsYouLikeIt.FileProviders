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

        [Fact]
        public async Task TestComplexFileName()
        {
            var bytes1 = GenerateRandomBytes();
            var bytes2 = GenerateRandomBytes();
            var bytes3 = GenerateRandomBytes();

            var directoryPath = "booker/sde";

            var filePath1 = Format.PathMergeForwardSlashes(directoryPath, "bytes1@2025.03.16.bin");
            var filePath2 = Format.PathMergeForwardSlashes(directoryPath, "bytes2-@2025.03.16.bin");
            var filePath3 = Format.PathMergeForwardSlashes(directoryPath, "Hist_FromDate_2024-07-01_ToDate_2024-12-31_run@03.16.2025.Payments.csv");

            await _fileService.WriteAllBytesAsync(filePath1, bytes1);
            await _fileService.WriteAllBytesAsync(filePath2, bytes2);
            await _fileService.WriteAllBytesAsync(filePath3, bytes3);

            var files = await _fileService.ListFilesAsync(directoryPath);

            Assert.Equal(3, files.Count);
            Assert.True(files[0].EqualsCaseInsensitive("bytes1@2025.03.16.bin"));
            Assert.True(files[1].EqualsCaseInsensitive("bytes2-@2025.03.16.bin"));
            Assert.True(files[2].EqualsCaseInsensitive("Hist_FromDate_2024-07-01_ToDate_2024-12-31_run@03.16.2025.Payments.csv"));

            await _fileService.DeleteDirectoryAndContentsAsync(directoryPath);
        }

        [Fact]
        public async Task TestFileMetadataAsync()
        {

            var directoryPath = "filemetadata/aaaa/bbb/";
            var startTimeOffset = DateTimeOffset.UtcNow;

            var size1 = 256;
            var size2 = 500; // 500 bytes
            var size3 = 1086; // 1086 bytes, to test larger file sizes

            var bytes1 = GenerateRandomBytes(size1);
            var bytes2 = GenerateRandomBytes(size2);
            var bytes3 = GenerateRandomBytes(size3);

            var filePath1 = Format.PathMergeForwardSlashes(directoryPath, "bytes.bin");
            var filePath2 = Format.PathMergeForwardSlashes(directoryPath, "bytes2.bin");
            var filePath3 = Format.PathMergeForwardSlashes(directoryPath, "bytes3.bin");

            await _fileService.WriteAllBytesAsync(filePath1, bytes1);
            await _fileService.WriteAllBytesAsync(filePath2, bytes2);
            await _fileService.WriteAllBytesAsync(filePath3, bytes3);

            var files = await _fileService.ListFilesWithMetadataAsync(directoryPath);
            Assert.Equal(3, files.Count);
            Assert.True(files[0].FileName.EqualsCaseInsensitive("bytes.bin"));
            Assert.True(files[1].FileName.EqualsCaseInsensitive("bytes2.bin"));
            Assert.True(files[2].FileName.EqualsCaseInsensitive("bytes3.bin"));

            // check the metadata for each file
            
            // check full path
            Assert.True(files[0].FullPath.EndsWith(Format.PathMergeForwardSlashes(directoryPath, "bytes.bin")),
                $"FullPath should equal '{Format.PathMergeForwardSlashes(directoryPath, "bytes.bin")}'");
            Assert.True(files[1].FullPath.EndsWith(Format.PathMergeForwardSlashes(directoryPath, "bytes2.bin")),
                $"FullPath should equal '{Format.PathMergeForwardSlashes(directoryPath, "bytes2.bin")}'");
            Assert.True(files[2].FullPath.EndsWith(Format.PathMergeForwardSlashes(directoryPath, "bytes3.bin")),
                $"FullPath should equal '{Format.PathMergeForwardSlashes(directoryPath, "bytes3.bin")}'");

            // check the full directory path
            foreach (var f in files)
            {
                // Ensure the full directory path is correct
                Assert.True(f.FullDirectoryPath.Contains(Format.PathMergeForwardSlashes(directoryPath)),
                    $"FullDirectoryPath should be '{Format.PathMergeForwardSlashes(directoryPath)}' for file '{f.FileName}'");
            }

            // check relative directory path
            Assert.True(files[0].RelativeDirectoryPath.EqualsCaseInsensitive(directoryPath),
                $"RelativeDirectoryPath should be '{directoryPath}' for file '{files[0].FileName}'");
            Assert.True(files[1].RelativeDirectoryPath.EqualsCaseInsensitive(directoryPath),
                $"RelativeDirectoryPath should be '{directoryPath}' for file '{files[1].FileName}'");
            Assert.True(files[2].RelativeDirectoryPath.EqualsCaseInsensitive(directoryPath),
                $"RelativeDirectoryPath should be '{directoryPath}' for file '{files[2].FileName}'");

            // check relative path
            Assert.True(files[0].RelativeFilePath == Format.PathMergeForwardSlashes(directoryPath, "bytes.bin"),
                $"FullPath should equal '{Format.PathMergeForwardSlashes(directoryPath, "bytes.bin")}'");
            Assert.True(files[1].RelativeFilePath == Format.PathMergeForwardSlashes(directoryPath, "bytes2.bin"),
                $"FullPath should equal '{Format.PathMergeForwardSlashes(directoryPath, "bytes2.bin")}'");
            Assert.True(files[2].RelativeFilePath == Format.PathMergeForwardSlashes(directoryPath, "bytes3.bin"),
                $"FullPath should equal '{Format.PathMergeForwardSlashes(directoryPath, "bytes3.bin")}'");

            // check file sizes
            Assert.True(files[0].Size == size1, $"Size of bytes.bin should be {size1}");
            Assert.True(files[1].Size == size2, $"Size of bytes2.bin should be {size2}");
            Assert.True(files[2].Size == size3, $"Size of bytes3.bin should be {size3}");

            // check last modified date
            foreach (var f in files)
            {
                Assert.True(f.LastModified > startTimeOffset);
            }

            // check the extensions
            foreach(var f in files)
            {
                // Ensure the file extension is correct
                var expectedExtension = ".bin"; // default expected extension for the test files
                Assert.True(f.Extension.EqualsCaseInsensitive(expectedExtension),
                    $"FileExtension should be '{expectedExtension}' for file '{f.FileName}'");
            }

            await _fileService.DeleteDirectoryAndContentsAsync(directoryPath);
        }




        #region helpers

        public byte[] GenerateRandomBytes(int bytes = 256)
        {
            byte[] randomBytes = new byte[bytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return randomBytes;
        }

        #endregion

    }
}
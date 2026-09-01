using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TicketHub.Application.Common.Models;
using TicketHub.Application.Interfaces;
using TicketHub.Infrastructure.Services;
using Xunit;

namespace TicketHub.Tests.bUnit
{
    public class StorageProviderTests : IDisposable
    {
        private readonly string _testTempDir;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly Mock<ILogger<LocalFileStorageService>> _mockLocalLogger;
        private readonly Mock<ILogger<S3FileStorageService>> _mockS3Logger;
        private readonly Mock<IAmazonS3> _mockS3Client;

        public StorageProviderTests()
        {
            _testTempDir = Path.Combine(Path.GetTempPath(), "TicketHub_Storage_Tests_" + Guid.NewGuid());
            Directory.CreateDirectory(_testTempDir);

            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockEnv.Setup(e => e.WebRootPath).Returns(_testTempDir);

            _mockLocalLogger = new Mock<ILogger<LocalFileStorageService>>();
            _mockS3Logger = new Mock<ILogger<S3FileStorageService>>();
            _mockS3Client = new Mock<IAmazonS3>();
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testTempDir))
                {
                    Directory.Delete(_testTempDir, true);
                }
            }
            catch
            {
                // Ignored
            }
        }

        [Fact]
        public async Task LocalFileStorageService_SaveFileAsync_SavesFileToDiskAndReturnsRelativePath()
        {
            // Arrange
            var options = Options.Create(new StorageSettings { LocalPath = "uploads/attachments" });
            var service = new LocalFileStorageService(_mockEnv.Object, _mockLocalLogger.Object, options);
            var fileContent = Encoding.UTF8.GetBytes("Sample file content for unit testing");
            using var stream = new MemoryStream(fileContent);

            // Act
            var relativePath = await service.SaveFileAsync(stream, "test_document.txt");

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(relativePath));
            Assert.StartsWith("uploads/attachments/", relativePath);
            Assert.EndsWith("test_document.txt", relativePath);

            var absolutePath = Path.Combine(_testTempDir, relativePath);
            Assert.True(File.Exists(absolutePath));

            var savedText = await File.ReadAllTextAsync(absolutePath);
            Assert.Equal("Sample file content for unit testing", savedText);
        }

        [Fact]
        public async Task LocalFileStorageService_GetFileStreamAsync_ReturnsValidStream_WhenFileExists()
        {
            // Arrange
            var service = new LocalFileStorageService(_mockEnv.Object, _mockLocalLogger.Object);
            var uploadsDir = Path.Combine(_testTempDir, "uploads", "attachments");
            Directory.CreateDirectory(uploadsDir);

            var filePath = Path.Combine(uploadsDir, "existing_file.txt");
            await File.WriteAllTextAsync(filePath, "Read stream test content");

            // Act
            using var stream = await service.GetFileStreamAsync("uploads/attachments/existing_file.txt");

            // Assert
            Assert.NotNull(stream);
            using var reader = new StreamReader(stream!);
            var content = await reader.ReadToEndAsync();
            Assert.Equal("Read stream test content", content);
        }

        [Fact]
        public async Task LocalFileStorageService_GetFileStreamAsync_ReturnsNull_WhenFileDoesNotExist()
        {
            // Arrange
            var service = new LocalFileStorageService(_mockEnv.Object, _mockLocalLogger.Object);

            // Act
            var stream = await service.GetFileStreamAsync("uploads/attachments/non_existent.txt");

            // Assert
            Assert.Null(stream);
        }

        [Fact]
        public async Task LocalFileStorageService_GetFileUrlAsync_ReturnsNormalizedRelativeUrl()
        {
            // Arrange
            var service = new LocalFileStorageService(_mockEnv.Object, _mockLocalLogger.Object);

            // Act
            var url = await service.GetFileUrlAsync("uploads/attachments/pic.png");

            // Assert
            Assert.Equal("/uploads/attachments/pic.png", url);
        }

        [Fact]
        public async Task LocalFileStorageService_DeleteFile_DeletesFileFromDisk()
        {
            // Arrange
            var service = new LocalFileStorageService(_mockEnv.Object, _mockLocalLogger.Object);
            var uploadsDir = Path.Combine(_testTempDir, "uploads", "attachments");
            Directory.CreateDirectory(uploadsDir);

            var filePath = Path.Combine(uploadsDir, "file_to_delete.txt");
            await File.WriteAllTextAsync(filePath, "Delete me");
            Assert.True(File.Exists(filePath));

            // Act
            await service.DeleteFileAsync("uploads/attachments/file_to_delete.txt");

            // Assert
            Assert.False(File.Exists(filePath));
        }

        [Fact]
        public async Task S3FileStorageService_SaveFileAsync_CallsPutObjectWithCorrectParameters()
        {
            // Arrange
            var settings = new StorageSettings
            {
                Provider = "MinIO",
                BucketName = "tickethub-test-bucket",
                Endpoint = "http://localhost:9000"
            };
            var options = Options.Create(settings);

            _mockS3Client
                .Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutObjectResponse());

            _mockS3Client
                .Setup(s => s.ListBucketsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListBucketsResponse
                {
                    Buckets = new List<S3Bucket> { new() { BucketName = "tickethub-test-bucket" } }
                });

            var service = new S3FileStorageService(_mockS3Client.Object, options, _mockS3Logger.Object);
            var fileBytes = Encoding.UTF8.GetBytes("S3 Object payload");
            using var stream = new MemoryStream(fileBytes);

            // Act
            var key = await service.SaveFileAsync(stream, "report.pdf", "uploads/tickets");

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(key));
            Assert.StartsWith("uploads/tickets/", key);
            Assert.EndsWith("report.pdf", key);

            _mockS3Client.Verify(s => s.PutObjectAsync(
                It.Is<PutObjectRequest>(r =>
                    r.BucketName == "tickethub-test-bucket" &&
                    r.Key == key &&
                    r.ContentType == "application/pdf" &&
                    r.DisablePayloadSigning == true),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task S3FileStorageService_GetFileStreamAsync_ReturnsResponseStream()
        {
            // Arrange
            var settings = new StorageSettings
            {
                Provider = "S3",
                BucketName = "my-s3-bucket"
            };
            var options = Options.Create(settings);

            var expectedContent = "Content from S3 bucket stream";
            var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(expectedContent));

            var getObjectResponse = new GetObjectResponse
            {
                ResponseStream = responseStream
            };

            _mockS3Client
                .Setup(s => s.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(getObjectResponse);

            var service = new S3FileStorageService(_mockS3Client.Object, options, _mockS3Logger.Object);

            // Act
            var stream = await service.GetFileStreamAsync("uploads/attachments/data.json");

            // Assert
            Assert.NotNull(stream);
            using var reader = new StreamReader(stream!);
            var content = await reader.ReadToEndAsync();
            Assert.Equal(expectedContent, content);

            _mockS3Client.Verify(s => s.GetObjectAsync(
                It.Is<GetObjectRequest>(r => r.BucketName == "my-s3-bucket" && r.Key == "uploads/attachments/data.json"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task S3FileStorageService_GetFileUrlAsync_GeneratesPresignedUrl()
        {
            // Arrange
            var settings = new StorageSettings
            {
                Provider = "MinIO",
                BucketName = "tickethub-bucket",
                PresignedUrlExpirationMinutes = 30
            };
            var options = Options.Create(settings);

            _mockS3Client
                .Setup(s => s.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
                .Returns("http://minio:9000/tickethub-bucket/uploads/file.png?X-Amz-Signature=xyz");

            var service = new S3FileStorageService(_mockS3Client.Object, options, _mockS3Logger.Object);

            // Act
            var url = await service.GetFileUrlAsync("uploads/file.png");

            // Assert
            Assert.Equal("http://minio:9000/tickethub-bucket/uploads/file.png?X-Amz-Signature=xyz", url);

            _mockS3Client.Verify(s => s.GetPreSignedURL(
                It.Is<GetPreSignedUrlRequest>(r =>
                    r.BucketName == "tickethub-bucket" &&
                    r.Key == "uploads/file.png" &&
                    r.Verb == HttpVerb.GET)), Times.Once);
        }

        [Fact]
        public async Task S3FileStorageService_GetFileUrlAsync_ReturnsCdnUrl_WhenPublicBaseUrlIsConfigured()
        {
            // Arrange
            var settings = new StorageSettings
            {
                Provider = "S3",
                BucketName = "tickethub-media",
                PublicBaseUrl = "https://cdn.tickethub.io"
            };
            var options = Options.Create(settings);

            var service = new S3FileStorageService(_mockS3Client.Object, options, _mockS3Logger.Object);

            // Act
            var url = await service.GetFileUrlAsync("uploads/attachments/image.webp");

            // Assert
            Assert.Equal("https://cdn.tickethub.io/tickethub-media/uploads/attachments/image.webp", url);
        }

        [Fact]
        public async Task S3FileStorageService_DeleteFileAsync_CallsDeleteObjectOnS3()
        {
            // Arrange
            var settings = new StorageSettings
            {
                Provider = "MinIO",
                BucketName = "tickethub-bucket"
            };
            var options = Options.Create(settings);

            _mockS3Client
                .Setup(s => s.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteObjectResponse());

            var service = new S3FileStorageService(_mockS3Client.Object, options, _mockS3Logger.Object);

            // Act
            await service.DeleteFileAsync("uploads/attachments/old_file.docx");

            // Assert
            _mockS3Client.Verify(s => s.DeleteObjectAsync(
                It.Is<DeleteObjectRequest>(r => r.BucketName == "tickethub-bucket" && r.Key == "uploads/attachments/old_file.docx"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void StorageServiceExtensions_RegistersLocalFileStorageService_WhenProviderIsLocal()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Storage:Provider", "Local" },
                { "Storage:LocalPath", "uploads" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton(_mockEnv.Object);
            services.AddLogging();

            // Act
            services.AddStorageServices(configuration);
            var provider = services.BuildServiceProvider();
            var storageService = provider.GetService<IFileStorageService>();

            // Assert
            Assert.NotNull(storageService);
            Assert.IsType<LocalFileStorageService>(storageService);
        }

        [Fact]
        public void StorageServiceExtensions_RegistersS3FileStorageService_WhenProviderIsMinIOOrS3()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Storage:Provider", "MinIO" },
                { "Storage:Endpoint", "http://localhost:9000" },
                { "Storage:BucketName", "test-bucket" },
                { "Storage:AccessKey", "minioadmin" },
                { "Storage:SecretKey", "minioadmin" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton(_mockEnv.Object);
            services.AddLogging();

            // Act
            services.AddStorageServices(configuration);
            var provider = services.BuildServiceProvider();
            var storageService = provider.GetService<IFileStorageService>();
            var s3Client = provider.GetService<IAmazonS3>();

            // Assert
            Assert.NotNull(storageService);
            Assert.IsType<S3FileStorageService>(storageService);
            Assert.NotNull(s3Client);
        }
    }
}

using DocFlowHub.Core.Services.Interfaces;
using DocFlowHub.Infrastructure.Services.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DocFlowHub.Tests.Unit.Services
{
    /// <summary>
    /// Unit tests for OpenAI service
    /// </summary>
    public class OpenAIServiceTests
    {
        private readonly Mock<ILogger<OpenAIService>> _mockLogger;
        private readonly IMemoryCache _memoryCache;
        private readonly Mock<IConfiguration> _mockConfiguration;

        public OpenAIServiceTests()
        {
            _mockLogger = new Mock<ILogger<OpenAIService>>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _mockConfiguration = new Mock<IConfiguration>();
        }

        [Fact]
        public void Constructor_WithValidApiKey_ShouldCreateService()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns("test-api-key");
            _mockConfiguration.Setup(c => c["OpenAI:Model"]).Returns("gpt-3.5-turbo");

            // Act & Assert - Should not throw
            var service = new OpenAIService(_mockConfiguration.Object, _mockLogger.Object, _memoryCache);
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithNullApiKey_ShouldThrowException()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns((string?)null);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                new OpenAIService(_mockConfiguration.Object, _mockLogger.Object, _memoryCache));
        }

        [Fact]
        public void Constructor_WithEmptyApiKey_ShouldThrowException()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns(string.Empty);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                new OpenAIService(_mockConfiguration.Object, _mockLogger.Object, _memoryCache));
        }

        [Fact]
        public void Constructor_WithoutModelConfiguration_ShouldUseDefaultModel()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns("test-api-key");
            _mockConfiguration.Setup(c => c["OpenAI:Model"]).Returns((string?)null);

            // Act - Should not throw and use default model
            var service = new OpenAIService(_mockConfiguration.Object, _mockLogger.Object, _memoryCache);
            
            // Assert
            Assert.NotNull(service);
            // The service should be created successfully with default model
        }

        [Fact]
        public async Task GetHealthAsync_WithValidService_ShouldReturnHealthStatus()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns("test-api-key");
            _mockConfiguration.Setup(c => c["OpenAI:Model"]).Returns("gpt-3.5-turbo");
            
            var service = new OpenAIService(_mockConfiguration.Object, _mockLogger.Object, _memoryCache);

            // Act
            var health = await service.GetHealthAsync();

            // Assert
            Assert.NotNull(health);
            Assert.NotEqual(default(DateTime), health.CheckedAt);
            Assert.NotNull(health.Status);
            // Note: This will likely fail due to network/API key issues in test environment
            // but it validates the method structure
        }

        [Fact]
        public async Task GenerateCompletionAsync_ReturnsFailure_WithInvalidApiKey()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns("invalid-test-key");
            _mockConfiguration.Setup(c => c["OpenAI:Model"]).Returns("gpt-3.5-turbo");
            
            var service = new OpenAIService(_mockConfiguration.Object, _mockLogger.Object, _memoryCache);

            // Act
            var result = await service.GenerateCompletionAsync("Test prompt");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal(0, result.TokensUsed);
            Assert.True(result.ResponseTime >= TimeSpan.Zero);
        }
    }
} 
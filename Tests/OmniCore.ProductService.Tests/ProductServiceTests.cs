using Moq;
using OmniCore.ProductService.Caching;
using OmniCore.ProductService.Models.DTOs;
using OmniCore.ProductService.Models.Entities;
using OmniCore.ProductService.Repositories.Interfaces;

using ProductServiceImpl =
    OmniCore.ProductService.Services.Implementations.ProductService;

namespace OmniCore.ProductService.Tests;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepository;
    private readonly Mock<ICacheService> _cacheService;

    private readonly ProductServiceImpl _productService;

    public ProductServiceTests()
    {
        _productRepository = new Mock<IProductRepository>();
        _cacheService = new Mock<ICacheService>();

        _productService = new ProductServiceImpl(
            _productRepository.Object,
            _cacheService.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesProduct()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Mechanical Keyboard",
            SKU = "key-001",
            Description = "RGB mechanical keyboard",
            Price = 4999,
            IsActive = true
        };

        _productRepository
            .Setup(repository => repository.SkuExistsAsync(
                "KEY-001",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _productService.CreateAsync(request);

        // Assert
        Assert.Equal("Mechanical Keyboard", result.Name);
        Assert.Equal("KEY-001", result.SKU);
        Assert.Equal(4999, result.Price);
        Assert.True(result.IsActive);

        _productRepository.Verify(
            repository => repository.AddAsync(
                It.Is<Product>(product =>
                    product.Name == "Mechanical Keyboard" &&
                    product.SKU == "KEY-001" &&
                    product.Price == 4999 &&
                    product.IsActive),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _productRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheService.Verify(
            cache => cache.RemoveByPrefixAsync(
                "products:list:",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenSkuExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Mechanical Keyboard",
            SKU = "KEY-001",
            Description = "Keyboard",
            Price = 4999,
            IsActive = true
        };

        _productRepository
            .Setup(repository => repository.SkuExistsAsync(
                "KEY-001",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act + Assert
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _productService.CreateAsync(request));

        Assert.Equal(
            "A product with this SKU already exists.",
            exception.Message);

        _productRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<Product>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCached_ReturnsCachedProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();

        var cachedProduct = new ProductResponse
        {
            Id = productId,
            Name = "Cached Keyboard",
            SKU = "KEY-001",
            Price = 4999,
            IsActive = true
        };

        _cacheService
            .Setup(cache => cache.GetAsync<ProductResponse>(
                $"products:id:{productId}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedProduct);

        // Act
        var result = await _productService.GetByIdAsync(productId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(productId, result.Id);
        Assert.Equal("Cached Keyboard", result.Name);

        _productRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotCached_LoadsFromRepositoryAndCaches()
    {
        // Arrange
        var productId = Guid.NewGuid();

        var product = new Product
        {
            Id = productId,
            Name = "Mechanical Keyboard",
            SKU = "KEY-001",
            Description = "Keyboard",
            Price = 4999,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _cacheService
            .Setup(cache => cache.GetAsync<ProductResponse>(
                $"products:id:{productId}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductResponse?)null);

        _productRepository
            .Setup(repository => repository.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _productService.GetByIdAsync(productId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(productId, result.Id);
        Assert.Equal("KEY-001", result.SKU);

        _cacheService.Verify(
            cache => cache.SetAsync(
                $"products:id:{productId}",
                It.Is<ProductResponse>(
                    response => response.Id == productId),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductDoesNotExist_ReturnsNull()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _cacheService
            .Setup(cache => cache.GetAsync<ProductResponse>(
                $"products:id:{productId}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductResponse?)null);

        _productRepository
            .Setup(repository => repository.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _productService.GetByIdAsync(productId);

        // Assert
        Assert.Null(result);

        _cacheService.Verify(
            cache => cache.SetAsync(
                It.IsAny<string>(),
                It.IsAny<ProductResponse>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesProductAndInvalidatesCache()
    {
        // Arrange
        var productId = Guid.NewGuid();

        var existingProduct = new Product
        {
            Id = productId,
            Name = "Old Keyboard",
            SKU = "KEY-001",
            Description = "Old description",
            Price = 3999,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var request = new UpdateProductRequest
        {
            Name = "Updated Keyboard",
            SKU = "KEY-002",
            Description = "Updated description",
            Price = 5999,
            IsActive = true
        };

        _productRepository
            .Setup(repository => repository.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _productRepository
            .Setup(repository => repository.SkuExistsAsync(
                "KEY-002",
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _productService.UpdateAsync(
            productId,
            request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Keyboard", result.Name);
        Assert.Equal("KEY-002", result.SKU);
        Assert.Equal(5999, result.Price);

        _productRepository.Verify(
            repository => repository.Update(
                It.Is<Product>(product =>
                    product.Id == productId &&
                    product.Name == "Updated Keyboard" &&
                    product.SKU == "KEY-002")),
            Times.Once);

        _cacheService.Verify(
            cache => cache.RemoveAsync(
                $"products:id:{productId}",
                It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheService.Verify(
            cache => cache.RemoveByPrefixAsync(
                "products:list:",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductExists_DeletesAndInvalidatesCache()
    {
        // Arrange
        var productId = Guid.NewGuid();

        var product = new Product
        {
            Id = productId,
            Name = "Mechanical Keyboard",
            SKU = "KEY-001",
            Price = 4999,
            IsActive = true
        };

        _productRepository
            .Setup(repository => repository.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _productService.DeleteAsync(productId);

        // Assert
        Assert.True(result);

        _productRepository.Verify(
            repository => repository.Delete(product),
            Times.Once);

        _productRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheService.Verify(
            cache => cache.RemoveAsync(
                $"products:id:{productId}",
                It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheService.Verify(
            cache => cache.RemoveByPrefixAsync(
                "products:list:",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetProductsAsync_ReturnsPagedResult()
    {
        // Arrange
        var query = new ProductQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortDirection = "asc"
        };

        var products = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Keyboard",
                SKU = "KEY-001",
                Price = 4999,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Mouse",
                SKU = "MOU-001",
                Price = 1999,
                IsActive = true
            }
        };

        _cacheService
            .Setup(cache => cache.GetAsync<PagedResult<ProductResponse>>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResult<ProductResponse>?)null);

        _productRepository
            .Setup(repository => repository.GetPagedAsync(
                query,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, 12));

        // Act
        var result = await _productService.GetProductsAsync(query);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(12, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);

        _cacheService.Verify(
            cache => cache.SetAsync(
                It.IsAny<string>(),
                It.Is<PagedResult<ProductResponse>>(
                    page => page.TotalCount == 12),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
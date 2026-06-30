using System.Data;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Implementations;
using Microsoft.EntityFrameworkCore.Storage;

namespace LoafNCatting.Service.Tests;

public class AdminCatalogServiceTests
{
    [Fact]
    public async Task CreateProductAsync_ReturnsNull_WhenCategoryDoesNotExist()
    {
        var products = new FakeProductRepository();
        var categories = new FakeCategoryRepository();
        var service = new ProductService(products, categories);

        var created = await service.CreateProductAsync(new AdminProductRequestDto(
            "Latte",
            "Coffee with milk",
            45000m,
            null,
            10,
            "/Images/Beverages/latte.jpg",
            CategoryId: 99,
            IsAvailable: true));

        Assert.Null(created);
        Assert.Empty(products.AddedProducts);
        Assert.Equal(0, products.SaveCount);
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_UpdatesOnlyStockAndAvailability()
    {
        var category = new Category { CategoryId = 1, Name = "Cà phê" };
        var product = new Product
        {
            ProductId = 7,
            Name = "Latte",
            Description = "Original description",
            Price = 45000m,
            UnitInStock = 5,
            Picture = "/Images/Beverages/latte.jpg",
            CategoryId = category.CategoryId,
            Category = category,
            IsAvailable = true
        };
        var products = new FakeProductRepository(product);
        var service = new ProductService(products, new FakeCategoryRepository(category));

        var updated = await service.UpdateAvailabilityAsync(
            product.ProductId,
            new StaffProductAvailabilityDto(UnitInStock: 0, IsAvailable: false));

        Assert.NotNull(updated);
        Assert.Equal(0, product.UnitInStock);
        Assert.False(product.IsAvailable);
        Assert.Equal("Latte", product.Name);
        Assert.Equal(45000m, product.Price);
        Assert.Equal(1, products.SaveCount);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ReturnsFalse_WhenProductsReferenceCategory()
    {
        var category = new Category { CategoryId = 1, Name = "Cà phê" };
        var product = new Product
        {
            ProductId = 7,
            Name = "Latte",
            Price = 45000m,
            UnitInStock = 5,
            CategoryId = category.CategoryId,
            Category = category,
            IsAvailable = true
        };
        var categories = new FakeCategoryRepository(category);
        var service = new CategoryService(categories, new FakeProductRepository(product));

        var deleted = await service.DeleteCategoryAsync(category.CategoryId);

        Assert.False(deleted);
        Assert.Null(categories.DeletedCategory);
        Assert.Equal(0, categories.SaveCount);
    }

    private sealed class FakeProductRepository(params Product[] products)
        : FakeRepository<Product>(products), IProductRepository
    {
        public List<Product> AddedProducts { get; } = [];

        public Task<IEnumerable<Product>> GetProductsAsync(int? categoryId, string? search)
        {
            IEnumerable<Product> query = Items;
            if (categoryId.HasValue)
            {
                query = query.Where(product => product.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(product => product.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult(query);
        }

        public Task<Product?> GetByIdWithCategoryAsync(int id)
        {
            return Task.FromResult(Items.FirstOrDefault(product => product.ProductId == id));
        }

        public Task<List<Product>> GetByIdsAsync(IEnumerable<int> ids)
        {
            var productIds = ids.ToHashSet();
            return Task.FromResult(Items.Where(product => productIds.Contains(product.ProductId)).ToList());
        }

        public Task<bool> TryReserveStockAsync(IReadOnlyDictionary<int, int> quantitiesByProductId) =>
            Task.FromResult(true);

        public override Task AddAsync(Product entity)
        {
            entity.ProductId = entity.ProductId == 0 ? 100 + AddedProducts.Count : entity.ProductId;
            AddedProducts.Add(entity);
            Items.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCategoryRepository(params Category[] categories)
        : FakeRepository<Category>(categories), ICategoryRepository
    {
        public Category? DeletedCategory { get; private set; }

        public Task<IEnumerable<Category>> GetAllOrderedAsync()
        {
            return Task.FromResult<IEnumerable<Category>>(Items.OrderBy(category => category.Name));
        }

        public override Task<Category?> GetByIdAsync(int id)
        {
            return Task.FromResult(Items.FirstOrDefault(category => category.CategoryId == id));
        }

        public override void Delete(Category entity)
        {
            DeletedCategory = entity;
            Items.Remove(entity);
        }
    }

    private abstract class FakeRepository<T>(IEnumerable<T> items) : IGenericRepository<T> where T : class
    {
        protected readonly List<T> Items = items.ToList();
        public int SaveCount { get; private set; }

        public virtual Task<T?> GetByIdAsync(int id) => Task.FromResult<T?>(null);
        public virtual Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(Items);
        public virtual Task AddAsync(T entity) => Task.CompletedTask;
        public virtual void Update(T entity) { }
        public virtual void Delete(T entity) { }

        public Task<int> SaveChangesAsync()
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync() =>
            Task.FromResult<IDbContextTransaction>(new FakeTransaction());

        public Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel) =>
            Task.FromResult<IDbContextTransaction>(new FakeTransaction());
    }

    private sealed class FakeTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public bool SupportsSavepoints => false;
        public void Commit() { }
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Rollback() { }
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void CreateSavepoint(string name) { }
        public Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void RollbackToSavepoint(string name) { }
        public Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void ReleaseSavepoint(string name) { }
        public Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

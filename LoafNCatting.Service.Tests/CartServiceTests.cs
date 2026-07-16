using System.Data;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Implementations;
using Microsoft.EntityFrameworkCore.Storage;

namespace LoafNCatting.Service.Tests;

public class CartServiceTests
{
    [Fact]
    public async Task AddItemAsync_CreatesUserCartAndClampsQuantityToStock()
    {
        var product = TestProduct(unitInStock: 2);
        var carts = new FakeCartRepository();
        var cartItems = new FakeCartItemRepository();
        var products = new FakeProductRepository(product);
        var service = new CartService(carts, cartItems, products);

        var cart = await service.AddItemAsync(new CartItemRequestDto(7, product.ProductId, 5));

        Assert.NotNull(cart);
        Assert.Equal(7, cart.UserId);
        Assert.Equal(2, cart.TotalQuantity);
        Assert.Equal(90000m, cart.TotalPrice);
        Assert.Single(cart.Items);
        Assert.Equal(product.ProductId, cart.Items[0].Product.ProductId);
        Assert.Equal(2, cart.Items[0].Quantity);
        Assert.Equal(1, carts.AddCount);
        Assert.Equal(1, carts.SaveCount);
    }

    [Fact]
    public async Task UpdateItemAsync_RemovesItemWhenQuantityIsZero()
    {
        var product = TestProduct(unitInStock: 4);
        var existingItem = new CartItem
        {
            CartItemId = 44,
            ProductId = product.ProductId,
            Product = product,
            Quantity = 2,
            UnitPrice = 45000m
        };
        var carts = new FakeCartRepository(new Cart
        {
            CartId = 12,
            UserId = 7,
            CartItems = { existingItem }
        });
        var cartItems = new FakeCartItemRepository();
        var products = new FakeProductRepository(product);
        var service = new CartService(carts, cartItems, products);

        var cart = await service.UpdateItemAsync(new CartItemRequestDto(7, product.ProductId, 0));

        Assert.NotNull(cart);
        Assert.Empty(cart.Items);
        Assert.Same(existingItem, cartItems.DeletedItem);
        Assert.Equal(1, carts.SaveCount);
    }

    private static Product TestProduct(int unitInStock) => new()
    {
        ProductId = 10,
        Name = "Cappuccino",
        Price = 45000m,
        UnitInStock = unitInStock,
        CategoryId = 3,
        Category = new Category { CategoryId = 3, Name = "Drinks" },
        IsAvailable = true
    };

    private sealed class FakeCartRepository(Cart? cart = null) : FakeRepository<Cart>, ICartRepository
    {
        private Cart? _cart = cart;
        public int AddCount { get; private set; }

        public Task<Cart?> GetByUserIdWithItemsAsync(int userId)
        {
            return Task.FromResult(_cart?.UserId == userId ? _cart : null);
        }

        public override Task AddAsync(Cart entity)
        {
            AddCount++;
            entity.CartId = entity.CartId == 0 ? 99 : entity.CartId;
            _cart = entity;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCartItemRepository : FakeRepository<CartItem>, ICartItemRepository
    {
        public CartItem? DeletedItem { get; private set; }

        public override void Delete(CartItem entity)
        {
            DeletedItem = entity;
        }
    }

    private sealed class FakeProductRepository(params Product[] products) : FakeRepository<Product>, IProductRepository
    {
        public Task<IEnumerable<Product>> GetProductsAsync(int? categoryId, string? search)
        {
            return Task.FromResult(products.AsEnumerable());
        }

        public Task<Product?> GetByIdWithCategoryAsync(int id)
        {
            return Task.FromResult(products.FirstOrDefault(product => product.ProductId == id));
        }

        public Task<List<Product>> GetByIdsAsync(IEnumerable<int> ids)
        {
            var productIds = ids.ToHashSet();
            return Task.FromResult(products.Where(product => productIds.Contains(product.ProductId)).ToList());
        }

        public Task<bool> TryReserveStockAsync(IReadOnlyDictionary<int, int> quantitiesByProductId)
        {
            if (quantitiesByProductId.Any(item =>
                products.FirstOrDefault(product => product.ProductId == item.Key) is not { IsAvailable: true } product ||
                product.UnitInStock < item.Value))
            {
                return Task.FromResult(false);
            }

            foreach (var item in quantitiesByProductId)
            {
                var product = products.First(product => product.ProductId == item.Key);
                product.UnitInStock -= item.Value;
                product.IsAvailable = product.UnitInStock > 0 && product.IsAvailable;
            }

            return Task.FromResult(true);
        }
    }

    private abstract class FakeRepository<T> : IGenericRepository<T> where T : class
    {
        public int SaveCount { get; private set; }

        public virtual Task<T?> GetByIdAsync(int id) => Task.FromResult<T?>(null);

        public virtual Task<IEnumerable<T>> GetAllAsync() => Task.FromResult(Enumerable.Empty<T>());

        public virtual Task AddAsync(T entity) => Task.CompletedTask;

        public virtual void Update(T entity) { }

        public virtual void Delete(T entity) { }

        public Task<int> SaveChangesAsync()
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return Task.FromResult<IDbContextTransaction>(new FakeTransaction());
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel)
        {
            return Task.FromResult<IDbContextTransaction>(new FakeTransaction());
        }
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

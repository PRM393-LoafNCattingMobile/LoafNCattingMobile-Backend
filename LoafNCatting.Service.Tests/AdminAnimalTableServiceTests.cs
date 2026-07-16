using System.Data;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Implementations;
using Microsoft.EntityFrameworkCore.Storage;

namespace LoafNCatting.Service.Tests;

public class AdminAnimalTableServiceTests
{
    [Fact]
    public async Task CreateCatAsync_ReturnsNull_WhenStatusDoesNotExist()
    {
        var cats = new FakeCatRepository();
        var service = new CatService(
            cats,
            new FakeCatStatusRepository(),
            new FakeGenderRepository(new Gender { GenderId = 1, GenderName = "Đực" }));

        var created = await service.CreateCatAsync(new AdminCatRequestDto(
            "Mochi",
            Age: 2,
            GenderId: 1,
            Breed: "British Shorthair",
            Picture: null,
            Description: null,
            FriendlinessRating: 5,
            CutenessRating: 5,
            PlayfulnessRating: 4,
            StatusId: 99));

        Assert.Null(created);
        Assert.Empty(cats.AddedCats);
        Assert.Equal(0, cats.SaveCount);
    }

    [Fact]
    public async Task UpdateCatStatusAsync_UpdatesOnlyStatus()
    {
        var working = new CatStatus { StatusId = 1, StatusName = "Đang làm việc" };
        var sick = new CatStatus { StatusId = 2, StatusName = "Bị bệnh" };
        var cat = new Cat
        {
            CatId = 7,
            Name = "Mochi",
            Age = 2,
            Breed = "British Shorthair",
            StatusId = working.StatusId,
            Status = working
        };
        var cats = new FakeCatRepository(cat);
        var service = new CatService(
            cats,
            new FakeCatStatusRepository(working, sick),
            new FakeGenderRepository());

        var updated = await service.UpdateCatStatusAsync(cat.CatId, new StaffCatStatusDto(sick.StatusId));

        Assert.NotNull(updated);
        Assert.Equal("Bị bệnh", updated.StatusName);
        Assert.Equal("Mochi", cat.Name);
        Assert.Equal(2, cat.Age);
        Assert.Equal(1, cats.SaveCount);
    }

    [Fact]
    public async Task CreateTableAsync_ReturnsNull_WhenCapacityIsNotPositive()
    {
        var tables = new FakeRestaurantTableRepository();
        var service = new TableService(
            tables,
            new FakeTableStatusRepository(new TableStatus { TableStatusId = 1, StatusName = "Trống" }));

        var created = await service.CreateTableAsync(new AdminTableRequestDto(
            "A1",
            Capacity: 0,
            Area: "Khu A",
            Description: null,
            TableStatusId: 1));

        Assert.Null(created);
        Assert.Empty(tables.AddedTables);
        Assert.Equal(0, tables.SaveCount);
    }

    [Fact]
    public async Task UpdateTableStatusAsync_UpdatesOnlyStatus()
    {
        var empty = new TableStatus { TableStatusId = 1, StatusName = "Trống" };
        var occupied = new TableStatus { TableStatusId = 3, StatusName = "Đang sử dụng" };
        var table = new RestaurantTable
        {
            TableId = 8,
            TableName = "A1",
            Capacity = 4,
            Area = "Khu A",
            TableStatusId = empty.TableStatusId,
            TableStatus = empty
        };
        var tables = new FakeRestaurantTableRepository(table);
        var service = new TableService(
            tables,
            new FakeTableStatusRepository(empty, occupied));

        var updated = await service.UpdateTableStatusAsync(
            table.TableId,
            new StaffTableStatusDto(occupied.TableStatusId));

        Assert.NotNull(updated);
        Assert.Equal("Đang sử dụng", updated.StatusName);
        Assert.Equal("A1", table.TableName);
        Assert.Equal(4, table.Capacity);
        Assert.Equal(1, tables.SaveCount);
    }

    private sealed class FakeCatRepository(params Cat[] cats)
        : FakeRepository<Cat>(cats), ICatRepository
    {
        public List<Cat> AddedCats { get; } = [];

        public Task<IEnumerable<Cat>> GetCatsAsync(string? search)
        {
            return Task.FromResult<IEnumerable<Cat>>(Items);
        }

        public Task<Cat?> GetByIdWithDetailsAsync(int id)
        {
            return Task.FromResult(Items.FirstOrDefault(cat => cat.CatId == id));
        }

        public override Task AddAsync(Cat entity)
        {
            entity.CatId = entity.CatId == 0 ? 100 + AddedCats.Count : entity.CatId;
            AddedCats.Add(entity);
            Items.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCatStatusRepository(params CatStatus[] statuses)
        : FakeRepository<CatStatus>(statuses), ICatStatusRepository
    {
        public override Task<CatStatus?> GetByIdAsync(int id)
        {
            return Task.FromResult(Items.FirstOrDefault(status => status.StatusId == id));
        }
    }

    private sealed class FakeGenderRepository(params Gender[] genders)
        : FakeRepository<Gender>(genders), IGenderRepository
    {
        public override Task<Gender?> GetByIdAsync(int id)
        {
            return Task.FromResult(Items.FirstOrDefault(gender => gender.GenderId == id));
        }
    }

    private sealed class FakeRestaurantTableRepository(params RestaurantTable[] tables)
        : FakeRepository<RestaurantTable>(tables), IRestaurantTableRepository
    {
        public List<RestaurantTable> AddedTables { get; } = [];

        public Task<IEnumerable<RestaurantTable>> GetAvailableTablesAsync(DateOnly date, TimeOnly time, int guestCount)
        {
            return Task.FromResult<IEnumerable<RestaurantTable>>(Items);
        }

        public Task<IEnumerable<RestaurantTable>> GetTablesAsync()
        {
            return Task.FromResult<IEnumerable<RestaurantTable>>(Items);
        }

        public Task<RestaurantTable?> GetByIdWithStatusAsync(int id)
        {
            return Task.FromResult(Items.FirstOrDefault(table => table.TableId == id));
        }

        public override Task<RestaurantTable?> GetByIdAsync(int id)
        {
            return Task.FromResult(Items.FirstOrDefault(table => table.TableId == id));
        }

        public override Task AddAsync(RestaurantTable entity)
        {
            entity.TableId = entity.TableId == 0 ? 100 + AddedTables.Count : entity.TableId;
            AddedTables.Add(entity);
            Items.Add(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTableStatusRepository(params TableStatus[] statuses)
        : FakeRepository<TableStatus>(statuses), ITableStatusRepository
    {
        public override Task<TableStatus?> GetByIdAsync(int id)
        {
            return Task.FromResult(Items.FirstOrDefault(status => status.TableStatusId == id));
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

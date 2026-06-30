using System.Data;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.Implementations;
using Microsoft.EntityFrameworkCore.Storage;

namespace LoafNCatting.Service.Tests;

public class LookupServiceTests
{
    [Fact]
    public async Task GetAdminLookupsAsync_ReturnsLookupGroupsForAdminAndStaffScreens()
    {
        var service = new LookupService(
            new FakeRoleRepository([
                new Role { RoleId = 1, RoleName = "Admin", Description = "Quản trị viên" },
                new Role { RoleId = 2, RoleName = "Staff", Description = "Nhân viên" },
                new Role { RoleId = 3, RoleName = "Customer", Description = "Khách hàng" }
            ]),
            new FakeOrderStatusRepository([
                new OrderStatus { OrderStatusId = 1, OrderStatusName = "Đang chờ" },
                new OrderStatus { OrderStatusId = 2, OrderStatusName = "Đang chuẩn bị" }
            ]),
            new FakeReservationStatusRepository([
                new ReservationStatus { StatusId = 1, StatusName = "Đang chờ" },
                new ReservationStatus { StatusId = 2, StatusName = "Đã xác nhận" }
            ]),
            new FakeCatStatusRepository([
                new CatStatus { StatusId = 1, StatusName = "Đang làm việc" },
                new CatStatus { StatusId = 2, StatusName = "Bị bệnh" }
            ]),
            new FakeTableStatusRepository([
                new TableStatus { TableStatusId = 1, StatusName = "Trống" },
                new TableStatus { TableStatusId = 2, StatusName = "Đã đặt" }
            ]),
            new FakePaymentMethodRepository([
                new PaymentMethod { MethodId = 1, MethodName = "Tiền mặt" },
                new PaymentMethod { MethodId = 4, MethodName = "Chuyển khoản ngân hàng" }
            ]),
            new FakeGenderRepository([
                new Gender { GenderId = 1, GenderName = "Đực" },
                new Gender { GenderId = 2, GenderName = "Cái" }
            ]),
            new FakeCategoryRepository([
                new Category { CategoryId = 1, Name = "Cà phê" },
                new Category { CategoryId = 1002, Name = "Trà" }
            ]));

        var lookups = await service.GetAdminLookupsAsync();

        Assert.Equal(["Admin", "Staff", "Customer"], lookups.Roles.Select(item => item.Name));
        Assert.Equal(["Đang chờ", "Đang chuẩn bị"], lookups.OrderStatuses.Select(item => item.Name));
        Assert.Equal(["Đang chờ", "Đã xác nhận"], lookups.ReservationStatuses.Select(item => item.Name));
        Assert.Equal(["Đang làm việc", "Bị bệnh"], lookups.CatStatuses.Select(item => item.Name));
        Assert.Equal(["Trống", "Đã đặt"], lookups.TableStatuses.Select(item => item.Name));
        Assert.Equal(["Tiền mặt", "Chuyển khoản ngân hàng"], lookups.PaymentMethods.Select(item => item.Name));
        Assert.Equal(["Đực", "Cái"], lookups.Genders.Select(item => item.Name));
        Assert.Equal(["Cà phê", "Trà"], lookups.Categories.Select(item => item.Name));
    }

    private abstract class FakeRepository<T>(IEnumerable<T> items) : IGenericRepository<T> where T : class
    {
        protected readonly List<T> Items = items.ToList();

        public virtual Task<T?> GetByIdAsync(int id) => Task.FromResult<T?>(null);
        public virtual Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(Items);
        public virtual Task AddAsync(T entity) => Task.CompletedTask;
        public virtual void Update(T entity) { }
        public virtual void Delete(T entity) { }
        public virtual Task<int> SaveChangesAsync() => Task.FromResult(0);
        public virtual Task<IDbContextTransaction> BeginTransactionAsync() => Task.FromResult<IDbContextTransaction>(new FakeTransaction());
        public virtual Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel) => Task.FromResult<IDbContextTransaction>(new FakeTransaction());
    }

    private sealed class FakeRoleRepository(IEnumerable<Role> items)
        : FakeRepository<Role>(items), IRoleRepository
    {
        public Task<Role> GetByNameAsync(string roleName) =>
            Task.FromResult(Items.First(item => item.RoleName == roleName));
    }

    private sealed class FakeOrderStatusRepository(IEnumerable<OrderStatus> items)
        : FakeRepository<OrderStatus>(items), IOrderStatusRepository
    {
        public Task<OrderStatus> GetByNameAsync(string name) =>
            Task.FromResult(Items.First(item => item.OrderStatusName == name));
    }

    private sealed class FakeReservationStatusRepository(IEnumerable<ReservationStatus> items)
        : FakeRepository<ReservationStatus>(items), IReservationStatusRepository
    {
        public Task<ReservationStatus> GetByNameAsync(string name) =>
            Task.FromResult(Items.First(item => item.StatusName == name));
    }

    private sealed class FakeCatStatusRepository(IEnumerable<CatStatus> items)
        : FakeRepository<CatStatus>(items), ICatStatusRepository;

    private sealed class FakeTableStatusRepository(IEnumerable<TableStatus> items)
        : FakeRepository<TableStatus>(items), ITableStatusRepository;

    private sealed class FakePaymentMethodRepository(IEnumerable<PaymentMethod> items)
        : FakeRepository<PaymentMethod>(items), IPaymentMethodRepository
    {
        public Task<PaymentMethod> GetByNameOrDefaultAsync(string name) =>
            Task.FromResult(Items.First());
    }

    private sealed class FakeGenderRepository(IEnumerable<Gender> items)
        : FakeRepository<Gender>(items), IGenderRepository;

    private sealed class FakeCategoryRepository(IEnumerable<Category> items)
        : FakeRepository<Category>(items), ICategoryRepository
    {
        public Task<IEnumerable<Category>> GetAllOrderedAsync() =>
            Task.FromResult<IEnumerable<Category>>(Items);
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

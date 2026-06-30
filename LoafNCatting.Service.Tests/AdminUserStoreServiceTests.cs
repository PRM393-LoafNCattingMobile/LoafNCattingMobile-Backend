using System.Data;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Implementations;
using Microsoft.EntityFrameworkCore.Storage;

namespace LoafNCatting.Service.Tests;

public class AdminUserStoreServiceTests
{
    [Fact]
    public async Task UpdateStoreLocationAsync_UpdatesExistingLocation()
    {
        var location = new StoreLocation
        {
            StoreLocationId = 1,
            StoreName = "Old Name",
            Address = "Old Address",
            Latitude = 10,
            Longitude = 106
        };
        var locations = new FakeStoreLocationRepository(location);
        var service = new StoreLocationService(locations);

        var result = await service.UpdateStoreLocationAsync(
            new AdminStoreLocationRequestDto(
                StoreName: "Loaf'NCatting Cat Cafe",
                Address: "District 7, Ho Chi Minh City",
                PhoneNumber: "0909123456",
                OpeningHours: "08:00 - 21:00",
                Latitude: 10.729,
                Longitude: 106.721));

        Assert.NotNull(result);
        Assert.Equal("Loaf'NCatting Cat Cafe", location.StoreName);
        Assert.Equal("District 7, Ho Chi Minh City", location.Address);
        Assert.Equal(10.729, location.Latitude);
        Assert.Equal(106.721, location.Longitude);
        Assert.Equal(1, locations.SaveCount);
    }

    [Theory]
    [InlineData("", "Address", 10, 106)]
    [InlineData("Store", "", 10, 106)]
    [InlineData("Store", "Address", -91, 106)]
    [InlineData("Store", "Address", 91, 106)]
    [InlineData("Store", "Address", 10, -181)]
    [InlineData("Store", "Address", 10, 181)]
    public async Task UpdateStoreLocationAsync_RejectsInvalidLocation(
        string storeName,
        string address,
        double latitude,
        double longitude)
    {
        var locations = new FakeStoreLocationRepository(new StoreLocation
        {
            StoreLocationId = 1,
            StoreName = "Old Name",
            Address = "Old Address",
            Latitude = 10,
            Longitude = 106
        });
        var service = new StoreLocationService(locations);

        var result = await service.UpdateStoreLocationAsync(
            new AdminStoreLocationRequestDto(
                storeName,
                address,
                PhoneNumber: null,
                OpeningHours: null,
                latitude,
                longitude));

        Assert.Null(result);
        Assert.Equal(0, locations.SaveCount);
    }

    [Theory]
    [InlineData("", "staff@example.com", "0901234567", "password")]
    [InlineData("Staff", "", "0901234567", "password")]
    [InlineData("Staff", "not-an-email", "0901234567", "password")]
    [InlineData("Staff", "staff@example.com", "", "password")]
    [InlineData("Staff", "staff@example.com", "123456789012345678901", "password")]
    [InlineData("Staff", "staff@example.com", "0901234567", "")]
    public async Task CreateStaffAsync_RejectsInvalidRequiredData(
        string name,
        string email,
        string phoneNumber,
        string password)
    {
        var users = new FakeUserRepository();
        var service = new AdminUserService(
            users,
            new FakeRoleRepository([
                new Role { RoleId = 2, RoleName = "Staff" }
            ]),
            new PasswordService());

        var result = await service.CreateStaffAsync(new AdminCreateStaffDto(
            name,
            email,
            phoneNumber,
            password,
            Address: null,
            AvatarUrl: null));

        Assert.Null(result);
        Assert.Null(users.AddedUser);
        Assert.Equal(0, users.SaveCount);
    }

    [Fact]
    public async Task UpdateActiveAsync_DeactivatesStaffAndSetsUpdatedAt()
    {
        var staffRole = new Role { RoleId = 2, RoleName = "Staff" };
        var user = new User
        {
            UserId = 10,
            Name = "Lan Anh",
            Email = "lan.anh@example.com",
            PhoneNumber = "0901234567",
            Password = "hash",
            RoleId = staffRole.RoleId,
            Role = staffRole,
            IsActive = true
        };
        var users = new FakeUserRepository { Users = [user] };
        var service = new AdminUserService(
            users,
            new FakeRoleRepository([staffRole]),
            new PasswordService());
        var startedAt = DateTime.UtcNow;

        var result = await service.UpdateActiveAsync(
            user.UserId,
            new AdminUserActiveDto(IsActive: false));

        Assert.NotNull(result);
        Assert.False(user.IsActive);
        Assert.NotNull(user.UpdatedAt);
        Assert.True(user.UpdatedAt >= startedAt);
        Assert.Equal(1, users.SaveCount);
    }

    [Fact]
    public async Task UpdateActiveAsync_RejectsAdminAccount()
    {
        var adminRole = new Role { RoleId = 1, RoleName = "Admin" };
        var user = new User
        {
            UserId = 1,
            Name = "Admin",
            Email = "admin@example.com",
            PhoneNumber = "0900000000",
            Password = "hash",
            RoleId = adminRole.RoleId,
            Role = adminRole,
            IsActive = true
        };
        var users = new FakeUserRepository { Users = [user] };
        var service = new AdminUserService(
            users,
            new FakeRoleRepository([adminRole]),
            new PasswordService());

        var result = await service.UpdateActiveAsync(
            user.UserId,
            new AdminUserActiveDto(IsActive: false));

        Assert.Null(result);
        Assert.True(user.IsActive);
        Assert.Equal(0, users.SaveCount);
    }

    [Fact]
    public async Task UpdateRoleAsync_ChangesStaffToCustomerAndSetsUpdatedAt()
    {
        var staffRole = new Role { RoleId = 2, RoleName = "Staff" };
        var customerRole = new Role { RoleId = 3, RoleName = "Customer" };
        var user = new User
        {
            UserId = 10,
            Name = "Lan Anh",
            Email = "lan.anh@example.com",
            PhoneNumber = "0901234567",
            Password = "hash",
            RoleId = staffRole.RoleId,
            Role = staffRole,
            IsActive = true
        };
        var users = new FakeUserRepository { Users = [user] };
        var service = new AdminUserService(
            users,
            new FakeRoleRepository([staffRole, customerRole]),
            new PasswordService());
        var startedAt = DateTime.UtcNow;

        var result = await service.UpdateRoleAsync(
            user.UserId,
            new AdminUserRoleDto(RoleId: customerRole.RoleId));

        Assert.NotNull(result);
        Assert.Equal(customerRole.RoleId, user.RoleId);
        Assert.Equal("Customer", user.Role.RoleName);
        Assert.NotNull(user.UpdatedAt);
        Assert.True(user.UpdatedAt >= startedAt);
        Assert.Equal(1, users.SaveCount);
    }

    [Theory]
    [InlineData("Admin", "Customer")]
    [InlineData("Staff", "Admin")]
    public async Task UpdateRoleAsync_RejectsAdminSourceOrTarget(
        string currentRoleName,
        string targetRoleName)
    {
        var currentRole = new Role { RoleId = 1, RoleName = currentRoleName };
        var targetRole = new Role { RoleId = 9, RoleName = targetRoleName };
        var user = new User
        {
            UserId = 10,
            Name = "Protected User",
            Email = "protected@example.com",
            PhoneNumber = "0901234567",
            Password = "hash",
            RoleId = currentRole.RoleId,
            Role = currentRole,
            IsActive = true
        };
        var users = new FakeUserRepository { Users = [user] };
        var service = new AdminUserService(
            users,
            new FakeRoleRepository([currentRole, targetRole]),
            new PasswordService());

        var result = await service.UpdateRoleAsync(
            user.UserId,
            new AdminUserRoleDto(RoleId: targetRole.RoleId));

        Assert.Null(result);
        Assert.Equal(0, users.SaveCount);
    }

    [Fact]
    public async Task GetUsersAsync_PassesFiltersAndMapsSafeUserData()
    {
        var role = new Role { RoleId = 2, RoleName = "Staff" };
        var users = new FakeUserRepository
        {
            Users =
            [
                new User
                {
                    UserId = 10,
                    Name = "Lan Anh",
                    Email = "lan.anh@example.com",
                    PhoneNumber = "0901234567",
                    Password = "must-not-be-returned",
                    RoleId = role.RoleId,
                    Role = role,
                    IsActive = true,
                    IsEmailVerified = true
                }
            ]
        };
        var service = new AdminUserService(
            users,
            new FakeRoleRepository([role]),
            new PasswordService());

        var result = await service.GetUsersAsync(
            roleId: 2,
            search: "lan",
            active: true);

        var item = Assert.Single(result);
        Assert.Equal("Lan Anh", item.Name);
        Assert.Equal("Staff", item.RoleName);
        Assert.Equal(2, users.LastRoleId);
        Assert.Equal("lan", users.LastSearch);
        Assert.True(users.LastActive);
    }

    [Fact]
    public async Task CreateStaffAsync_RejectsDuplicateEmailOrPhone()
    {
        var users = new FakeUserRepository { DuplicateExists = true };
        var service = new AdminUserService(
            users,
            new FakeRoleRepository([
                new Role { RoleId = 2, RoleName = "Staff" }
            ]),
            new PasswordService());

        var result = await service.CreateStaffAsync(new AdminCreateStaffDto(
            Name: "Lan Anh",
            Email: "lan.anh@example.com",
            PhoneNumber: "0901234567",
            Password: "Staff@123",
            Address: null,
            AvatarUrl: null));

        Assert.Null(result);
        Assert.Null(users.AddedUser);
        Assert.Equal(0, users.SaveCount);
    }

    [Fact]
    public async Task CreateStaffAsync_CreatesVerifiedStaffWithHashedPassword()
    {
        var users = new FakeUserRepository();
        var passwordService = new PasswordService();
        var service = new AdminUserService(
            users,
            new FakeRoleRepository([
                new Role { RoleId = 2, RoleName = "Staff" }
            ]),
            passwordService);

        var result = await service.CreateStaffAsync(new AdminCreateStaffDto(
            Name: "  Lan Anh  ",
            Email: "  LAN.ANH@EXAMPLE.COM  ",
            PhoneNumber: " 0901234567 ",
            Password: "Staff@123",
            Address: "District 7",
            AvatarUrl: null));

        Assert.NotNull(result);
        var created = Assert.IsType<User>(users.AddedUser);
        Assert.Equal("Lan Anh", created.Name);
        Assert.Equal("lan.anh@example.com", created.Email);
        Assert.Equal("0901234567", created.PhoneNumber);
        Assert.Equal(2, created.RoleId);
        Assert.Equal("Staff", created.Role.RoleName);
        Assert.True(created.IsActive);
        Assert.True(created.IsEmailVerified);
        Assert.NotEqual("Staff@123", created.Password);
        Assert.True(passwordService.VerifyPassword("Staff@123", created.Password));
        Assert.Equal("Staff", result.RoleName);
        Assert.Equal(1, users.SaveCount);
    }

    private sealed class FakeUserRepository : FakeRepository<User>, IUserRepository
    {
        public User? AddedUser { get; private set; }
        public bool DuplicateExists { get; init; }
        public List<User> Users { get; init; } = [];
        public int? LastRoleId { get; private set; }
        public string? LastSearch { get; private set; }
        public bool? LastActive { get; private set; }

        public override Task AddAsync(User entity)
        {
            entity.UserId = 10;
            AddedUser = entity;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByEmailOrPhoneAsync(string email, string phoneNumber) =>
            Task.FromResult(DuplicateExists);

        public Task<User?> GetByEmailAsync(string email) => Task.FromResult<User?>(null);

        public Task<User?> GetByLoginAsync(string login, string phoneNumber) =>
            Task.FromResult<User?>(null);

        public Task<User?> GetFirstStaffAsync() => Task.FromResult<User?>(null);

        public Task<User?> GetByIdWithRoleAsync(int id) =>
            Task.FromResult(Users.FirstOrDefault(user => user.UserId == id));

        public Task<IEnumerable<User>> GetAdminUsersAsync(
            int? roleId,
            string? search,
            bool? active)
        {
            LastRoleId = roleId;
            LastSearch = search;
            LastActive = active;
            return Task.FromResult<IEnumerable<User>>(Users);
        }
    }

    private sealed class FakeRoleRepository(IEnumerable<Role> roles)
        : FakeRepository<Role>, IRoleRepository
    {
        private readonly List<Role> _roles = roles.ToList();

        public override Task<Role?> GetByIdAsync(int id) =>
            Task.FromResult(_roles.FirstOrDefault(role => role.RoleId == id));

        public Task<Role> GetByNameAsync(string roleName) =>
            Task.FromResult(_roles.First(role => role.RoleName == roleName));
    }

    private sealed class FakeStoreLocationRepository(StoreLocation? location)
        : FakeRepository<StoreLocation>, IStoreLocationRepository
    {
        public Task<StoreLocation?> GetFirstAsync() => Task.FromResult(location);
    }

    private abstract class FakeRepository<T> : IGenericRepository<T> where T : class
    {
        public int SaveCount { get; private set; }

        public virtual Task<T?> GetByIdAsync(int id) => Task.FromResult<T?>(null);

        public virtual Task<IEnumerable<T>> GetAllAsync() =>
            Task.FromResult<IEnumerable<T>>([]);

        public virtual Task AddAsync(T entity) => Task.CompletedTask;

        public virtual void Update(T entity) { }

        public virtual void Delete(T entity) { }

        public Task<int> SaveChangesAsync()
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync() =>
            throw new NotSupportedException();

        public Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel) =>
            throw new NotSupportedException();
    }
}

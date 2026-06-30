using LoafNCatting.Api.Controllers;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Service.Tests;

public class AdminUserStoreControllerTests
{
    [Fact]
    public async Task AdminStoreLocation_Update_ReturnsForbidden_ForStaff()
    {
        var controller = CreateStoreController("Staff");

        var result = await controller.UpdateStoreLocation(SampleLocationRequest());

        var failure = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, failure.StatusCode);
    }

    [Fact]
    public async Task AdminStoreLocation_Update_AllowsAdmin()
    {
        var controller = CreateStoreController("Admin");

        var result = await controller.UpdateStoreLocation(SampleLocationRequest());

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task AdminUsers_GetUsers_ReturnsForbidden_ForStaff()
    {
        var controller = CreateUsersController("Staff");

        var result = await controller.GetUsers(
            role: null,
            search: null,
            active: null);

        var failure = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, failure.StatusCode);
    }

    [Fact]
    public async Task AdminUsers_CreateStaff_AllowsAdmin()
    {
        var controller = CreateUsersController("Admin");

        var result = await controller.CreateStaff(new AdminCreateStaffDto(
            "Lan Anh",
            "lan.anh@example.com",
            "0901234567",
            "Staff@123",
            Address: null,
            AvatarUrl: null));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var user = Assert.IsType<AdminUserDto>(ok.Value);
        Assert.Equal("Staff", user.RoleName);
    }

    [Fact]
    public async Task AdminUsers_UpdateRole_AllowsAdmin()
    {
        var controller = CreateUsersController("Admin");

        var result = await controller.UpdateRole(
            id: 10,
            new AdminUserRoleDto(RoleId: 3));

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task AdminUsers_UpdateActive_AllowsAdmin()
    {
        var controller = CreateUsersController("Admin");

        var result = await controller.UpdateActive(
            id: 10,
            new AdminUserActiveDto(IsActive: false));

        Assert.IsType<OkObjectResult>(result.Result);
    }

    private static AdminUsersController CreateUsersController(string roleName)
    {
        var controller = new AdminUsersController(
            new FakeAdminUserService(),
            new FakeSessionTokenService(
                new UserSession(1, roleName, DateTime.UtcNow.AddHours(1))))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Headers.Authorization = "Bearer test-token";
        return controller;
    }

    private static AdminStoreLocationController CreateStoreController(string roleName)
    {
        var controller = new AdminStoreLocationController(
            new FakeStoreLocationService(),
            new FakeSessionTokenService(
                new UserSession(1, roleName, DateTime.UtcNow.AddHours(1))))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Headers.Authorization = "Bearer test-token";
        return controller;
    }

    private static AdminStoreLocationRequestDto SampleLocationRequest() =>
        new(
            "Loaf'NCatting Cat Cafe",
            "District 7, Ho Chi Minh City",
            "0909123456",
            "08:00 - 21:00",
            10.729,
            106.721);

    private sealed class FakeAdminUserService : IAdminUserService
    {
        public Task<List<AdminUserDto>> GetUsersAsync(
            int? roleId,
            string? search,
            bool? active) => Task.FromResult<List<AdminUserDto>>([]);

        public Task<AdminUserDto?> CreateStaffAsync(AdminCreateStaffDto request) =>
            Task.FromResult<AdminUserDto?>(SampleUser(roleId: 2, roleName: "Staff"));

        public Task<AdminUserDto?> UpdateRoleAsync(
            int id,
            AdminUserRoleDto request) =>
            Task.FromResult<AdminUserDto?>(SampleUser(request.RoleId, "Customer"));

        public Task<AdminUserDto?> UpdateActiveAsync(
            int id,
            AdminUserActiveDto request) =>
            Task.FromResult<AdminUserDto?>(SampleUser(roleId: 2, roleName: "Staff") with
            {
                IsActive = request.IsActive
            });

        private static AdminUserDto SampleUser(int roleId, string roleName) =>
            new(
                UserId: 10,
                Name: "Lan Anh",
                Email: "lan.anh@example.com",
                PhoneNumber: "0901234567",
                Address: null,
                AvatarUrl: null,
                RoleId: roleId,
                RoleName: roleName,
                IsActive: true,
                IsEmailVerified: true,
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: null);
    }

    private sealed class FakeStoreLocationService : IStoreLocationService
    {
        public Task<StoreLocationDto?> GetStoreLocationAsync() =>
            Task.FromResult<StoreLocationDto?>(null);

        public Task<StoreLocationDto?> UpdateStoreLocationAsync(
            AdminStoreLocationRequestDto request) =>
            Task.FromResult<StoreLocationDto?>(new StoreLocationDto(
                request.StoreName,
                request.Address,
                request.PhoneNumber,
                request.OpeningHours,
                request.Latitude,
                request.Longitude));
    }

    private sealed class FakeSessionTokenService(UserSession? session) : ISessionTokenService
    {
        public string IssueToken(User user) => "test-token";

        public UserSession? GetSession(string token) =>
            token == "test-token" ? session : null;

        public void Revoke(string token) { }
    }
}

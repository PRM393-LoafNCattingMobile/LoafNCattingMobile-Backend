using LoafNCatting.Api.Controllers;
using LoafNCatting.Data.Models;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Service.Tests;

public class AdminAnimalTableControllerTests
{
    [Fact]
    public async Task AdminCats_CreateCat_ReturnsForbidden_ForStaff()
    {
        var controller = CreateAdminCatsController("Staff");

        var result = await controller.CreateCat(SampleCatRequest());

        var failure = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, failure.StatusCode);
    }

    [Fact]
    public async Task StaffCats_UpdateStatus_AllowsStaff()
    {
        var controller = CreateStaffCatsController("Staff");

        var result = await controller.UpdateStatus(7, new StaffCatStatusDto(StatusId: 2));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var cat = Assert.IsType<CatDto>(ok.Value);
        Assert.Equal("Resting", cat.StatusName);
    }

    [Fact]
    public async Task AdminTables_CreateTable_ReturnsOk_ForAdmin()
    {
        var controller = CreateAdminTablesController("Admin");

        var result = await controller.CreateTable(new AdminTableRequestDto(
            "A1",
            Capacity: 4,
            Area: "Main",
            Description: null,
            TableStatusId: 1));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var table = Assert.IsType<TableDto>(ok.Value);
        Assert.Equal("A1", table.TableName);
    }

    [Fact]
    public async Task StaffTables_UpdateStatus_AllowsStaff()
    {
        var controller = CreateStaffTablesController("Staff");

        var result = await controller.UpdateStatus(8, new StaffTableStatusDto(TableStatusId: 3));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var table = Assert.IsType<TableDto>(ok.Value);
        Assert.Equal("Occupied", table.StatusName);
    }

    private static AdminCatsController CreateAdminCatsController(string roleName)
    {
        var controller = new AdminCatsController(
            new FakeCatService(),
            new FakeSessionTokenService(new UserSession(7, roleName, DateTime.UtcNow.AddHours(1))))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Headers.Authorization = "Bearer test-token";
        return controller;
    }

    private static StaffCatsController CreateStaffCatsController(string roleName)
    {
        var controller = new StaffCatsController(
            new FakeCatService(),
            new FakeSessionTokenService(new UserSession(7, roleName, DateTime.UtcNow.AddHours(1))))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Headers.Authorization = "Bearer test-token";
        return controller;
    }

    private static AdminTablesController CreateAdminTablesController(string roleName)
    {
        var controller = new AdminTablesController(
            new FakeTableService(),
            new FakeSessionTokenService(new UserSession(7, roleName, DateTime.UtcNow.AddHours(1))))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Headers.Authorization = "Bearer test-token";
        return controller;
    }

    private static StaffTablesController CreateStaffTablesController(string roleName)
    {
        var controller = new StaffTablesController(
            new FakeTableService(),
            new FakeSessionTokenService(new UserSession(7, roleName, DateTime.UtcNow.AddHours(1))))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Headers.Authorization = "Bearer test-token";
        return controller;
    }

    private static AdminCatRequestDto SampleCatRequest() =>
        new(
            "Mochi",
            Age: 2,
            GenderId: 1,
            Breed: "British Shorthair",
            Picture: null,
            Description: null,
            FriendlinessRating: 5,
            CutenessRating: 5,
            PlayfulnessRating: 4,
            StatusId: 1);

    private sealed class FakeCatService : ICatService
    {
        public Task<List<CatDto>> GetCatsAsync(string? search) => Task.FromResult<List<CatDto>>([]);

        public Task<CatDto?> GetCatAsync(int id) => Task.FromResult<CatDto?>(SampleCat(id, "Working"));

        public Task<CatDto?> CreateCatAsync(AdminCatRequestDto request) =>
            Task.FromResult<CatDto?>(SampleCat(7, "Working"));

        public Task<CatDto?> UpdateCatAsync(int id, AdminCatRequestDto request) =>
            Task.FromResult<CatDto?>(SampleCat(id, "Working"));

        public Task<CatDto?> UpdateCatStatusAsync(int id, StaffCatStatusDto request) =>
            Task.FromResult<CatDto?>(SampleCat(id, "Resting"));

        public Task<bool> DeleteCatAsync(int id) => Task.FromResult(true);

        private static CatDto SampleCat(int id, string statusName) =>
            new(id, "Mochi", 2, "Male", "British Shorthair", null, null, 5, 5, 4, statusName);
    }

    private sealed class FakeTableService : ITableService
    {
        public Task<List<TableDto>> GetAvailableTablesAsync(DateOnly date, TimeOnly time, int guestCount) =>
            Task.FromResult<List<TableDto>>([]);

        public Task<List<TableDto>> GetTablesAsync() => Task.FromResult<List<TableDto>>([]);

        public Task<TableDto?> GetTableAsync(int id) => Task.FromResult<TableDto?>(SampleTable(id, "Empty"));

        public Task<TableDto?> CreateTableAsync(AdminTableRequestDto request) =>
            Task.FromResult<TableDto?>(new TableDto(8, request.TableName, request.Capacity, request.Area, request.Description, "Empty"));

        public Task<TableDto?> UpdateTableAsync(int id, AdminTableRequestDto request) =>
            Task.FromResult<TableDto?>(new TableDto(id, request.TableName, request.Capacity, request.Area, request.Description, "Empty"));

        public Task<TableDto?> UpdateTableStatusAsync(int id, StaffTableStatusDto request) =>
            Task.FromResult<TableDto?>(SampleTable(id, "Occupied"));

        public Task<bool> DeleteTableAsync(int id) => Task.FromResult(true);

        private static TableDto SampleTable(int id, string statusName) =>
            new(id, "A1", 4, "Main", null, statusName);
    }

    private sealed class FakeSessionTokenService(UserSession? session) : ISessionTokenService
    {
        public string IssueToken(User user) => "test-token";

        public UserSession? GetSession(string token) => token == "test-token" ? session : null;

        public void Revoke(string token) { }
    }
}

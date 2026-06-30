using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Mappers;
using LoafNCatting.Data.Interfaces;
using LoafNCatting.Data.Models;

namespace LoafNCatting.Service.Implementations;

public class CatService(
    ICatRepository cats,
    ICatStatusRepository catStatuses,
    IGenderRepository genders) : ICatService
{
    public async Task<List<CatDto>> GetCatsAsync(string? search)
    {
        var items = await cats.GetCatsAsync(search);
        return items.Select(CafeDtoMapper.ToCatDto).ToList();
    }

    public async Task<CatDto?> GetCatAsync(int id)
    {
        var cat = await cats.GetByIdWithDetailsAsync(id);
        return cat is null ? null : CafeDtoMapper.ToCatDto(cat);
    }

    public async Task<CatDto?> CreateCatAsync(AdminCatRequestDto request)
    {
        var status = await catStatuses.GetByIdAsync(request.StatusId);
        var gender = request.GenderId.HasValue ? await genders.GetByIdAsync(request.GenderId.Value) : null;
        if (!IsValidRequest(request) || status is null || (request.GenderId.HasValue && gender is null))
        {
            return null;
        }

        var cat = new Cat
        {
            Name = request.Name.Trim(),
            Age = request.Age,
            GenderId = gender?.GenderId,
            Gender = gender,
            Breed = NormalizeOptional(request.Breed),
            Picture = NormalizeOptional(request.Picture),
            Description = NormalizeOptional(request.Description),
            FriendlinessRating = request.FriendlinessRating,
            CutenessRating = request.CutenessRating,
            PlayfulnessRating = request.PlayfulnessRating,
            StatusId = status.StatusId,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        await cats.AddAsync(cat);
        await cats.SaveChangesAsync();
        return CafeDtoMapper.ToCatDto(cat);
    }

    public async Task<CatDto?> UpdateCatAsync(int id, AdminCatRequestDto request)
    {
        var cat = await cats.GetByIdWithDetailsAsync(id);
        var status = await catStatuses.GetByIdAsync(request.StatusId);
        var gender = request.GenderId.HasValue ? await genders.GetByIdAsync(request.GenderId.Value) : null;
        if (cat is null || !IsValidRequest(request) || status is null || (request.GenderId.HasValue && gender is null))
        {
            return null;
        }

        cat.Name = request.Name.Trim();
        cat.Age = request.Age;
        cat.GenderId = gender?.GenderId;
        cat.Gender = gender;
        cat.Breed = NormalizeOptional(request.Breed);
        cat.Picture = NormalizeOptional(request.Picture);
        cat.Description = NormalizeOptional(request.Description);
        cat.FriendlinessRating = request.FriendlinessRating;
        cat.CutenessRating = request.CutenessRating;
        cat.PlayfulnessRating = request.PlayfulnessRating;
        cat.StatusId = status.StatusId;
        cat.Status = status;
        cat.UpdatedAt = DateTime.UtcNow;

        cats.Update(cat);
        await cats.SaveChangesAsync();
        return CafeDtoMapper.ToCatDto(cat);
    }

    public async Task<CatDto?> UpdateCatStatusAsync(int id, StaffCatStatusDto request)
    {
        var cat = await cats.GetByIdWithDetailsAsync(id);
        var status = await catStatuses.GetByIdAsync(request.StatusId);
        if (cat is null || status is null)
        {
            return null;
        }

        cat.StatusId = status.StatusId;
        cat.Status = status;
        cat.UpdatedAt = DateTime.UtcNow;

        cats.Update(cat);
        await cats.SaveChangesAsync();
        return CafeDtoMapper.ToCatDto(cat);
    }

    public async Task<bool> DeleteCatAsync(int id)
    {
        var cat = await cats.GetByIdAsync(id);
        if (cat is null)
        {
            return false;
        }

        cats.Delete(cat);
        await cats.SaveChangesAsync();
        return true;
    }

    private static bool IsValidRequest(AdminCatRequestDto request)
    {
        return !string.IsNullOrWhiteSpace(request.Name) &&
            (!request.Age.HasValue || request.Age.Value >= 0) &&
            IsRatingValid(request.FriendlinessRating) &&
            IsRatingValid(request.CutenessRating) &&
            IsRatingValid(request.PlayfulnessRating);
    }

    private static bool IsRatingValid(int? rating)
    {
        return !rating.HasValue || rating.Value is >= 1 and <= 5;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}




using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public interface ILookupService
{
    Task<AdminLookupsDto> GetAdminLookupsAsync();
}

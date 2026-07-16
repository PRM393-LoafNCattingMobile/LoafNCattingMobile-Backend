using LoafNCatting.Data.Models;
using LoafNCatting.Service.Mappers;

namespace LoafNCatting.Service.Tests;

public class ProductMapperTests
{
    [Fact]
    public void ToProductDto_KeepsRawAvailabilityAndAddsOrderability()
    {
        var product = new Product
        {
            ProductId = 10,
            Name = "Cappuccino",
            Price = 45000m,
            UnitInStock = 0,
            CategoryId = 3,
            Category = new Category { CategoryId = 3, Name = "Drinks" },
            IsAvailable = true
        };

        var dto = CafeDtoMapper.ToProductDto(product);

        Assert.True(dto.IsAvailable);
        Assert.False(dto.CanOrder);
    }
}

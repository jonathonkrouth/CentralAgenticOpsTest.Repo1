using FoodProducts.Application.Contracts;

namespace FoodProducts.Application.Services;

public interface IFoodProductService
{
    Task<IReadOnlyList<FoodProductDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<FoodProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<FoodProductDto> CreateAsync(FoodProductRequest request, CancellationToken cancellationToken = default);

    Task<FoodProductDto?> UpdateAsync(int id, FoodProductRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

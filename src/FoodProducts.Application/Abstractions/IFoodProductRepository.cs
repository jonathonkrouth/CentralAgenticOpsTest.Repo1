using FoodProducts.Application.Domain;

namespace FoodProducts.Application.Abstractions;

public interface IFoodProductRepository
{
    Task<IReadOnlyList<FoodProduct>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<FoodProduct?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(FoodProduct foodProduct, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(FoodProduct foodProduct, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

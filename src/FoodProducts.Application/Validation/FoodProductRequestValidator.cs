using FoodProducts.Application.Contracts;
using FoodProducts.Application.Exceptions;

namespace FoodProducts.Application.Validation;

internal static class FoodProductRequestValidator
{
    private const int NameMaxLength = 200;
    private const int DescriptionMaxLength = 1000;
    private const int CategoryMaxLength = 100;

    public static void ValidateAndThrow(FoodProductRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = ["Name is required."];
        }
        else if (request.Name.Trim().Length > NameMaxLength)
        {
            errors[nameof(request.Name)] = [$"Name must be {NameMaxLength} characters or fewer."];
        }

        if (request.Description?.Trim().Length > DescriptionMaxLength)
        {
            errors[nameof(request.Description)] = [$"Description must be {DescriptionMaxLength} characters or fewer."];
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            errors[nameof(request.Category)] = ["Category is required."];
        }
        else if (request.Category.Trim().Length > CategoryMaxLength)
        {
            errors[nameof(request.Category)] = [$"Category must be {CategoryMaxLength} characters or fewer."];
        }

        if (request.Price < 0)
        {
            errors[nameof(request.Price)] = ["Price cannot be negative."];
        }
        else if (decimal.Round(request.Price, 2) != request.Price)
        {
            errors[nameof(request.Price)] = ["Price cannot have more than two decimal places."];
        }

        if (request.CaloriesPerServing < 0)
        {
            errors[nameof(request.CaloriesPerServing)] = ["Calories per serving cannot be negative."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}

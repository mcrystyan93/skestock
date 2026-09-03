using System.ComponentModel.DataAnnotations;

namespace skestock.Application.Common.Models.Options;

public class NutrientApiSettings
{
    [Required]
    public string BaseUrl { get; init; } = string.Empty;
    [Required]
    public string ApiKey { get; init; } = string.Empty;
}

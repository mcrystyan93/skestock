using System.ComponentModel.DataAnnotations;

namespace skestock.Application.Common.Models.Options;

public class GeminiApiSettings
{
    [Required]
    public string Model { get; init; } = string.Empty;
    [Required] 
    public string ApiKey { get; init; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Settings.Dtos;

public record UpdatePlatformSettingDto
{
    [Required]
    public required string SettingValue { get; init; }
}

public record CreatePlatformSettingDto
{
    [Required]
    [MaxLength(100)]
    public required string SettingKey { get; init; }

    [Required]
    [MaxLength(255)]
    public required string SettingName { get; init; }

    public string? SettingValue { get; init; }

    [MaxLength(50)]
    public string? ValueType { get; init; } = "string";

    [MaxLength(500)]
    public string? Description { get; init; }

    [MaxLength(50)]
    public string? Category { get; init; }

    public bool IsPublic { get; init; } = false;

    public bool IsEditable { get; init; } = true;
}

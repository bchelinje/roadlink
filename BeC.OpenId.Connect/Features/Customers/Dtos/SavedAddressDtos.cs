namespace BeC.OpenId.Connect.Features.Customers.Dtos;

public class SavedAddress
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty; // "Home", "Work", "Warehouse", etc.
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? County { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "UK";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? SpecialInstructions { get; set; }
    public bool IsDefault { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSavedAddressDto
{
    public required string Label { get; set; }
    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public required string City { get; set; }
    public string? County { get; set; }
    public required string PostalCode { get; set; }
    public string Country { get; set; } = "UK";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? SpecialInstructions { get; set; }
    public bool IsDefault { get; set; } = false;
}

public class UpdateSavedAddressDto
{
    public string? Label { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? County { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? SpecialInstructions { get; set; }
}

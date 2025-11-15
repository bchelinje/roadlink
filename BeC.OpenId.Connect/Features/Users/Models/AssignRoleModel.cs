using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Users.Models;

/// <summary>
/// Model for assigning role to user
/// </summary>
public class AssignRoleModel
{
    [Required]
    public required string RoleName { get; set; }
}

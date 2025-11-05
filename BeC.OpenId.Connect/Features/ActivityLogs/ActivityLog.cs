// Models/ActivityLog.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BeC.OpenId.Connect.Features.Users.Dtos;

namespace BeC.OpenId.Connect.Features.ActivityLogs
{
    [Table("ActivityLogs")]
    public class ActivityLog
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string UserEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        [MaxLength(450)]
        public string? EntityId { get; set; }

        [MaxLength(200)]
        public string? EntityName { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// JSON string for additional metadata
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? MetadataJson { get; set; }

        [NotMapped]
        public Dictionary<string, object>? Metadata
        {
            get => string.IsNullOrEmpty(MetadataJson)
                ? null
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(MetadataJson);
            set => MetadataJson = value == null
                ? null
                : System.Text.Json.JsonSerializer.Serialize(value);
        }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = "INFO";

        // Navigation property (optional)
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }

    // Enums as constants for consistency
    public static class ActivityActions
    {
        // User actions
        public const string UserCreated = "USER_CREATED";
        public const string UserUpdated = "USER_UPDATED";
        public const string UserDeleted = "USER_DELETED";
        public const string UserLocked = "USER_LOCKED";
        public const string UserUnlocked = "USER_UNLOCKED";
        public const string UserPasswordChanged = "USER_PASSWORD_CHANGED";
        public const string UserEmailVerified = "USER_EMAIL_VERIFIED";

        // Authentication actions
        public const string LoginSuccess = "LOGIN_SUCCESS";
        public const string LoginFailed = "LOGIN_FAILED";
        public const string Logout = "LOGOUT";
        public const string PasswordResetRequested = "PASSWORD_RESET_REQUESTED";
        public const string PasswordResetCompleted = "PASSWORD_RESET_COMPLETED";

        // Role actions
        public const string RoleCreated = "ROLE_CREATED";
        public const string RoleUpdated = "ROLE_UPDATED";
        public const string RoleDeleted = "ROLE_DELETED";
        public const string RoleAssigned = "ROLE_ASSIGNED";
        public const string RoleRemoved = "ROLE_REMOVED";

        // Move actions
        public const string MoveCreated = "MOVE_CREATED";
        public const string MoveUpdated = "MOVE_UPDATED";
        public const string MoveCancelled = "MOVE_CANCELLED";
        public const string MoveCompleted = "MOVE_COMPLETED";

        // Driver actions
        public const string DriverAssigned = "DRIVER_ASSIGNED";
        public const string DriverUnassigned = "DRIVER_UNASSIGNED";
        public const string DriverStatusChanged = "DRIVER_STATUS_CHANGED";

        // System actions
        public const string SettingsChanged = "SETTINGS_CHANGED";
        public const string DataExported = "DATA_EXPORTED";
        public const string DataImported = "DATA_IMPORTED";
        public const string BackupCreated = "BACKUP_CREATED";
    }

    public static class EntityTypes
    {
        public const string User = "USER";
        public const string Role = "ROLE";
        public const string Move = "MOVE";
        public const string Driver = "DRIVER";
        public const string Vehicle = "VEHICLE";
        public const string Settings = "SETTINGS";
        public const string System = "SYSTEM";
    }

    public static class LogSeverities
    {
        public const string Info = "INFO";
        public const string Warning = "WARNING";
        public const string Error = "ERROR";
        public const string Critical = "CRITICAL";
    }
}
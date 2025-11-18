using System.ComponentModel.DataAnnotations;

namespace BeC.OpenId.Connect.Features.Messages.Models;

public class SendMessageDto
{
    [Required]
    public required string RecipientId { get; set; }

    [Required]
    public required string Message { get; set; }

    public string MessageType { get; set; } = "text";
    public Guid? JobId { get; set; }
    public object? Attachment { get; set; }
    public object? LocationData { get; set; }
}

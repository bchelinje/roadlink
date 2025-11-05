namespace BeC.OpenId.Connect.Features.Users.Dtos;

public class ProfilePictureResponseDto
{
    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// URL of the uploaded profile picture
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// File name of the uploaded picture
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}
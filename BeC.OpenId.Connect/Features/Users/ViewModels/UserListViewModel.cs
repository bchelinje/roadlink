namespace BeC.OpenId.Connect.Features.Users.ViewModels;

/// <summary>
/// View model for paginated user list
/// </summary>
public class UserListViewModel
{
    public List<UserViewModel> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

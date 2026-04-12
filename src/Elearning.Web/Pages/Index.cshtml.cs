namespace Elearning.Web.Pages;

public class IndexModel : ElearningPageModel
{
    public bool IsAuthenticated => CurrentUser.IsAuthenticated;

    public string? UserName => CurrentUser.UserName;

    public void OnGet()
    {
    }
}

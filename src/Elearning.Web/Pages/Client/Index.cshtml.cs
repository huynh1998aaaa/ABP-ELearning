namespace Elearning.Web.Pages.Client;

public class IndexModel : ElearningClientPageModel
{
    public string? UserName => CurrentUser.UserName;

    public void OnGet()
    {
    }
}

using Microsoft.AspNetCore.Authorization;
using Elearning.Web.Pages;

namespace Elearning.Web.Pages.Client;

[Authorize]
public abstract class ElearningClientPageModel : ElearningPageModel
{
}

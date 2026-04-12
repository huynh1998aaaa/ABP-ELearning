using Microsoft.AspNetCore.Authorization;
using Elearning.Web.Pages;

namespace Elearning.Web.Pages.Admin;

[Authorize]
public abstract class ElearningAdminPageModel : ElearningPageModel
{
}

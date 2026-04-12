using Elearning.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Elearning.Web.Pages;

/* Inherit your PageModel classes from this class.
 */
public abstract class ElearningPageModel : AbpPageModel
{
    protected ElearningPageModel()
    {
        LocalizationResourceType = typeof(ElearningResource);
    }
}

using Elearning.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Elearning.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class ElearningController : AbpControllerBase
{
    protected ElearningController()
    {
        LocalizationResource = typeof(ElearningResource);
    }
}

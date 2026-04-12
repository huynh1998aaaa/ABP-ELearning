using System;
using System.Collections.Generic;
using System.Text;
using Elearning.Localization;
using Volo.Abp.Application.Services;

namespace Elearning;

/* Inherit your application services from this class.
 */
public abstract class ElearningAppService : ApplicationService
{
    protected ElearningAppService()
    {
        LocalizationResource = typeof(ElearningResource);
    }
}

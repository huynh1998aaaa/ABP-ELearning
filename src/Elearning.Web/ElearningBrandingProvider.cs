using Microsoft.Extensions.Localization;
using Elearning.Localization;
using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace Elearning.Web;

[Dependency(ReplaceServices = true)]
public class ElearningBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<ElearningResource> _localizer;

    public ElearningBrandingProvider(IStringLocalizer<ElearningResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}

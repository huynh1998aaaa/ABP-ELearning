using Volo.Abp.Settings;

namespace Elearning.Settings;

public class ElearningSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(ElearningSettings.MySetting1));
    }
}

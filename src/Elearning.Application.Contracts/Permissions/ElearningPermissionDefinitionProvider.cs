using Elearning.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Elearning.Permissions;

public class ElearningPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(ElearningPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(ElearningPermissions.MyPermission1, L("Permission:MyPermission1"));
        var questionTypes = myGroup.AddPermission(ElearningPermissions.QuestionTypes.Default, L("Permission:QuestionTypes"));
        questionTypes.AddChild(ElearningPermissions.QuestionTypes.Create, L("Permission:QuestionTypes.Create"));
        questionTypes.AddChild(ElearningPermissions.QuestionTypes.Update, L("Permission:QuestionTypes.Update"));
        questionTypes.AddChild(ElearningPermissions.QuestionTypes.Delete, L("Permission:QuestionTypes.Delete"));

        var questions = myGroup.AddPermission(ElearningPermissions.Questions.Default, L("Permission:Questions"));
        questions.AddChild(ElearningPermissions.Questions.Create, L("Permission:Questions.Create"));
        questions.AddChild(ElearningPermissions.Questions.Update, L("Permission:Questions.Update"));
        questions.AddChild(ElearningPermissions.Questions.Delete, L("Permission:Questions.Delete"));
        questions.AddChild(ElearningPermissions.Questions.Publish, L("Permission:Questions.Publish"));
        questions.AddChild(ElearningPermissions.Questions.Import, L("Permission:Questions.Import"));

        var premiumSubscriptions = myGroup.AddPermission(ElearningPermissions.PremiumSubscriptions.Default, L("Permission:PremiumSubscriptions"));
        premiumSubscriptions.AddChild(ElearningPermissions.PremiumSubscriptions.Create, L("Permission:PremiumSubscriptions.Create"));
        premiumSubscriptions.AddChild(ElearningPermissions.PremiumSubscriptions.Update, L("Permission:PremiumSubscriptions.Update"));
        premiumSubscriptions.AddChild(ElearningPermissions.PremiumSubscriptions.Cancel, L("Permission:PremiumSubscriptions.Cancel"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<ElearningResource>(name);
    }
}

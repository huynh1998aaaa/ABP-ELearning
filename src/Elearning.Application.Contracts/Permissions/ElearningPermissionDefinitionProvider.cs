using Elearning.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Elearning.Permissions;

public class ElearningPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(ElearningPermissions.GroupName);
        myGroup.AddPermission(ElearningPermissions.AdminPortal.Access, L("Permission:AdminPortal"));

        var questionTypes = myGroup.AddPermission(ElearningPermissions.QuestionTypes.Default, L("Permission:QuestionTypes"));
        questionTypes.AddChild(ElearningPermissions.QuestionTypes.Create, L("Permission:QuestionTypes.Create"));
        questionTypes.AddChild(ElearningPermissions.QuestionTypes.Update, L("Permission:QuestionTypes.Update"));
        questionTypes.AddChild(ElearningPermissions.QuestionTypes.Delete, L("Permission:QuestionTypes.Delete"));

        var subjects = myGroup.AddPermission(ElearningPermissions.Subjects.Default, L("Permission:Subjects"));
        subjects.AddChild(ElearningPermissions.Subjects.Create, L("Permission:Subjects.Create"));
        subjects.AddChild(ElearningPermissions.Subjects.Update, L("Permission:Subjects.Update"));
        subjects.AddChild(ElearningPermissions.Subjects.Delete, L("Permission:Subjects.Delete"));

        var questions = myGroup.AddPermission(ElearningPermissions.Questions.Default, L("Permission:Questions"));
        questions.AddChild(ElearningPermissions.Questions.Create, L("Permission:Questions.Create"));
        questions.AddChild(ElearningPermissions.Questions.Update, L("Permission:Questions.Update"));
        questions.AddChild(ElearningPermissions.Questions.Publish, L("Permission:Questions.Publish"));
        questions.AddChild(ElearningPermissions.Questions.Import, L("Permission:Questions.Import"));
        questions.AddChild(ElearningPermissions.Questions.Delete, L("Permission:Questions.Delete"));

        var premiumSubscriptions = myGroup.AddPermission(ElearningPermissions.PremiumSubscriptions.Default, L("Permission:PremiumSubscriptions"));
        premiumSubscriptions.AddChild(ElearningPermissions.PremiumSubscriptions.Create, L("Permission:PremiumSubscriptions.Create"));
        premiumSubscriptions.AddChild(ElearningPermissions.PremiumSubscriptions.Update, L("Permission:PremiumSubscriptions.Update"));
        premiumSubscriptions.AddChild(ElearningPermissions.PremiumSubscriptions.Cancel, L("Permission:PremiumSubscriptions.Cancel"));

        var exams = myGroup.AddPermission(ElearningPermissions.Exams.Default, L("Permission:Exams"));
        exams.AddChild(ElearningPermissions.Exams.Create, L("Permission:Exams.Create"));
        exams.AddChild(ElearningPermissions.Exams.Update, L("Permission:Exams.Update"));
        exams.AddChild(ElearningPermissions.Exams.Delete, L("Permission:Exams.Delete"));
        exams.AddChild(ElearningPermissions.Exams.Publish, L("Permission:Exams.Publish"));
        exams.AddChild(ElearningPermissions.Exams.ManageQuestions, L("Permission:Exams.ManageQuestions"));

        var practices = myGroup.AddPermission(ElearningPermissions.Practices.Default, L("Permission:Practices"));
        practices.AddChild(ElearningPermissions.Practices.Create, L("Permission:Practices.Create"));
        practices.AddChild(ElearningPermissions.Practices.Update, L("Permission:Practices.Update"));
        practices.AddChild(ElearningPermissions.Practices.Delete, L("Permission:Practices.Delete"));
        practices.AddChild(ElearningPermissions.Practices.Publish, L("Permission:Practices.Publish"));
        practices.AddChild(ElearningPermissions.Practices.ManageQuestions, L("Permission:Practices.ManageQuestions"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<ElearningResource>(name);
    }
}

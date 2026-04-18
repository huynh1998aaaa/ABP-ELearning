using System.Threading.Tasks;
using Elearning.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Identity;

namespace Elearning.Web.Pages.Admin;

public class IndexModel : ElearningAdminPageModel
{
    private readonly IAuthorizationService _authorizationService;

    public IndexModel(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public bool CanManageAccounts { get; private set; }

    public bool CanManagePremium { get; private set; }

    public bool CanManageQuestionTypes { get; private set; }

    public bool CanManageSubjects { get; private set; }

    public bool CanManageQuestions { get; private set; }

    public bool CanImportQuestions { get; private set; }

    public bool CanManageExams { get; private set; }

    public bool CanManageExamQuestions { get; private set; }

    public bool CanManagePractices { get; private set; }

    public bool CanManagePracticeQuestions { get; private set; }

    public async Task OnGetAsync()
    {
        CanManageAccounts = (await _authorizationService.AuthorizeAsync(User, IdentityPermissions.Users.Default)).Succeeded;
        CanManagePremium = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.PremiumSubscriptions.Default)).Succeeded;
        CanManageQuestionTypes = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.QuestionTypes.Default)).Succeeded;
        CanManageSubjects = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Subjects.Default)).Succeeded;
        CanManageQuestions = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Questions.Default)).Succeeded;
        CanImportQuestions = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Questions.Import)).Succeeded;
        CanManageExams = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Exams.Default)).Succeeded;
        CanManageExamQuestions = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Exams.ManageQuestions)).Succeeded;
        CanManagePractices = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Practices.Default)).Succeeded;
        CanManagePracticeQuestions = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Practices.ManageQuestions)).Succeeded;
    }
}

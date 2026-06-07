using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.Questions;
using Elearning.QuestionTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Questions;

[Authorize(ElearningPermissions.Questions.Default)]
public class IndexModel : ElearningAdminPageModel
{
    private const int PageSize = 10;

    private readonly IAuthorizationService _authorizationService;
    private readonly IQuestionAppService _questionAppService;
    private readonly IQuestionTypeAppService _questionTypeAppService;

    public IndexModel(
        IQuestionAppService questionAppService,
        IQuestionTypeAppService questionTypeAppService,
        IAuthorizationService authorizationService)
    {
        _questionAppService = questionAppService;
        _questionTypeAppService = questionTypeAppService;
        _authorizationService = authorizationService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? QuestionTypeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public QuestionDifficulty? Difficulty { get; set; }

    [BindProperty(SupportsGet = true)]
    public QuestionStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public IReadOnlyList<QuestionDto> Questions { get; private set; } = Array.Empty<QuestionDto>();

    public IReadOnlyList<QuestionTypeDto> QuestionTypes { get; private set; } = Array.Empty<QuestionTypeDto>();

    public long TotalCount { get; private set; }

    public int PublishedCount { get; private set; }

    public int DraftCount { get; private set; }

    public int ArchivedCount { get; private set; }

    public bool CanCreate { get; private set; }

    public bool CanUpdate { get; private set; }

    public bool CanImport { get; private set; }

    public bool CanDelete { get; private set; }

    public int TotalPages => TotalCount == 0
        ? 1
        : (int)Math.Ceiling((double)TotalCount / PageSize);

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnGetTableAsync()
    {
        await LoadAsync();
        return Partial("_Table", this);
    }

    private async Task LoadAsync()
    {
        if (CurrentPage < 1)
        {
            CurrentPage = 1;
        }

        CanCreate = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Questions.Create)).Succeeded;
        CanUpdate = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Questions.Update)).Succeeded;
        CanImport = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Questions.Import)).Succeeded;
        CanDelete = (await _authorizationService.AuthorizeAsync(User, ElearningPermissions.Questions.Delete)).Succeeded;

        var questionTypes = await _questionTypeAppService.GetListAsync(new GetQuestionTypeListInput
        {
            MaxResultCount = 1000,
            SkipCount = 0
        });
        QuestionTypes = questionTypes.Items.OrderBy(x => x.SortOrder).ThenBy(x => x.DisplayName).ToList();

        var allQuestions = await _questionAppService.GetListAsync(new GetQuestionListInput
        {
            MaxResultCount = 1000,
            SkipCount = 0,
            Filter = Filter,
            QuestionTypeId = QuestionTypeId,
            Difficulty = Difficulty,
            Status = Status
        });

        TotalCount = allQuestions.TotalCount;
        PublishedCount = allQuestions.Items.Count(x => x.Status == QuestionStatus.Published);
        DraftCount = allQuestions.Items.Count(x => x.Status == QuestionStatus.Draft);
        ArchivedCount = allQuestions.Items.Count(x => x.Status == QuestionStatus.Archived);
        Questions = allQuestions.Items.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _questionAppService.DeleteAsync(id);
            if (IsAjaxRequest)
            {
                return AjaxSuccess();
            }
        }
        catch (Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }

        return RedirectToPage(new { Filter, QuestionTypeId, Difficulty, Status, CurrentPage });
    }

    public string BuildTableUrl()
    {
        var query = new List<string> { "handler=Table", $"currentPage={CurrentPage}" };
        if (!string.IsNullOrWhiteSpace(Filter))
        {
            query.Add($"filter={Uri.EscapeDataString(Filter)}");
        }
        if (QuestionTypeId.HasValue)
        {
            query.Add($"questionTypeId={QuestionTypeId}");
        }
        if (Difficulty.HasValue)
        {
            query.Add($"difficulty={Difficulty}");
        }
        if (Status.HasValue)
        {
            query.Add($"status={Status}");
        }

        return "/admin/questions?" + string.Join("&", query);
    }

    public string BuildPageUrl(int page)
    {
        var query = new List<string> { $"currentPage={page}" };
        if (!string.IsNullOrWhiteSpace(Filter))
        {
            query.Add($"filter={Uri.EscapeDataString(Filter)}");
        }
        if (QuestionTypeId.HasValue)
        {
            query.Add($"questionTypeId={QuestionTypeId}");
        }
        if (Difficulty.HasValue)
        {
            query.Add($"difficulty={Difficulty}");
        }
        if (Status.HasValue)
        {
            query.Add($"status={Status}");
        }

        return "/admin/questions?" + string.Join("&", query);
    }
}

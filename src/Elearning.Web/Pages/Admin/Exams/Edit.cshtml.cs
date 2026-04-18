using System;
using System.Threading.Tasks;
using Elearning.Exams;
using Elearning.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elearning.Web.Pages.Admin.Exams;

[Authorize(ElearningPermissions.Exams.Update)]
public class EditModel : ExamFormPageModel
{
    private readonly IExamAppService _examAppService;

    public EditModel(IExamAppService examAppService)
    {
        _examAppService = examAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public UpdateExamDto Input { get; set; } = new();

    public ExamDto Exam { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        LoadSelectOptions();
        await LoadExamAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetModalAsync()
    {
        LoadSelectOptions();
        await LoadExamAsync();
        return Partial("_EditForm", this);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            LoadSelectOptions();
            Exam = await _examAppService.GetAsync(Id);
            return Page();
        }

        await _examAppService.UpdateAsync(Id, Input);
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostModalAsync()
    {
        if (!ModelState.IsValid)
        {
            LoadSelectOptions();
            Exam = await _examAppService.GetAsync(Id);
            Response.StatusCode = 400;
            return Partial("_EditForm", this);
        }

        try
        {
            await _examAppService.UpdateAsync(Id, Input);
            return AjaxSuccess();
        }
        catch (System.Exception ex) when (IsAjaxRequest)
        {
            return AjaxError(ex);
        }
    }

    private async Task LoadExamAsync()
    {
        Exam = await _examAppService.GetAsync(Id);
        Input = new UpdateExamDto
        {
            Code = Exam.Code,
            Title = Exam.Title,
            Description = Exam.Description,
            AccessLevel = Exam.AccessLevel,
            SelectionMode = Exam.SelectionMode,
            DurationMinutes = Exam.DurationMinutes,
            TotalQuestionCount = Exam.TotalQuestionCount,
            PassingScore = Exam.PassingScore,
            ShuffleQuestions = Exam.ShuffleQuestions,
            ShuffleOptions = Exam.ShuffleOptions,
            SortOrder = Exam.SortOrder
        };
    }
}

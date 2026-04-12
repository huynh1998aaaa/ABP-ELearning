using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.QuestionTypes;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Elearning.Web.Pages.Admin.Questions;

public abstract class QuestionFormPageModel : ElearningAdminPageModel
{
    protected const int DefaultOptionRows = 5;
    protected const int DefaultMatchingRows = 5;

    protected readonly IQuestionTypeAppService QuestionTypeAppService;

    protected QuestionFormPageModel(IQuestionTypeAppService questionTypeAppService)
    {
        QuestionTypeAppService = questionTypeAppService;
    }

    public List<QuestionTypeDto> AvailableQuestionTypes { get; private set; } = new();

    public List<SelectListItem> DifficultyOptions { get; private set; } = new();

    protected async Task LoadQuestionTypesAsync(bool activeOnly)
    {
        var result = await QuestionTypeAppService.GetListAsync(new GetQuestionTypeListInput
        {
            MaxResultCount = 1000,
            SkipCount = 0,
            IsActive = activeOnly ? true : null
        });

        AvailableQuestionTypes = result.Items.OrderBy(x => x.SortOrder).ThenBy(x => x.DisplayName).ToList();
        DifficultyOptions = Enum.GetValues<Elearning.Questions.QuestionDifficulty>()
            .Select(x => new SelectListItem(L[$"Enum:QuestionDifficulty:{x}"], x.ToString()))
            .ToList();
    }

    protected static List<Elearning.Questions.QuestionOptionInputDto> PadOptions(
        IEnumerable<Elearning.Questions.QuestionOptionInputDto> options)
    {
        var result = options.ToList();
        while (result.Count < DefaultOptionRows)
        {
            result.Add(new Elearning.Questions.QuestionOptionInputDto { SortOrder = result.Count + 1 });
        }

        return result;
    }

    protected static List<Elearning.Questions.QuestionMatchingPairInputDto> PadMatchingPairs(
        IEnumerable<Elearning.Questions.QuestionMatchingPairInputDto> pairs)
    {
        var result = pairs.ToList();
        while (result.Count < DefaultMatchingRows)
        {
            result.Add(new Elearning.Questions.QuestionMatchingPairInputDto { SortOrder = result.Count + 1 });
        }

        return result;
    }
}

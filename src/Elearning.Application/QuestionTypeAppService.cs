using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elearning.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Elearning.QuestionTypes;

[Authorize(ElearningPermissions.QuestionTypes.Default)]
public class QuestionTypeAppService : ElearningAppService, IQuestionTypeAppService
{
    private static readonly Regex CodeRegex = new("^[a-z0-9_-]+$", RegexOptions.Compiled);

    private readonly IGuidGenerator _guidGenerator;
    private readonly IRepository<QuestionType, Guid> _questionTypeRepository;

    public QuestionTypeAppService(
        IRepository<QuestionType, Guid> questionTypeRepository,
        IGuidGenerator guidGenerator)
    {
        _questionTypeRepository = questionTypeRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task<PagedResultDto<QuestionTypeDto>> GetListAsync(GetQuestionTypeListInput input)
    {
        var query = await _questionTypeRepository.GetQueryableAsync();

        if (!input.Filter.IsNullOrWhiteSpace())
        {
            var filter = input.Filter!.Trim();
            query = query.Where(x =>
                x.Code.Contains(filter) ||
                x.DisplayName.Contains(filter) ||
                (x.Description != null && x.Description.Contains(filter)));
        }

        if (input.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == input.IsActive.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(query);
        var items = await AsyncExecuter.ToListAsync(ApplySorting(query, input.Sorting)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount));

        return new PagedResultDto<QuestionTypeDto>(
            totalCount,
            items.Select(MapToDto).ToList());
    }

    public async Task<QuestionTypeDto> GetAsync(Guid id)
    {
        return MapToDto(await _questionTypeRepository.GetAsync(id));
    }

    [Authorize(ElearningPermissions.QuestionTypes.Create)]
    public async Task<QuestionTypeDto> CreateAsync(CreateQuestionTypeDto input)
    {
        var code = NormalizeCode(input.Code);
        await ValidateCodeAsync(code);
        ValidateOptionRules(input.SupportsOptions, input.MinimumOptions, input.MaximumOptions);

        var questionType = new QuestionType(
            _guidGenerator.Create(),
            code,
            input.DisplayName,
            input.InputKind,
            input.ScoringKind,
            input.SupportsOptions,
            input.SupportsAnswerPairs,
            input.RequiresManualGrading,
            input.AllowMultipleCorrectAnswers,
            input.SortOrder,
            description: input.Description,
            minimumOptions: input.MinimumOptions,
            maximumOptions: input.MaximumOptions);

        if (!input.IsActive)
        {
            questionType.Deactivate();
        }

        await _questionTypeRepository.InsertAsync(questionType, autoSave: true);
        return MapToDto(questionType);
    }

    [Authorize(ElearningPermissions.QuestionTypes.Update)]
    public async Task<QuestionTypeDto> UpdateAsync(Guid id, UpdateQuestionTypeDto input)
    {
        var questionType = await _questionTypeRepository.GetAsync(id);
        var code = NormalizeCode(input.Code);

        if (!string.Equals(questionType.Code, code, StringComparison.Ordinal))
        {
            throw new UserFriendlyException(L["QuestionTypes:CodeCannotBeChanged"]);
        }

        ValidateOptionRules(input.SupportsOptions, input.MinimumOptions, input.MaximumOptions);

        questionType.UpdateDetails(
            input.DisplayName,
            input.Description,
            input.InputKind,
            input.ScoringKind,
            input.SupportsOptions,
            input.SupportsAnswerPairs,
            input.RequiresManualGrading,
            input.AllowMultipleCorrectAnswers,
            input.SortOrder,
            input.MinimumOptions,
            input.MaximumOptions);

        await _questionTypeRepository.UpdateAsync(questionType, autoSave: true);
        return MapToDto(questionType);
    }

    [Authorize(ElearningPermissions.QuestionTypes.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var questionType = await _questionTypeRepository.GetAsync(id);

        if (questionType.IsSystem)
        {
            throw new UserFriendlyException(L["QuestionTypes:SystemCannotBeDeleted"]);
        }

        await _questionTypeRepository.DeleteAsync(questionType, autoSave: true);
    }

    [Authorize(ElearningPermissions.QuestionTypes.Update)]
    public async Task ActivateAsync(Guid id)
    {
        var questionType = await _questionTypeRepository.GetAsync(id);
        questionType.Activate();
        await _questionTypeRepository.UpdateAsync(questionType, autoSave: true);
    }

    [Authorize(ElearningPermissions.QuestionTypes.Update)]
    public async Task DeactivateAsync(Guid id)
    {
        var questionType = await _questionTypeRepository.GetAsync(id);
        questionType.Deactivate();
        await _questionTypeRepository.UpdateAsync(questionType, autoSave: true);
    }

    private static IQueryable<QuestionType> ApplySorting(IQueryable<QuestionType> query, string? sorting)
    {
        return sorting?.Trim().ToLowerInvariant() switch
        {
            "code" => query.OrderBy(x => x.Code),
            "code desc" => query.OrderByDescending(x => x.Code),
            "displayname" => query.OrderBy(x => x.DisplayName),
            "displayname desc" => query.OrderByDescending(x => x.DisplayName),
            "sortorder desc" => query.OrderByDescending(x => x.SortOrder).ThenBy(x => x.DisplayName),
            _ => query.OrderBy(x => x.SortOrder).ThenBy(x => x.DisplayName)
        };
    }

    private async Task ValidateCodeAsync(string code)
    {
        if (!CodeRegex.IsMatch(code))
        {
            throw new UserFriendlyException(L["QuestionTypes:CodeInvalid"]);
        }

        if (await _questionTypeRepository.FindAsync(x => x.Code == code) != null)
        {
            throw new UserFriendlyException(L["QuestionTypes:CodeAlreadyExists", code]);
        }
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().ToLowerInvariant();
    }

    private void ValidateOptionRules(bool supportsOptions, int? minimumOptions, int? maximumOptions)
    {
        if (!supportsOptions)
        {
            return;
        }

        if (minimumOptions.HasValue && maximumOptions.HasValue && maximumOptions < minimumOptions)
        {
            throw new UserFriendlyException(L["QuestionTypes:MaximumOptionsMustBeGreaterOrEqualMinimum"]);
        }
    }

    private static QuestionTypeDto MapToDto(QuestionType questionType)
    {
        return new QuestionTypeDto
        {
            Id = questionType.Id,
            Code = questionType.Code,
            DisplayName = questionType.DisplayName,
            Description = questionType.Description,
            InputKind = questionType.InputKind,
            ScoringKind = questionType.ScoringKind,
            IsActive = questionType.IsActive,
            IsSystem = questionType.IsSystem,
            SortOrder = questionType.SortOrder,
            SupportsOptions = questionType.SupportsOptions,
            SupportsAnswerPairs = questionType.SupportsAnswerPairs,
            RequiresManualGrading = questionType.RequiresManualGrading,
            AllowMultipleCorrectAnswers = questionType.AllowMultipleCorrectAnswers,
            MinimumOptions = questionType.MinimumOptions,
            MaximumOptions = questionType.MaximumOptions,
            CreationTime = questionType.CreationTime,
            CreatorId = questionType.CreatorId,
            LastModificationTime = questionType.LastModificationTime,
            LastModifierId = questionType.LastModifierId,
            IsDeleted = questionType.IsDeleted,
            DeleterId = questionType.DeleterId,
            DeletionTime = questionType.DeletionTime
        };
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Elearning.Practices;

public interface IPracticeSetAppService : IApplicationService
{
    Task<PagedResultDto<PracticeSetDto>> GetListAsync(GetPracticeSetListInput input);

    Task<PracticeSetDto> GetAsync(Guid id);

    Task<PracticeSetDto> CreateAsync(CreatePracticeSetDto input);

    Task<PracticeSetDto> UpdateAsync(Guid id, UpdatePracticeSetDto input);

    Task DeleteAsync(Guid id);

    Task PublishAsync(Guid id);

    Task ArchiveAsync(Guid id);

    Task ActivateAsync(Guid id);

    Task DeactivateAsync(Guid id);

    Task<PracticeSetPreviewDto> GetPreviewAsync(Guid id);

    Task<PagedResultDto<PracticeQuestionDto>> GetQuestionsAsync(Guid practiceSetId, GetPracticeQuestionListInput input);

    Task<PagedResultDto<PracticeAvailableQuestionDto>> GetAvailableQuestionsAsync(Guid practiceSetId, GetPracticeAvailableQuestionListInput input);

    Task<PracticeQuestionDto> AddQuestionAsync(Guid practiceSetId, AddPracticeQuestionDto input);

    Task<PracticeQuestionDto> UpdateQuestionAsync(Guid practiceQuestionId, UpdatePracticeQuestionDto input);

    Task RemoveQuestionAsync(Guid practiceQuestionId);

    Task ReorderQuestionsAsync(Guid practiceSetId, List<Guid> practiceQuestionIds);

    Task<List<PracticeAutoQuestionRuleDto>> GetAutoQuestionRulesAsync(Guid practiceSetId);

    Task<PracticeAutoQuestionRuleDto> AddAutoQuestionRuleAsync(Guid practiceSetId, CreatePracticeAutoQuestionRuleDto input);

    Task<PracticeAutoQuestionRuleDto> UpdateAutoQuestionRuleAsync(Guid ruleId, UpdatePracticeAutoQuestionRuleDto input);

    Task RemoveAutoQuestionRuleAsync(Guid ruleId);

    Task<PracticeAutoAssignmentPreviewDto> PreviewAutoAssignAsync(Guid practiceSetId);

    Task<PracticeAutoAssignmentResultDto> ApplyAutoAssignAsync(Guid practiceSetId);
}

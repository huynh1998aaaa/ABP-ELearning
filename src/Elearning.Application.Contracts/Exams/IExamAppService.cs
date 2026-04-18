using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Elearning.Exams;

public interface IExamAppService : IApplicationService
{
    Task<PagedResultDto<ExamDto>> GetListAsync(GetExamListInput input);

    Task<ExamDto> GetAsync(Guid id);

    Task<ExamDto> CreateAsync(CreateExamDto input);

    Task<ExamDto> UpdateAsync(Guid id, UpdateExamDto input);

    Task DeleteAsync(Guid id);

    Task PublishAsync(Guid id);

    Task ArchiveAsync(Guid id);

    Task ActivateAsync(Guid id);

    Task DeactivateAsync(Guid id);

    Task<ExamPreviewDto> GetPreviewAsync(Guid id);

    Task<PagedResultDto<ExamQuestionDto>> GetQuestionsAsync(Guid examId, GetExamQuestionListInput input);

    Task<PagedResultDto<ExamAvailableQuestionDto>> GetAvailableQuestionsAsync(Guid examId, GetExamAvailableQuestionListInput input);

    Task<ExamQuestionDto> AddQuestionAsync(Guid examId, AddExamQuestionDto input);

    Task<ExamQuestionDto> UpdateQuestionAsync(Guid examQuestionId, UpdateExamQuestionDto input);

    Task RemoveQuestionAsync(Guid examQuestionId);

    Task ReorderQuestionsAsync(Guid examId, List<Guid> examQuestionIds);

    Task<List<ExamAutoQuestionRuleDto>> GetAutoQuestionRulesAsync(Guid examId);

    Task<ExamAutoQuestionRuleDto> AddAutoQuestionRuleAsync(Guid examId, CreateExamAutoQuestionRuleDto input);

    Task<ExamAutoQuestionRuleDto> UpdateAutoQuestionRuleAsync(Guid ruleId, UpdateExamAutoQuestionRuleDto input);

    Task RemoveAutoQuestionRuleAsync(Guid ruleId);

    Task<ExamAutoAssignmentPreviewDto> PreviewAutoAssignAsync(Guid examId);

    Task<ExamAutoAssignmentResultDto> ApplyAutoAssignAsync(Guid examId);
}

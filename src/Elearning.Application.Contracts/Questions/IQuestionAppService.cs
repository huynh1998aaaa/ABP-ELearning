using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Elearning.Questions;

public interface IQuestionAppService : IApplicationService
{
    Task<PagedResultDto<QuestionDto>> GetListAsync(GetQuestionListInput input);

    Task<QuestionDto> GetAsync(Guid id);

    Task<QuestionDto> CreateAsync(CreateQuestionDto input);

    Task<QuestionDto> UpdateAsync(Guid id, UpdateQuestionDto input);

    Task ActivateAsync(Guid id);

    Task DeactivateAsync(Guid id);

    Task PublishAsync(Guid id);

    Task ArchiveAsync(Guid id);

    Task DeleteAsync(Guid id);

    Task<BulkQuestionActionResultDto> BulkPublishAsync(BulkQuestionActionInput input);

    Task<BulkQuestionActionResultDto> BulkArchiveAsync(BulkQuestionActionInput input);
}

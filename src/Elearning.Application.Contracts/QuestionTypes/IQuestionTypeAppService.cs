using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Elearning.QuestionTypes;

public interface IQuestionTypeAppService : IApplicationService
{
    Task<PagedResultDto<QuestionTypeDto>> GetListAsync(GetQuestionTypeListInput input);

    Task<QuestionTypeDto> GetAsync(Guid id);

    Task<QuestionTypeDto> CreateAsync(CreateQuestionTypeDto input);

    Task<QuestionTypeDto> UpdateAsync(Guid id, UpdateQuestionTypeDto input);

    Task DeleteAsync(Guid id);

    Task ActivateAsync(Guid id);

    Task DeactivateAsync(Guid id);
}

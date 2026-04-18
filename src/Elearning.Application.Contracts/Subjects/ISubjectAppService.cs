using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Elearning.Subjects;

public interface ISubjectAppService : IApplicationService
{
    Task<PagedResultDto<SubjectDto>> GetListAsync(GetSubjectListInput input);

    Task<SubjectDto> GetAsync(Guid id);

    Task<SubjectDto> CreateAsync(CreateSubjectDto input);

    Task<SubjectDto> UpdateAsync(Guid id, UpdateSubjectDto input);

    Task DeleteAsync(Guid id);

    Task ActivateAsync(Guid id);

    Task DeactivateAsync(Guid id);
}

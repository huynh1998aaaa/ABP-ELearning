using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Elearning.ClientContent;

public interface IClientLearningSessionAppService : IApplicationService
{
    Task<ClientLearningLaunchResultDto> StartOrResumeAsync(ClientLearningItemKind kind, Guid id);

    Task<ClientLearningSessionDto> GetAsync(Guid sessionId);

    Task SaveAnswerAsync(Guid sessionId, SaveClientLearningAnswerDto input);

    Task<ClientLearningSessionResultDto> SubmitAsync(Guid sessionId);

    Task<ClientLearningSessionResultDto> GetResultAsync(Guid sessionId);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Exams;
using Elearning.Practices;
using Elearning.PremiumSubscriptions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Domain.Repositories;

namespace Elearning.ClientContent;

[Authorize]
public class ClientLearningPortalAppService : ElearningAppService, IClientLearningPortalAppService
{
    private readonly ICurrentUserPremiumAppService _currentUserPremiumAppService;
    private readonly IRepository<Exam, Guid> _examRepository;
    private readonly IRepository<ExamQuestion, Guid> _examQuestionRepository;
    private readonly IRepository<PracticeSet, Guid> _practiceSetRepository;
    private readonly IRepository<PracticeQuestion, Guid> _practiceQuestionRepository;

    public ClientLearningPortalAppService(
        IRepository<Exam, Guid> examRepository,
        IRepository<ExamQuestion, Guid> examQuestionRepository,
        IRepository<PracticeSet, Guid> practiceSetRepository,
        IRepository<PracticeQuestion, Guid> practiceQuestionRepository,
        ICurrentUserPremiumAppService currentUserPremiumAppService)
    {
        _examRepository = examRepository;
        _examQuestionRepository = examQuestionRepository;
        _practiceSetRepository = practiceSetRepository;
        _practiceQuestionRepository = practiceQuestionRepository;
        _currentUserPremiumAppService = currentUserPremiumAppService;
    }

    public async Task<ClientLearningPortalDto> GetAsync()
    {
        var premiumStatus = await _currentUserPremiumAppService.GetCurrentUserPremiumStatusAsync();
        var isPremium = premiumStatus.IsPremium;

        var examItems = await GetExamItemsAsync(isPremium);
        var practiceItems = await GetPracticeItemsAsync(isPremium);
        var items = examItems
            .Concat(practiceItems)
            .OrderBy(x => x.AccessLevel)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .ToList();

        return new ClientLearningPortalDto
        {
            IsPremium = isPremium,
            FreeItems = items.Where(x => x.AccessLevel == ClientLearningAccessLevel.Free).ToList(),
            PremiumItems = items.Where(x => x.AccessLevel == ClientLearningAccessLevel.Premium).ToList()
        };
    }

    private async Task<List<ClientLearningItemDto>> GetExamItemsAsync(bool isPremium)
    {
        var query = await _examRepository.GetQueryableAsync();
        var exams = await AsyncExecuter.ToListAsync(query
            .Where(x => x.Status == ExamStatus.Published && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title));
        var questionCounts = await GetExamQuestionCountsAsync(exams.Select(x => x.Id).ToList());

        return exams.Select(x => new ClientLearningItemDto
        {
            Id = x.Id,
            Kind = ClientLearningItemKind.Exam,
            Code = x.Code,
            Title = x.Title,
            Description = x.Description,
            AccessLevel = x.AccessLevel == ExamAccessLevel.Premium
                ? ClientLearningAccessLevel.Premium
                : ClientLearningAccessLevel.Free,
            SelectionMode = x.SelectionMode == ExamSelectionMode.Random
                ? ClientLearningSelectionMode.Random
                : ClientLearningSelectionMode.Fixed,
            TotalQuestionCount = x.TotalQuestionCount,
            AssignedQuestionCount = questionCounts.GetValueOrDefault(x.Id),
            IsLocked = x.AccessLevel == ExamAccessLevel.Premium && !isPremium,
            ShowExplanation = false,
            SortOrder = x.SortOrder
        }).ToList();
    }

    private async Task<List<ClientLearningItemDto>> GetPracticeItemsAsync(bool isPremium)
    {
        var query = await _practiceSetRepository.GetQueryableAsync();
        var practiceSets = await AsyncExecuter.ToListAsync(query
            .Where(x => x.Status == PracticeStatus.Published && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title));
        var questionCounts = await GetPracticeQuestionCountsAsync(practiceSets.Select(x => x.Id).ToList());

        return practiceSets.Select(x => new ClientLearningItemDto
        {
            Id = x.Id,
            Kind = ClientLearningItemKind.Practice,
            Code = x.Code,
            Title = x.Title,
            Description = x.Description,
            AccessLevel = x.AccessLevel == PracticeAccessLevel.Premium
                ? ClientLearningAccessLevel.Premium
                : ClientLearningAccessLevel.Free,
            SelectionMode = x.SelectionMode == PracticeSelectionMode.Random
                ? ClientLearningSelectionMode.Random
                : ClientLearningSelectionMode.Fixed,
            TotalQuestionCount = x.TotalQuestionCount,
            AssignedQuestionCount = questionCounts.GetValueOrDefault(x.Id),
            IsLocked = x.AccessLevel == PracticeAccessLevel.Premium && !isPremium,
            ShowExplanation = x.ShowExplanation,
            SortOrder = x.SortOrder
        }).ToList();
    }

    private async Task<Dictionary<Guid, int>> GetExamQuestionCountsAsync(IReadOnlyList<Guid> examIds)
    {
        if (examIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var query = await _examQuestionRepository.GetQueryableAsync();
        var rows = await AsyncExecuter.ToListAsync(query.Where(x => examIds.Contains(x.ExamId)));
        return rows.GroupBy(x => x.ExamId).ToDictionary(x => x.Key, x => x.Count());
    }

    private async Task<Dictionary<Guid, int>> GetPracticeQuestionCountsAsync(IReadOnlyList<Guid> practiceSetIds)
    {
        if (practiceSetIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var query = await _practiceQuestionRepository.GetQueryableAsync();
        var rows = await AsyncExecuter.ToListAsync(query.Where(x => practiceSetIds.Contains(x.PracticeSetId)));
        return rows.GroupBy(x => x.PracticeSetId).ToDictionary(x => x.Key, x => x.Count());
    }
}

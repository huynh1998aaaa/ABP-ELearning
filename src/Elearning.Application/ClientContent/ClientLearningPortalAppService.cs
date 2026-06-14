using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Exams;
using Elearning.Practices;
using Elearning.PremiumSubscriptions;
using Elearning.Questions;
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
    private readonly QuestionRuntimeReadinessProvider _questionRuntimeReadinessProvider;

    public ClientLearningPortalAppService(
        IRepository<Exam, Guid> examRepository,
        IRepository<ExamQuestion, Guid> examQuestionRepository,
        IRepository<PracticeSet, Guid> practiceSetRepository,
        IRepository<PracticeQuestion, Guid> practiceQuestionRepository,
        QuestionRuntimeReadinessProvider questionRuntimeReadinessProvider,
        ICurrentUserPremiumAppService currentUserPremiumAppService)
    {
        _examRepository = examRepository;
        _examQuestionRepository = examQuestionRepository;
        _practiceSetRepository = practiceSetRepository;
        _practiceQuestionRepository = practiceQuestionRepository;
        _questionRuntimeReadinessProvider = questionRuntimeReadinessProvider;
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
            PremiumEndTime = isPremium ? premiumStatus.EndTime : null,
            FreeItems = items.Where(x => x.AccessLevel == ClientLearningAccessLevel.Free).ToList(),
            PremiumItems = items.Where(x => x.AccessLevel == ClientLearningAccessLevel.Premium).ToList()
        };
    }

    private async Task<List<ClientLearningItemDto>> GetExamItemsAsync(bool isPremium)
    {
        var query = await _examRepository.GetQueryableAsync();
        var exams = await AsyncExecuter.ToListAsync(query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title));
        var readinessMap = await GetExamReadinessAsync(exams);

        return exams.Select(x =>
        {
            var readiness = readinessMap.GetValueOrDefault(x.Id, AssignmentReadiness.Empty);
            return new ClientLearningItemDto
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
                AssignedQuestionCount = readiness.AssignedQuestionCount,
                ValidAssignedQuestionCount = readiness.ValidAssignedQuestionCount,
                IsReady = readiness.ValidAssignedQuestionCount >= x.TotalQuestionCount,
                IsLocked = x.AccessLevel == ExamAccessLevel.Premium && !isPremium,
                ShowExplanation = false,
                SortOrder = x.SortOrder
            };
        })
            .Where(x => x.IsReady)
            .ToList();
    }

    private async Task<List<ClientLearningItemDto>> GetPracticeItemsAsync(bool isPremium)
    {
        var query = await _practiceSetRepository.GetQueryableAsync();
        var practiceSets = await AsyncExecuter.ToListAsync(query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title));
        var readinessMap = await GetPracticeReadinessAsync(practiceSets);

        return practiceSets.Select(x =>
        {
            var readiness = readinessMap.GetValueOrDefault(x.Id, AssignmentReadiness.Empty);
            return new ClientLearningItemDto
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
                AssignedQuestionCount = readiness.AssignedQuestionCount,
                ValidAssignedQuestionCount = readiness.ValidAssignedQuestionCount,
                IsReady = readiness.ValidAssignedQuestionCount >= x.TotalQuestionCount,
                IsLocked = x.AccessLevel == PracticeAccessLevel.Premium && !isPremium,
                ShowExplanation = x.ShowExplanation,
                SortOrder = x.SortOrder
            };
        })
            .Where(x => x.IsReady)
            .ToList();
    }

    private async Task<Dictionary<Guid, AssignmentReadiness>> GetExamReadinessAsync(IReadOnlyList<Exam> exams)
    {
        var result = exams.ToDictionary(x => x.Id, _ => AssignmentReadiness.Empty);
        if (exams.Count == 0)
        {
            return result;
        }

        var examIds = exams.Select(x => x.Id).ToList();
        var query = await _examQuestionRepository.GetQueryableAsync();
        var rows = await AsyncExecuter.ToListAsync(query.Where(x => examIds.Contains(x.ExamId)));
        var readinessMap = await _questionRuntimeReadinessProvider.GetReadinessMapAsync(rows.Select(x => x.QuestionId).Distinct().ToList());

        foreach (var group in rows.GroupBy(x => x.ExamId))
        {
            result[group.Key] = new AssignmentReadiness(
                group.Count(),
                group.Count(x => readinessMap.GetValueOrDefault(x.QuestionId)));
        }

        return result;
    }

    private async Task<Dictionary<Guid, AssignmentReadiness>> GetPracticeReadinessAsync(IReadOnlyList<PracticeSet> practiceSets)
    {
        var result = practiceSets.ToDictionary(x => x.Id, _ => AssignmentReadiness.Empty);
        if (practiceSets.Count == 0)
        {
            return result;
        }

        var practiceSetIds = practiceSets.Select(x => x.Id).ToList();
        var query = await _practiceQuestionRepository.GetQueryableAsync();
        var rows = await AsyncExecuter.ToListAsync(query.Where(x => practiceSetIds.Contains(x.PracticeSetId)));
        var readinessMap = await _questionRuntimeReadinessProvider.GetReadinessMapAsync(rows.Select(x => x.QuestionId).Distinct().ToList());

        foreach (var group in rows.GroupBy(x => x.PracticeSetId))
        {
            result[group.Key] = new AssignmentReadiness(
                group.Count(),
                group.Count(x => readinessMap.GetValueOrDefault(x.QuestionId)));
        }

        return result;
    }

    private sealed class AssignmentReadiness
    {
        public static AssignmentReadiness Empty { get; } = new(0, 0);

        public AssignmentReadiness(int assignedQuestionCount, int validAssignedQuestionCount)
        {
            AssignedQuestionCount = assignedQuestionCount;
            ValidAssignedQuestionCount = validAssignedQuestionCount;
        }

        public int AssignedQuestionCount { get; }

        public int ValidAssignedQuestionCount { get; }
    }
}

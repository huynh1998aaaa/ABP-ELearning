using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Permissions;
using Elearning.Questions;
using Elearning.QuestionTypes;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Elearning.Practices;

[Authorize(ElearningPermissions.Practices.Default)]
public class PracticeSetAppService : ElearningAppService, IPracticeSetAppService
{
    private readonly IRepository<PracticeSet, Guid> _practiceSetRepository;
    private readonly IRepository<PracticeAutoQuestionRule, Guid> _practiceAutoQuestionRuleRepository;
    private readonly IRepository<PracticeQuestion, Guid> _practiceQuestionRepository;
    private readonly IRepository<Question, Guid> _questionRepository;
    private readonly IRepository<QuestionType, Guid> _questionTypeRepository;
    private readonly IGuidGenerator _guidGenerator;

    public PracticeSetAppService(
        IRepository<PracticeSet, Guid> practiceSetRepository,
        IRepository<PracticeAutoQuestionRule, Guid> practiceAutoQuestionRuleRepository,
        IRepository<PracticeQuestion, Guid> practiceQuestionRepository,
        IRepository<Question, Guid> questionRepository,
        IRepository<QuestionType, Guid> questionTypeRepository,
        IGuidGenerator guidGenerator)
    {
        _practiceSetRepository = practiceSetRepository;
        _practiceAutoQuestionRuleRepository = practiceAutoQuestionRuleRepository;
        _practiceQuestionRepository = practiceQuestionRepository;
        _questionRepository = questionRepository;
        _questionTypeRepository = questionTypeRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task<PagedResultDto<PracticeSetDto>> GetListAsync(GetPracticeSetListInput input)
    {
        var query = await _practiceSetRepository.GetQueryableAsync();

        if (!input.Filter.IsNullOrWhiteSpace())
        {
            var filter = input.Filter!.Trim();
            query = query.Where(x =>
                x.Code.Contains(filter) ||
                x.Title.Contains(filter) ||
                (x.Description != null && x.Description.Contains(filter)));
        }

        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }

        if (input.AccessLevel.HasValue)
        {
            query = query.Where(x => x.AccessLevel == input.AccessLevel.Value);
        }

        if (input.SelectionMode.HasValue)
        {
            query = query.Where(x => x.SelectionMode == input.SelectionMode.Value);
        }

        if (input.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == input.IsActive.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(query);
        var practiceSets = await AsyncExecuter.ToListAsync(ApplySorting(query, input.Sorting)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount));

        var questionCounts = await GetQuestionCountsAsync(practiceSets.Select(x => x.Id).ToList());

        return new PagedResultDto<PracticeSetDto>(
            totalCount,
            practiceSets.Select(x => MapToDto(x, questionCounts.GetValueOrDefault(x.Id))).ToList());
    }

    public async Task<PracticeSetDto> GetAsync(Guid id)
    {
        var practiceSet = await _practiceSetRepository.GetAsync(id);
        var questionCount = await GetQuestionCountAsync(id);
        return MapToDto(practiceSet, questionCount);
    }

    [Authorize(ElearningPermissions.Practices.Create)]
    public async Task<PracticeSetDto> CreateAsync(CreatePracticeSetDto input)
    {
        var normalizedCode = NormalizeCode(input.Code);
        await EnsureUniqueCodeAsync(normalizedCode);

        var practiceSet = new PracticeSet(
            _guidGenerator.Create(),
            normalizedCode,
            input.Title,
            input.AccessLevel,
            input.SelectionMode,
            input.TotalQuestionCount,
            input.SortOrder,
            input.Description,
            input.ShuffleQuestions,
            input.ShowExplanation);

        if (!input.IsActive)
        {
            practiceSet.Deactivate();
        }

        await _practiceSetRepository.InsertAsync(practiceSet, autoSave: true);
        return MapToDto(practiceSet, 0);
    }

    [Authorize(ElearningPermissions.Practices.Update)]
    public async Task<PracticeSetDto> UpdateAsync(Guid id, UpdatePracticeSetDto input)
    {
        var practiceSet = await _practiceSetRepository.GetAsync(id);
        var normalizedCode = NormalizeCode(input.Code);
        await EnsureUniqueCodeAsync(normalizedCode, id);

        practiceSet.UpdateDetails(
            normalizedCode,
            input.Title,
            input.Description,
            input.AccessLevel,
            input.SelectionMode,
            input.TotalQuestionCount,
            input.ShuffleQuestions,
            input.ShowExplanation,
            input.SortOrder);

        await _practiceSetRepository.UpdateAsync(practiceSet, autoSave: true);
        return MapToDto(practiceSet, await GetQuestionCountAsync(id));
    }

    [Authorize(ElearningPermissions.Practices.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var practiceSet = await _practiceSetRepository.GetAsync(id);
        if (practiceSet.Status != PracticeStatus.Draft)
        {
            practiceSet.Archive(Clock.Now);
            await _practiceSetRepository.UpdateAsync(practiceSet, autoSave: true);
            return;
        }

        var practiceQuestions = await GetPracticeQuestionsByPracticeSetIdAsync(id);
        foreach (var practiceQuestion in practiceQuestions)
        {
            await _practiceQuestionRepository.DeleteAsync(practiceQuestion);
        }

        await _practiceSetRepository.DeleteAsync(practiceSet);
    }

    [Authorize(ElearningPermissions.Practices.Publish)]
    public async Task PublishAsync(Guid id)
    {
        var practiceSet = await _practiceSetRepository.GetAsync(id);
        var questionCount = await GetQuestionCountAsync(id);
        EnsureCanPublish(practiceSet);

        if (questionCount == 0)
        {
            throw new UserFriendlyException(L["Practices:CannotPublishWithoutQuestions"]);
        }

        if (questionCount < practiceSet.TotalQuestionCount)
        {
            throw new UserFriendlyException(L["Practices:AssignedQuestionsBelowTarget", practiceSet.TotalQuestionCount, questionCount]);
        }

        if (practiceSet.SelectionMode == PracticeSelectionMode.Random && practiceSet.TotalQuestionCount > questionCount)
        {
            throw new UserFriendlyException(L["Practices:RandomCountExceedsPool", practiceSet.TotalQuestionCount, questionCount]);
        }

        await EnsureAssignedQuestionsCanBePublishedAsync(id);
        practiceSet.Publish(Clock.Now);
        await _practiceSetRepository.UpdateAsync(practiceSet, autoSave: true);
    }

    [Authorize(ElearningPermissions.Practices.Publish)]
    public async Task ArchiveAsync(Guid id)
    {
        var practiceSet = await _practiceSetRepository.GetAsync(id);
        practiceSet.Archive(Clock.Now);
        await _practiceSetRepository.UpdateAsync(practiceSet, autoSave: true);
    }

    [Authorize(ElearningPermissions.Practices.Update)]
    public async Task ActivateAsync(Guid id)
    {
        var practiceSet = await _practiceSetRepository.GetAsync(id);
        EnsureCanActivate(practiceSet);
        practiceSet.Activate();
        await _practiceSetRepository.UpdateAsync(practiceSet, autoSave: true);
    }

    [Authorize(ElearningPermissions.Practices.Update)]
    public async Task DeactivateAsync(Guid id)
    {
        var practiceSet = await _practiceSetRepository.GetAsync(id);
        practiceSet.Deactivate();
        await _practiceSetRepository.UpdateAsync(practiceSet, autoSave: true);
    }

    public async Task<PracticeSetPreviewDto> GetPreviewAsync(Guid id)
    {
        return new PracticeSetPreviewDto
        {
            PracticeSet = await GetAsync(id),
            Questions = (await GetQuestionsAsync(id, new GetPracticeQuestionListInput
            {
                MaxResultCount = PracticeSetConsts.MaxQuestionCount,
                SkipCount = 0
            })).Items.ToList()
        };
    }

    public async Task<PagedResultDto<PracticeQuestionDto>> GetQuestionsAsync(Guid practiceSetId, GetPracticeQuestionListInput input)
    {
        await _practiceSetRepository.GetAsync(practiceSetId);

        var practiceQuestionQuery = await _practiceQuestionRepository.GetQueryableAsync();
        var practiceQuestions = await AsyncExecuter.ToListAsync(practiceQuestionQuery
            .Where(x => x.PracticeSetId == practiceSetId)
            .OrderBy(x => x.SortOrder));

        var questionMap = await GetQuestionMapAsync(practiceQuestions.Select(x => x.QuestionId).ToList());
        var typeMap = await GetQuestionTypeMapAsync(questionMap.Values.Select(x => x.QuestionTypeId).Distinct().ToList());

        var mapped = practiceQuestions
            .Select(x => MapToDto(x, questionMap.GetValueOrDefault(x.QuestionId), typeMap))
            .Where(x => input.Filter.IsNullOrWhiteSpace()
                || x.QuestionTitle.Contains(input.Filter!.Trim())
                || x.QuestionContent.Contains(input.Filter!.Trim())
                || x.QuestionTypeName.Contains(input.Filter!.Trim()))
            .ToList();

        var totalCount = mapped.Count;
        mapped = ApplyPracticeQuestionSorting(mapped, input.Sorting)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        return new PagedResultDto<PracticeQuestionDto>(totalCount, mapped);
    }

    [Authorize(ElearningPermissions.Practices.ManageQuestions)]
    public async Task<PagedResultDto<PracticeAvailableQuestionDto>> GetAvailableQuestionsAsync(Guid practiceSetId, GetPracticeAvailableQuestionListInput input)
    {
        await _practiceSetRepository.GetAsync(practiceSetId);

        var assignedQuestionIds = (await GetPracticeQuestionsByPracticeSetIdAsync(practiceSetId)).Select(x => x.QuestionId).ToList();
        var questionQuery = await _questionRepository.GetQueryableAsync();
        questionQuery = questionQuery.Where(x =>
            x.IsActive &&
            x.Status == QuestionStatus.Published &&
            !assignedQuestionIds.Contains(x.Id));

        if (!input.Filter.IsNullOrWhiteSpace())
        {
            var filter = input.Filter!.Trim();
            questionQuery = questionQuery.Where(x =>
                x.Title.Contains(filter) ||
                x.Content.Contains(filter) ||
                (x.Explanation != null && x.Explanation.Contains(filter)));
        }

        var totalCount = await AsyncExecuter.CountAsync(questionQuery);
        var questions = await AsyncExecuter.ToListAsync(ApplyQuestionSorting(questionQuery, input.Sorting)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount));

        var typeMap = await GetQuestionTypeMapAsync(questions.Select(x => x.QuestionTypeId).Distinct().ToList());

        return new PagedResultDto<PracticeAvailableQuestionDto>(
            totalCount,
            questions.Select(x => new PracticeAvailableQuestionDto
            {
                Id = x.Id,
                Title = x.Title,
                Content = x.Content,
                QuestionTypeName = typeMap.GetValueOrDefault(x.QuestionTypeId)?.DisplayName ?? string.Empty,
                Difficulty = x.Difficulty,
                Status = x.Status,
                IsActive = x.IsActive,
                Score = x.Score
            }).ToList());
    }

    [Authorize(ElearningPermissions.Practices.ManageQuestions)]
    public async Task<PracticeQuestionDto> AddQuestionAsync(Guid practiceSetId, AddPracticeQuestionDto input)
    {
        await _practiceSetRepository.GetAsync(practiceSetId);
        var question = await _questionRepository.GetAsync(input.QuestionId);
        EnsureQuestionCanBeUsed(question);

        var practiceQuestionQuery = await _practiceQuestionRepository.GetQueryableAsync();
        if (await AsyncExecuter.AnyAsync(practiceQuestionQuery.Where(x => x.PracticeSetId == practiceSetId && x.QuestionId == input.QuestionId)))
        {
            throw new UserFriendlyException(L["Practices:QuestionAlreadyAssigned"]);
        }

        var nextSortOrder = input.SortOrder == 0
            ? await GetNextPracticeQuestionSortOrderAsync(practiceSetId)
            : input.SortOrder;
        var practiceQuestion = new PracticeQuestion(
            _guidGenerator.Create(),
            practiceSetId,
            input.QuestionId,
            nextSortOrder,
            input.IsRequired);

        await _practiceQuestionRepository.InsertAsync(practiceQuestion, autoSave: true);
        var typeMap = await GetQuestionTypeMapAsync(new List<Guid> { question.QuestionTypeId });
        return MapToDto(practiceQuestion, question, typeMap);
    }

    [Authorize(ElearningPermissions.Practices.ManageQuestions)]
    public async Task<PracticeQuestionDto> UpdateQuestionAsync(Guid practiceQuestionId, UpdatePracticeQuestionDto input)
    {
        var practiceQuestion = await _practiceQuestionRepository.GetAsync(practiceQuestionId);
        practiceQuestion.UpdateDetails(input.SortOrder, input.IsRequired);
        await _practiceQuestionRepository.UpdateAsync(practiceQuestion, autoSave: true);

        var question = await _questionRepository.GetAsync(practiceQuestion.QuestionId);
        var typeMap = await GetQuestionTypeMapAsync(new List<Guid> { question.QuestionTypeId });
        return MapToDto(practiceQuestion, question, typeMap);
    }

    [Authorize(ElearningPermissions.Practices.ManageQuestions)]
    public async Task RemoveQuestionAsync(Guid practiceQuestionId)
    {
        await _practiceQuestionRepository.DeleteAsync(practiceQuestionId);
    }

    [Authorize(ElearningPermissions.Practices.ManageQuestions)]
    public async Task ReorderQuestionsAsync(Guid practiceSetId, List<Guid> practiceQuestionIds)
    {
        await _practiceSetRepository.GetAsync(practiceSetId);
        var practiceQuestions = await GetPracticeQuestionsByPracticeSetIdAsync(practiceSetId);
        var practiceQuestionMap = practiceQuestions.ToDictionary(x => x.Id);

        for (var index = 0; index < practiceQuestionIds.Count; index++)
        {
            if (practiceQuestionMap.TryGetValue(practiceQuestionIds[index], out var practiceQuestion))
            {
                practiceQuestion.UpdateDetails((index + 1) * 10, practiceQuestion.IsRequired);
                await _practiceQuestionRepository.UpdateAsync(practiceQuestion);
            }
        }
    }

    [Authorize(ElearningPermissions.Practices.ManageQuestions)]
    public async Task<List<PracticeAutoQuestionRuleDto>> GetAutoQuestionRulesAsync(Guid practiceSetId)
    {
        await _practiceSetRepository.GetAsync(practiceSetId);
        var rules = await GetAutoQuestionRulesByPracticeSetIdAsync(practiceSetId);
        var typeMap = await GetQuestionTypeMapAsync(rules.Select(x => x.QuestionTypeId).Distinct().ToList());
        return rules.Select(x => MapToDto(x, typeMap)).ToList();
    }

    [Authorize(ElearningPermissions.Practices.ManageQuestions)]
    public async Task<PracticeAutoQuestionRuleDto> AddAutoQuestionRuleAsync(Guid practiceSetId, CreatePracticeAutoQuestionRuleDto input)
    {
        var practiceSet = await _practiceSetRepository.GetAsync(practiceSetId);
        EnsureCanManageAutoRules(practiceSet);
        var questionType = await GetActiveQuestionTypeAsync(input.QuestionTypeId);

        var rule = new PracticeAutoQuestionRule(
            _guidGenerator.Create(),
            practiceSetId,
            questionType.Id,
            input.TargetCount,
            input.SortOrder,
            input.Difficulty);

        await _practiceAutoQuestionRuleRepository.InsertAsync(rule, autoSave: true);
        return MapToDto(rule, new Dictionary<Guid, QuestionType> { [questionType.Id] = questionType });
    }

    [Authorize(ElearningPermissions.Practices.ManageQuestions)]
    public async Task<PracticeAutoQuestionRuleDto> UpdateAutoQuestionRuleAsync(Guid ruleId, UpdatePracticeAutoQuestionRuleDto input)
    {
        var rule = await _practiceAutoQuestionRuleRepository.GetAsync(ruleId);
        var practiceSet = await _practiceSetRepository.GetAsync(rule.PracticeSetId);
        EnsureCanManageAutoRules(practiceSet);
        var questionType = await GetActiveQuestionTypeAsync(input.QuestionTypeId);

        rule.UpdateDetails(questionType.Id, input.TargetCount, input.SortOrder, input.Difficulty);
        await _practiceAutoQuestionRuleRepository.UpdateAsync(rule, autoSave: true);
        return MapToDto(rule, new Dictionary<Guid, QuestionType> { [questionType.Id] = questionType });
    }

    [Authorize(ElearningPermissions.Practices.ManageQuestions)]
    public async Task RemoveAutoQuestionRuleAsync(Guid ruleId)
    {
        var rule = await _practiceAutoQuestionRuleRepository.GetAsync(ruleId);
        var practiceSet = await _practiceSetRepository.GetAsync(rule.PracticeSetId);
        EnsureCanManageAutoRules(practiceSet);
        await _practiceAutoQuestionRuleRepository.DeleteAsync(rule);
    }

    [Authorize(ElearningPermissions.Practices.ManageQuestions)]
    public async Task<PracticeAutoAssignmentPreviewDto> PreviewAutoAssignAsync(Guid practiceSetId)
    {
        await _practiceSetRepository.GetAsync(practiceSetId);
        var rules = await GetAutoQuestionRulesByPracticeSetIdAsync(practiceSetId);
        var plan = await BuildAutoAssignmentPlanAsync(practiceSetId, rules);
        return plan.Preview;
    }

    [Authorize(ElearningPermissions.Practices.ManageQuestions)]
    public async Task<PracticeAutoAssignmentResultDto> ApplyAutoAssignAsync(Guid practiceSetId)
    {
        var practiceSet = await _practiceSetRepository.GetAsync(practiceSetId);
        EnsureCanApplyAutoAssignment(practiceSet);

        var rules = await GetAutoQuestionRulesByPracticeSetIdAsync(practiceSetId);
        if (rules.Count == 0)
        {
            throw new UserFriendlyException(L["Practices:AutoAssignNoRules"]);
        }

        var plan = await BuildAutoAssignmentPlanAsync(practiceSetId, rules);
        var practiceQuestions = await GetPracticeQuestionsByPracticeSetIdAsync(practiceSetId);
        var autoAssignedQuestions = practiceQuestions
            .Where(x => x.AssignmentSource == QuestionAssignmentSource.Auto)
            .ToList();

        foreach (var autoAssignedQuestion in autoAssignedQuestions)
        {
            await _practiceQuestionRepository.DeleteAsync(autoAssignedQuestion);
        }

        var nextSortOrder = await GetNextPracticeQuestionSortOrderAsync(practiceSetId);
        foreach (var question in plan.SelectedQuestions)
        {
            await _practiceQuestionRepository.InsertAsync(new PracticeQuestion(
                _guidGenerator.Create(),
                practiceSetId,
                question.Id,
                nextSortOrder,
                assignmentSource: QuestionAssignmentSource.Auto), autoSave: true);

            nextSortOrder += 10;
        }

        return new PracticeAutoAssignmentResultDto
        {
            RequestedCount = plan.Preview.RequestedCount,
            FulfilledByManualCount = plan.Preview.FulfilledByManualCount,
            AssignedCount = plan.SelectedQuestions.Count,
            ReplacedAutoAssignedCount = autoAssignedQuestions.Count,
            PreservedManualCount = plan.Preview.PreservedManualCount,
            ShortageCount = plan.Preview.ShortageCount
        };
    }

    private async Task EnsureUniqueCodeAsync(string code, Guid? excludedId = null)
    {
        var query = await _practiceSetRepository.GetQueryableAsync();
        query = query.Where(x => x.Code == code);
        if (excludedId.HasValue)
        {
            query = query.Where(x => x.Id != excludedId.Value);
        }

        if (await AsyncExecuter.AnyAsync(query))
        {
            throw new UserFriendlyException(L["Practices:CodeAlreadyExists", code]);
        }
    }

    private void EnsureCanManageAutoRules(PracticeSet practiceSet)
    {
        if (practiceSet.Status == PracticeStatus.Archived)
        {
            throw new UserFriendlyException(L["Practices:ArchivedAutoRulesCannotBeChanged"]);
        }
    }

    private void EnsureCanApplyAutoAssignment(PracticeSet practiceSet)
    {
        EnsureCanManageAutoRules(practiceSet);
    }

    private void EnsureCanActivate(PracticeSet practiceSet)
    {
        if (practiceSet.Status == PracticeStatus.Archived)
        {
            throw new UserFriendlyException(L["Practices:ArchivedCannotBeActivated"]);
        }
    }

    private void EnsureCanPublish(PracticeSet practiceSet)
    {
        if (practiceSet.Status == PracticeStatus.Archived)
        {
            throw new UserFriendlyException(L["Practices:ArchivedCannotBePublished"]);
        }

        if (!practiceSet.IsActive)
        {
            throw new UserFriendlyException(L["Practices:InactiveCannotBePublished"]);
        }
    }

    private void EnsureQuestionCanBeUsed(Question question)
    {
        if (!question.IsActive)
        {
            throw new UserFriendlyException(L["Practices:InactiveQuestionCannotBeUsed"]);
        }

        if (question.Status != QuestionStatus.Published)
        {
            throw new UserFriendlyException(L["Practices:UnpublishedQuestionCannotBeUsed"]);
        }
    }

    private async Task EnsureAssignedQuestionsCanBePublishedAsync(Guid practiceSetId)
    {
        var practiceQuestions = await GetPracticeQuestionsByPracticeSetIdAsync(practiceSetId);
        if (practiceQuestions.Count == 0)
        {
            return;
        }

        var questionMap = await GetQuestionMapAsync(practiceQuestions.Select(x => x.QuestionId).Distinct().ToList());
        if (practiceQuestions.Any(x =>
                !questionMap.TryGetValue(x.QuestionId, out var question) ||
                !question.IsActive ||
                question.Status != QuestionStatus.Published))
        {
            throw new UserFriendlyException(L["Practices:AssignedQuestionsMustBePublished"]);
        }
    }

    private async Task<int> GetQuestionCountAsync(Guid practiceSetId)
    {
        var query = await _practiceQuestionRepository.GetQueryableAsync();
        return await AsyncExecuter.CountAsync(query.Where(x => x.PracticeSetId == practiceSetId));
    }

    private async Task<List<PracticeAutoQuestionRule>> GetAutoQuestionRulesByPracticeSetIdAsync(Guid practiceSetId)
    {
        var query = await _practiceAutoQuestionRuleRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query
            .Where(x => x.PracticeSetId == practiceSetId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreationTime));
    }

    private async Task<Dictionary<Guid, int>> GetQuestionCountsAsync(IReadOnlyList<Guid> practiceSetIds)
    {
        if (practiceSetIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var query = await _practiceQuestionRepository.GetQueryableAsync();
        var practiceQuestions = await AsyncExecuter.ToListAsync(query.Where(x => practiceSetIds.Contains(x.PracticeSetId)));
        return practiceQuestions
            .GroupBy(x => x.PracticeSetId)
            .ToDictionary(x => x.Key, x => x.Count());
    }

    private async Task<List<PracticeQuestion>> GetPracticeQuestionsByPracticeSetIdAsync(Guid practiceSetId)
    {
        var query = await _practiceQuestionRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query.Where(x => x.PracticeSetId == practiceSetId).OrderBy(x => x.SortOrder));
    }

    private async Task<int> GetNextPracticeQuestionSortOrderAsync(Guid practiceSetId)
    {
        var practiceQuestions = await GetPracticeQuestionsByPracticeSetIdAsync(practiceSetId);
        return practiceQuestions.Count == 0 ? 10 : practiceQuestions.Max(x => x.SortOrder) + 10;
    }

    private async Task<QuestionType> GetActiveQuestionTypeAsync(Guid questionTypeId)
    {
        var questionType = await _questionTypeRepository.GetAsync(questionTypeId);
        if (!questionType.IsActive)
        {
            throw new UserFriendlyException(L["Practices:InactiveQuestionTypeCannotBeUsed"]);
        }

        return questionType;
    }

    private async Task<Dictionary<Guid, Question>> GetQuestionMapAsync(IReadOnlyList<Guid> questionIds)
    {
        if (questionIds.Count == 0)
        {
            return new Dictionary<Guid, Question>();
        }

        var query = await _questionRepository.GetQueryableAsync();
        var questions = await AsyncExecuter.ToListAsync(query.Where(x => questionIds.Contains(x.Id)));
        return questions.ToDictionary(x => x.Id);
    }

    private async Task<Dictionary<Guid, QuestionType>> GetQuestionTypeMapAsync(IReadOnlyList<Guid> questionTypeIds)
    {
        if (questionTypeIds.Count == 0)
        {
            return new Dictionary<Guid, QuestionType>();
        }

        var query = await _questionTypeRepository.GetQueryableAsync();
        var questionTypes = await AsyncExecuter.ToListAsync(query.Where(x => questionTypeIds.Contains(x.Id)));
        return questionTypes.ToDictionary(x => x.Id);
    }

    private async Task<PracticeAutoAssignmentPlan> BuildAutoAssignmentPlanAsync(Guid practiceSetId, IReadOnlyList<PracticeAutoQuestionRule> rules)
    {
        var typeMap = await GetQuestionTypeMapAsync(rules.Select(x => x.QuestionTypeId).Distinct().ToList());
        var assignedQuestions = await GetPracticeQuestionsByPracticeSetIdAsync(practiceSetId);
        var manualAssignedQuestions = assignedQuestions
            .Where(x => x.AssignmentSource == QuestionAssignmentSource.Manual)
            .ToList();
        var autoAssignedQuestions = assignedQuestions
            .Where(x => x.AssignmentSource == QuestionAssignmentSource.Auto)
            .ToList();

        var manualQuestionMap = await GetQuestionMapAsync(manualAssignedQuestions.Select(x => x.QuestionId).Distinct().ToList());
        var remainingManualQuestions = manualAssignedQuestions
            .Where(x => manualQuestionMap.ContainsKey(x.QuestionId))
            .Select(x => new ManualAssignmentQuestionRef<PracticeQuestion>
            {
                Assignment = x,
                Question = manualQuestionMap[x.QuestionId]
            })
            .OrderBy(x => x.Assignment.SortOrder)
            .ToList();

        var excludedQuestionIds = remainingManualQuestions
            .Select(x => x.Question.Id)
            .ToHashSet();

        var plan = new PracticeAutoAssignmentPlan
        {
            Preview = new PracticeAutoAssignmentPreviewDto
            {
                ExistingAutoAssignedCount = autoAssignedQuestions.Count,
                PreservedManualCount = manualAssignedQuestions.Count
            }
        };

        foreach (var rule in rules)
        {
            var fulfilledByManual = remainingManualQuestions
                .Where(x => MatchesRule(x.Question, rule))
                .Take(rule.TargetCount)
                .ToList();

            foreach (var manualQuestion in fulfilledByManual)
            {
                remainingManualQuestions.Remove(manualQuestion);
            }

            var remainingTargetCount = Math.Max(0, rule.TargetCount - fulfilledByManual.Count);
            var query = await _questionRepository.GetQueryableAsync();
            query = query.Where(x =>
                x.IsActive &&
                x.Status == QuestionStatus.Published &&
                x.QuestionTypeId == rule.QuestionTypeId &&
                !excludedQuestionIds.Contains(x.Id));

            if (rule.Difficulty.HasValue)
            {
                query = query.Where(x => x.Difficulty == rule.Difficulty.Value);
            }

            var selectedQuestions = await AsyncExecuter.ToListAsync(query
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CreationTime)
                .Take(remainingTargetCount));

            foreach (var selectedQuestion in selectedQuestions)
            {
                excludedQuestionIds.Add(selectedQuestion.Id);
            }

            plan.SelectedQuestions.AddRange(selectedQuestions);
            plan.Preview.Items.Add(new PracticeAutoAssignmentRulePreviewDto
            {
                RuleId = rule.Id,
                QuestionTypeId = rule.QuestionTypeId,
                QuestionTypeName = typeMap.GetValueOrDefault(rule.QuestionTypeId)?.DisplayName ?? string.Empty,
                Difficulty = rule.Difficulty,
                RequestedCount = rule.TargetCount,
                FulfilledByManualCount = fulfilledByManual.Count,
                AutoAssignableCount = selectedQuestions.Count,
                ShortageCount = Math.Max(0, rule.TargetCount - fulfilledByManual.Count - selectedQuestions.Count)
            });
        }

        plan.Preview.RequestedCount = plan.Preview.Items.Sum(x => x.RequestedCount);
        plan.Preview.FulfilledByManualCount = plan.Preview.Items.Sum(x => x.FulfilledByManualCount);
        plan.Preview.AutoAssignableCount = plan.Preview.Items.Sum(x => x.AutoAssignableCount);
        plan.Preview.ShortageCount = plan.Preview.Items.Sum(x => x.ShortageCount);

        return plan;
    }

    private static bool MatchesRule(Question question, PracticeAutoQuestionRule rule)
    {
        return question.QuestionTypeId == rule.QuestionTypeId
            && (!rule.Difficulty.HasValue || question.Difficulty == rule.Difficulty.Value);
    }

    private static string NormalizeCode(string code)
    {
        return Check.NotNullOrWhiteSpace(code, nameof(code), PracticeSetConsts.MaxCodeLength).Trim();
    }

    private static IQueryable<PracticeSet> ApplySorting(IQueryable<PracticeSet> query, string? sorting)
    {
        return sorting?.Trim().ToLowerInvariant() switch
        {
            "title" => query.OrderBy(x => x.Title),
            "title desc" => query.OrderByDescending(x => x.Title),
            "code" => query.OrderBy(x => x.Code),
            "code desc" => query.OrderByDescending(x => x.Code),
            "sortorder desc" => query.OrderByDescending(x => x.SortOrder).ThenBy(x => x.Title),
            _ => query.OrderBy(x => x.SortOrder).ThenByDescending(x => x.CreationTime)
        };
    }

    private static IQueryable<Question> ApplyQuestionSorting(IQueryable<Question> query, string? sorting)
    {
        return sorting?.Trim().ToLowerInvariant() switch
        {
            "title" => query.OrderBy(x => x.Title),
            "title desc" => query.OrderByDescending(x => x.Title),
            _ => query.OrderBy(x => x.SortOrder).ThenByDescending(x => x.CreationTime)
        };
    }

    private static IEnumerable<PracticeQuestionDto> ApplyPracticeQuestionSorting(IEnumerable<PracticeQuestionDto> practiceQuestions, string? sorting)
    {
        return sorting?.Trim().ToLowerInvariant() switch
        {
            "questiontitle" => practiceQuestions.OrderBy(x => x.QuestionTitle),
            "questiontitle desc" => practiceQuestions.OrderByDescending(x => x.QuestionTitle),
            "sortorder desc" => practiceQuestions.OrderByDescending(x => x.SortOrder),
            _ => practiceQuestions.OrderBy(x => x.SortOrder).ThenBy(x => x.QuestionTitle)
        };
    }

    private static PracticeSetDto MapToDto(PracticeSet practiceSet, int assignedQuestionCount)
    {
        return new PracticeSetDto
        {
            Id = practiceSet.Id,
            Code = practiceSet.Code,
            Title = practiceSet.Title,
            Description = practiceSet.Description,
            Status = practiceSet.Status,
            AccessLevel = practiceSet.AccessLevel,
            SelectionMode = practiceSet.SelectionMode,
            TotalQuestionCount = practiceSet.TotalQuestionCount,
            AssignedQuestionCount = assignedQuestionCount,
            ShuffleQuestions = practiceSet.ShuffleQuestions,
            ShowExplanation = practiceSet.ShowExplanation,
            IsActive = practiceSet.IsActive,
            SortOrder = practiceSet.SortOrder,
            PublishedTime = practiceSet.PublishedTime,
            ArchivedTime = practiceSet.ArchivedTime,
            CreationTime = practiceSet.CreationTime,
            CreatorId = practiceSet.CreatorId,
            LastModificationTime = practiceSet.LastModificationTime,
            LastModifierId = practiceSet.LastModifierId,
            IsDeleted = practiceSet.IsDeleted,
            DeleterId = practiceSet.DeleterId,
            DeletionTime = practiceSet.DeletionTime
        };
    }

    private static PracticeQuestionDto MapToDto(
        PracticeQuestion practiceQuestion,
        Question? question,
        IReadOnlyDictionary<Guid, QuestionType> questionTypeMap)
    {
        QuestionType? questionType = null;
        if (question != null)
        {
            questionTypeMap.TryGetValue(question.QuestionTypeId, out questionType);
        }

        return new PracticeQuestionDto
        {
            Id = practiceQuestion.Id,
            PracticeSetId = practiceQuestion.PracticeSetId,
            QuestionId = practiceQuestion.QuestionId,
            QuestionTitle = question?.Title ?? string.Empty,
            QuestionContent = question?.Content ?? string.Empty,
            QuestionTypeName = questionType?.DisplayName ?? string.Empty,
            QuestionDifficulty = question?.Difficulty ?? QuestionDifficulty.Medium,
            QuestionStatus = question?.Status ?? QuestionStatus.Draft,
            QuestionIsActive = question?.IsActive ?? false,
            QuestionScore = question?.Score ?? 0,
            SortOrder = practiceQuestion.SortOrder,
            IsRequired = practiceQuestion.IsRequired,
            AssignmentSource = practiceQuestion.AssignmentSource,
            CreationTime = practiceQuestion.CreationTime,
            CreatorId = practiceQuestion.CreatorId,
            LastModificationTime = practiceQuestion.LastModificationTime,
            LastModifierId = practiceQuestion.LastModifierId,
            IsDeleted = practiceQuestion.IsDeleted,
            DeleterId = practiceQuestion.DeleterId,
            DeletionTime = practiceQuestion.DeletionTime
        };
    }

    private static PracticeAutoQuestionRuleDto MapToDto(
        PracticeAutoQuestionRule rule,
        IReadOnlyDictionary<Guid, QuestionType> questionTypeMap)
    {
        return new PracticeAutoQuestionRuleDto
        {
            Id = rule.Id,
            PracticeSetId = rule.PracticeSetId,
            QuestionTypeId = rule.QuestionTypeId,
            QuestionTypeName = questionTypeMap.GetValueOrDefault(rule.QuestionTypeId)?.DisplayName ?? string.Empty,
            Difficulty = rule.Difficulty,
            TargetCount = rule.TargetCount,
            SortOrder = rule.SortOrder,
            CreationTime = rule.CreationTime,
            CreatorId = rule.CreatorId,
            LastModificationTime = rule.LastModificationTime,
            LastModifierId = rule.LastModifierId,
            IsDeleted = rule.IsDeleted,
            DeleterId = rule.DeleterId,
            DeletionTime = rule.DeletionTime
        };
    }

    private sealed class PracticeAutoAssignmentPlan
    {
        public PracticeAutoAssignmentPreviewDto Preview { get; init; } = new();

        public List<Question> SelectedQuestions { get; } = [];
    }

    private sealed class ManualAssignmentQuestionRef<TAssignment>
    {
        public required TAssignment Assignment { get; init; }

        public required Question Question { get; init; }
    }
}

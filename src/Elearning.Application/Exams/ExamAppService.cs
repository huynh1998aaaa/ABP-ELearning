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

namespace Elearning.Exams;

[Authorize(ElearningPermissions.Exams.Default)]
public class ExamAppService : ElearningAppService, IExamAppService
{
    private readonly IRepository<Exam, Guid> _examRepository;
    private readonly IRepository<ExamAutoQuestionRule, Guid> _examAutoQuestionRuleRepository;
    private readonly IRepository<ExamQuestion, Guid> _examQuestionRepository;
    private readonly IRepository<Question, Guid> _questionRepository;
    private readonly IRepository<QuestionType, Guid> _questionTypeRepository;
    private readonly IGuidGenerator _guidGenerator;

    public ExamAppService(
        IRepository<Exam, Guid> examRepository,
        IRepository<ExamAutoQuestionRule, Guid> examAutoQuestionRuleRepository,
        IRepository<ExamQuestion, Guid> examQuestionRepository,
        IRepository<Question, Guid> questionRepository,
        IRepository<QuestionType, Guid> questionTypeRepository,
        IGuidGenerator guidGenerator)
    {
        _examRepository = examRepository;
        _examAutoQuestionRuleRepository = examAutoQuestionRuleRepository;
        _examQuestionRepository = examQuestionRepository;
        _questionRepository = questionRepository;
        _questionTypeRepository = questionTypeRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task<PagedResultDto<ExamDto>> GetListAsync(GetExamListInput input)
    {
        var query = await _examRepository.GetQueryableAsync();

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
        var exams = await AsyncExecuter.ToListAsync(ApplySorting(query, input.Sorting)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount));

        var questionCounts = await GetQuestionCountsAsync(exams.Select(x => x.Id).ToList());

        return new PagedResultDto<ExamDto>(
            totalCount,
            exams.Select(x => MapToDto(x, questionCounts.GetValueOrDefault(x.Id))).ToList());
    }

    public async Task<ExamDto> GetAsync(Guid id)
    {
        var exam = await _examRepository.GetAsync(id);
        var questionCount = await GetQuestionCountAsync(id);
        return MapToDto(exam, questionCount);
    }

    [Authorize(ElearningPermissions.Exams.Create)]
    public async Task<ExamDto> CreateAsync(CreateExamDto input)
    {
        var normalizedCode = NormalizeCode(input.Code);
        await EnsureUniqueCodeAsync(normalizedCode);

        var exam = new Exam(
            _guidGenerator.Create(),
            normalizedCode,
            input.Title,
            input.AccessLevel,
            input.SelectionMode,
            input.DurationMinutes,
            input.TotalQuestionCount,
            input.SortOrder,
            input.Description,
            input.PassingScore,
            input.ShuffleQuestions,
            input.ShuffleOptions);

        if (!input.IsActive)
        {
            exam.Deactivate();
        }

        await _examRepository.InsertAsync(exam, autoSave: true);
        return MapToDto(exam, 0);
    }

    [Authorize(ElearningPermissions.Exams.Update)]
    public async Task<ExamDto> UpdateAsync(Guid id, UpdateExamDto input)
    {
        var exam = await _examRepository.GetAsync(id);
        var normalizedCode = NormalizeCode(input.Code);
        await EnsureUniqueCodeAsync(normalizedCode, id);

        exam.UpdateDetails(
            normalizedCode,
            input.Title,
            input.Description,
            input.AccessLevel,
            input.SelectionMode,
            input.DurationMinutes,
            input.TotalQuestionCount,
            input.PassingScore,
            input.ShuffleQuestions,
            input.ShuffleOptions,
            input.SortOrder);

        await _examRepository.UpdateAsync(exam, autoSave: true);
        return MapToDto(exam, await GetQuestionCountAsync(id));
    }

    [Authorize(ElearningPermissions.Exams.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var exam = await _examRepository.GetAsync(id);
        if (exam.Status != ExamStatus.Draft)
        {
            exam.Archive(Clock.Now);
            await _examRepository.UpdateAsync(exam, autoSave: true);
            return;
        }

        var examQuestions = await GetExamQuestionsByExamIdAsync(id);
        foreach (var examQuestion in examQuestions)
        {
            await _examQuestionRepository.DeleteAsync(examQuestion);
        }

        await _examRepository.DeleteAsync(exam);
    }

    [Authorize(ElearningPermissions.Exams.Publish)]
    public async Task PublishAsync(Guid id)
    {
        var exam = await _examRepository.GetAsync(id);
        var questionCount = await GetQuestionCountAsync(id);
        EnsureCanPublish(exam);

        if (questionCount == 0)
        {
            throw new UserFriendlyException(L["Exams:CannotPublishWithoutQuestions"]);
        }

        if (questionCount < exam.TotalQuestionCount)
        {
            throw new UserFriendlyException(L["Exams:AssignedQuestionsBelowTarget", exam.TotalQuestionCount, questionCount]);
        }

        if (exam.SelectionMode == ExamSelectionMode.Random && exam.TotalQuestionCount > questionCount)
        {
            throw new UserFriendlyException(L["Exams:RandomCountExceedsPool", exam.TotalQuestionCount, questionCount]);
        }

        await EnsureAssignedQuestionsCanBePublishedAsync(id);
        exam.Publish(Clock.Now);
        await _examRepository.UpdateAsync(exam, autoSave: true);
    }

    [Authorize(ElearningPermissions.Exams.Publish)]
    public async Task ArchiveAsync(Guid id)
    {
        var exam = await _examRepository.GetAsync(id);
        exam.Archive(Clock.Now);
        await _examRepository.UpdateAsync(exam, autoSave: true);
    }

    [Authorize(ElearningPermissions.Exams.Update)]
    public async Task ActivateAsync(Guid id)
    {
        var exam = await _examRepository.GetAsync(id);
        EnsureCanActivate(exam);
        exam.Activate();
        await _examRepository.UpdateAsync(exam, autoSave: true);
    }

    [Authorize(ElearningPermissions.Exams.Update)]
    public async Task DeactivateAsync(Guid id)
    {
        var exam = await _examRepository.GetAsync(id);
        exam.Deactivate();
        await _examRepository.UpdateAsync(exam, autoSave: true);
    }

    public async Task<ExamPreviewDto> GetPreviewAsync(Guid id)
    {
        return new ExamPreviewDto
        {
            Exam = await GetAsync(id),
            Questions = (await GetQuestionsAsync(id, new GetExamQuestionListInput
            {
                MaxResultCount = ExamConsts.MaxQuestionCount,
                SkipCount = 0
            })).Items.ToList()
        };
    }

    public async Task<PagedResultDto<ExamQuestionDto>> GetQuestionsAsync(Guid examId, GetExamQuestionListInput input)
    {
        await _examRepository.GetAsync(examId);

        var examQuestionQuery = await _examQuestionRepository.GetQueryableAsync();
        var examQuestions = await AsyncExecuter.ToListAsync(examQuestionQuery
            .Where(x => x.ExamId == examId)
            .OrderBy(x => x.SortOrder));

        var questionMap = await GetQuestionMapAsync(examQuestions.Select(x => x.QuestionId).ToList());
        var typeMap = await GetQuestionTypeMapAsync(questionMap.Values.Select(x => x.QuestionTypeId).Distinct().ToList());

        var mapped = examQuestions
            .Select(x => MapToDto(x, questionMap.GetValueOrDefault(x.QuestionId), typeMap))
            .Where(x => input.Filter.IsNullOrWhiteSpace()
                || x.QuestionTitle.Contains(input.Filter!.Trim())
                || x.QuestionContent.Contains(input.Filter!.Trim())
                || x.QuestionTypeName.Contains(input.Filter!.Trim()))
            .ToList();

        var totalCount = mapped.Count;
        mapped = ApplyExamQuestionSorting(mapped, input.Sorting)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        return new PagedResultDto<ExamQuestionDto>(totalCount, mapped);
    }

    [Authorize(ElearningPermissions.Exams.ManageQuestions)]
    public async Task<PagedResultDto<ExamAvailableQuestionDto>> GetAvailableQuestionsAsync(Guid examId, GetExamAvailableQuestionListInput input)
    {
        await _examRepository.GetAsync(examId);

        var assignedQuestionIds = (await GetExamQuestionsByExamIdAsync(examId)).Select(x => x.QuestionId).ToList();
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

        return new PagedResultDto<ExamAvailableQuestionDto>(
            totalCount,
            questions.Select(x => new ExamAvailableQuestionDto
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

    [Authorize(ElearningPermissions.Exams.ManageQuestions)]
    public async Task<ExamQuestionDto> AddQuestionAsync(Guid examId, AddExamQuestionDto input)
    {
        await _examRepository.GetAsync(examId);
        var question = await _questionRepository.GetAsync(input.QuestionId);
        EnsureQuestionCanBeUsed(question);

        var examQuestionQuery = await _examQuestionRepository.GetQueryableAsync();
        if (await AsyncExecuter.AnyAsync(examQuestionQuery.Where(x => x.ExamId == examId && x.QuestionId == input.QuestionId)))
        {
            throw new UserFriendlyException(L["Exams:QuestionAlreadyAssigned"]);
        }

        var nextSortOrder = input.SortOrder == 0
            ? await GetNextExamQuestionSortOrderAsync(examId)
            : input.SortOrder;
        var examQuestion = new ExamQuestion(
            _guidGenerator.Create(),
            examId,
            input.QuestionId,
            nextSortOrder,
            input.ScoreOverride,
            input.IsRequired);

        await _examQuestionRepository.InsertAsync(examQuestion, autoSave: true);
        var typeMap = await GetQuestionTypeMapAsync(new List<Guid> { question.QuestionTypeId });
        return MapToDto(examQuestion, question, typeMap);
    }

    [Authorize(ElearningPermissions.Exams.ManageQuestions)]
    public async Task<ExamQuestionDto> UpdateQuestionAsync(Guid examQuestionId, UpdateExamQuestionDto input)
    {
        var examQuestion = await _examQuestionRepository.GetAsync(examQuestionId);
        examQuestion.UpdateDetails(input.SortOrder, input.ScoreOverride, input.IsRequired);
        await _examQuestionRepository.UpdateAsync(examQuestion, autoSave: true);

        var question = await _questionRepository.GetAsync(examQuestion.QuestionId);
        var typeMap = await GetQuestionTypeMapAsync(new List<Guid> { question.QuestionTypeId });
        return MapToDto(examQuestion, question, typeMap);
    }

    [Authorize(ElearningPermissions.Exams.ManageQuestions)]
    public async Task RemoveQuestionAsync(Guid examQuestionId)
    {
        await _examQuestionRepository.DeleteAsync(examQuestionId);
    }

    [Authorize(ElearningPermissions.Exams.ManageQuestions)]
    public async Task ReorderQuestionsAsync(Guid examId, List<Guid> examQuestionIds)
    {
        await _examRepository.GetAsync(examId);
        var examQuestions = await GetExamQuestionsByExamIdAsync(examId);
        var examQuestionMap = examQuestions.ToDictionary(x => x.Id);

        for (var index = 0; index < examQuestionIds.Count; index++)
        {
            if (examQuestionMap.TryGetValue(examQuestionIds[index], out var examQuestion))
            {
                examQuestion.UpdateDetails((index + 1) * 10, examQuestion.ScoreOverride, examQuestion.IsRequired);
                await _examQuestionRepository.UpdateAsync(examQuestion);
            }
        }
    }

    [Authorize(ElearningPermissions.Exams.ManageQuestions)]
    public async Task<List<ExamAutoQuestionRuleDto>> GetAutoQuestionRulesAsync(Guid examId)
    {
        await _examRepository.GetAsync(examId);
        var rules = await GetAutoQuestionRulesByExamIdAsync(examId);
        var typeMap = await GetQuestionTypeMapAsync(rules.Select(x => x.QuestionTypeId).Distinct().ToList());
        return rules.Select(x => MapToDto(x, typeMap)).ToList();
    }

    [Authorize(ElearningPermissions.Exams.ManageQuestions)]
    public async Task<ExamAutoQuestionRuleDto> AddAutoQuestionRuleAsync(Guid examId, CreateExamAutoQuestionRuleDto input)
    {
        var exam = await _examRepository.GetAsync(examId);
        EnsureCanManageAutoRules(exam);
        var questionType = await GetActiveQuestionTypeAsync(input.QuestionTypeId);

        var rule = new ExamAutoQuestionRule(
            _guidGenerator.Create(),
            examId,
            questionType.Id,
            input.TargetCount,
            input.SortOrder,
            input.Difficulty);

        await _examAutoQuestionRuleRepository.InsertAsync(rule, autoSave: true);
        return MapToDto(rule, new Dictionary<Guid, QuestionType> { [questionType.Id] = questionType });
    }

    [Authorize(ElearningPermissions.Exams.ManageQuestions)]
    public async Task<ExamAutoQuestionRuleDto> UpdateAutoQuestionRuleAsync(Guid ruleId, UpdateExamAutoQuestionRuleDto input)
    {
        var rule = await _examAutoQuestionRuleRepository.GetAsync(ruleId);
        var exam = await _examRepository.GetAsync(rule.ExamId);
        EnsureCanManageAutoRules(exam);
        var questionType = await GetActiveQuestionTypeAsync(input.QuestionTypeId);

        rule.UpdateDetails(questionType.Id, input.TargetCount, input.SortOrder, input.Difficulty);
        await _examAutoQuestionRuleRepository.UpdateAsync(rule, autoSave: true);
        return MapToDto(rule, new Dictionary<Guid, QuestionType> { [questionType.Id] = questionType });
    }

    [Authorize(ElearningPermissions.Exams.ManageQuestions)]
    public async Task RemoveAutoQuestionRuleAsync(Guid ruleId)
    {
        var rule = await _examAutoQuestionRuleRepository.GetAsync(ruleId);
        var exam = await _examRepository.GetAsync(rule.ExamId);
        EnsureCanManageAutoRules(exam);
        await _examAutoQuestionRuleRepository.DeleteAsync(rule);
    }

    [Authorize(ElearningPermissions.Exams.ManageQuestions)]
    public async Task<ExamAutoAssignmentPreviewDto> PreviewAutoAssignAsync(Guid examId)
    {
        await _examRepository.GetAsync(examId);
        var rules = await GetAutoQuestionRulesByExamIdAsync(examId);
        var plan = await BuildAutoAssignmentPlanAsync(examId, rules);
        return plan.Preview;
    }

    [Authorize(ElearningPermissions.Exams.ManageQuestions)]
    public async Task<ExamAutoAssignmentResultDto> ApplyAutoAssignAsync(Guid examId)
    {
        var exam = await _examRepository.GetAsync(examId);
        EnsureCanApplyAutoAssignment(exam);

        var rules = await GetAutoQuestionRulesByExamIdAsync(examId);
        if (rules.Count == 0)
        {
            throw new UserFriendlyException(L["Exams:AutoAssignNoRules"]);
        }

        var plan = await BuildAutoAssignmentPlanAsync(examId, rules);
        var examQuestions = await GetExamQuestionsByExamIdAsync(examId);
        var autoAssignedQuestions = examQuestions
            .Where(x => x.AssignmentSource == QuestionAssignmentSource.Auto)
            .ToList();

        foreach (var autoAssignedQuestion in autoAssignedQuestions)
        {
            await _examQuestionRepository.DeleteAsync(autoAssignedQuestion);
        }

        var nextSortOrder = await GetNextExamQuestionSortOrderAsync(examId);
        foreach (var question in plan.SelectedQuestions)
        {
            await _examQuestionRepository.InsertAsync(new ExamQuestion(
                _guidGenerator.Create(),
                examId,
                question.Id,
                nextSortOrder,
                assignmentSource: QuestionAssignmentSource.Auto), autoSave: true);

            nextSortOrder += 10;
        }

        return new ExamAutoAssignmentResultDto
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
        var query = await _examRepository.GetQueryableAsync();
        query = query.Where(x => x.Code == code);
        if (excludedId.HasValue)
        {
            query = query.Where(x => x.Id != excludedId.Value);
        }

        if (await AsyncExecuter.AnyAsync(query))
        {
            throw new UserFriendlyException(L["Exams:CodeAlreadyExists", code]);
        }
    }

    private void EnsureCanManageAutoRules(Exam exam)
    {
        if (exam.Status == ExamStatus.Archived)
        {
            throw new UserFriendlyException(L["Exams:ArchivedAutoRulesCannotBeChanged"]);
        }
    }

    private void EnsureCanApplyAutoAssignment(Exam exam)
    {
        EnsureCanManageAutoRules(exam);
    }

    private void EnsureCanActivate(Exam exam)
    {
        if (exam.Status == ExamStatus.Archived)
        {
            throw new UserFriendlyException(L["Exams:ArchivedCannotBeActivated"]);
        }
    }

    private void EnsureCanPublish(Exam exam)
    {
        if (exam.Status == ExamStatus.Archived)
        {
            throw new UserFriendlyException(L["Exams:ArchivedCannotBePublished"]);
        }

        if (!exam.IsActive)
        {
            throw new UserFriendlyException(L["Exams:InactiveCannotBePublished"]);
        }
    }

    private void EnsureQuestionCanBeUsed(Question question)
    {
        if (!question.IsActive)
        {
            throw new UserFriendlyException(L["Exams:InactiveQuestionCannotBeUsed"]);
        }

        if (question.Status != QuestionStatus.Published)
        {
            throw new UserFriendlyException(L["Exams:UnpublishedQuestionCannotBeUsed"]);
        }
    }

    private async Task EnsureAssignedQuestionsCanBePublishedAsync(Guid examId)
    {
        var examQuestions = await GetExamQuestionsByExamIdAsync(examId);
        if (examQuestions.Count == 0)
        {
            return;
        }

        var questionMap = await GetQuestionMapAsync(examQuestions.Select(x => x.QuestionId).Distinct().ToList());
        if (examQuestions.Any(x =>
                !questionMap.TryGetValue(x.QuestionId, out var question) ||
                !question.IsActive ||
                question.Status != QuestionStatus.Published))
        {
            throw new UserFriendlyException(L["Exams:AssignedQuestionsMustBePublished"]);
        }
    }

    private async Task<int> GetQuestionCountAsync(Guid examId)
    {
        var query = await _examQuestionRepository.GetQueryableAsync();
        return await AsyncExecuter.CountAsync(query.Where(x => x.ExamId == examId));
    }

    private async Task<List<ExamAutoQuestionRule>> GetAutoQuestionRulesByExamIdAsync(Guid examId)
    {
        var query = await _examAutoQuestionRuleRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query
            .Where(x => x.ExamId == examId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreationTime));
    }

    private async Task<Dictionary<Guid, int>> GetQuestionCountsAsync(IReadOnlyList<Guid> examIds)
    {
        if (examIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var query = await _examQuestionRepository.GetQueryableAsync();
        var examQuestions = await AsyncExecuter.ToListAsync(query.Where(x => examIds.Contains(x.ExamId)));
        return examQuestions
            .GroupBy(x => x.ExamId)
            .ToDictionary(x => x.Key, x => x.Count());
    }

    private async Task<List<ExamQuestion>> GetExamQuestionsByExamIdAsync(Guid examId)
    {
        var query = await _examQuestionRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query.Where(x => x.ExamId == examId).OrderBy(x => x.SortOrder));
    }

    private async Task<int> GetNextExamQuestionSortOrderAsync(Guid examId)
    {
        var examQuestions = await GetExamQuestionsByExamIdAsync(examId);
        return examQuestions.Count == 0 ? 10 : examQuestions.Max(x => x.SortOrder) + 10;
    }

    private async Task<QuestionType> GetActiveQuestionTypeAsync(Guid questionTypeId)
    {
        var questionType = await _questionTypeRepository.GetAsync(questionTypeId);
        if (!questionType.IsActive)
        {
            throw new UserFriendlyException(L["Exams:InactiveQuestionTypeCannotBeUsed"]);
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

    private async Task<ExamAutoAssignmentPlan> BuildAutoAssignmentPlanAsync(Guid examId, IReadOnlyList<ExamAutoQuestionRule> rules)
    {
        var typeMap = await GetQuestionTypeMapAsync(rules.Select(x => x.QuestionTypeId).Distinct().ToList());
        var assignedQuestions = await GetExamQuestionsByExamIdAsync(examId);
        var manualAssignedQuestions = assignedQuestions
            .Where(x => x.AssignmentSource == QuestionAssignmentSource.Manual)
            .ToList();
        var autoAssignedQuestions = assignedQuestions
            .Where(x => x.AssignmentSource == QuestionAssignmentSource.Auto)
            .ToList();

        var manualQuestionMap = await GetQuestionMapAsync(manualAssignedQuestions.Select(x => x.QuestionId).Distinct().ToList());
        var remainingManualQuestions = manualAssignedQuestions
            .Where(x => manualQuestionMap.ContainsKey(x.QuestionId))
            .Select(x => new ManualAssignmentQuestionRef<ExamQuestion>
            {
                Assignment = x,
                Question = manualQuestionMap[x.QuestionId]
            })
            .OrderBy(x => x.Assignment.SortOrder)
            .ToList();

        var excludedQuestionIds = remainingManualQuestions
            .Select(x => x.Question.Id)
            .ToHashSet();

        var plan = new ExamAutoAssignmentPlan
        {
            Preview = new ExamAutoAssignmentPreviewDto
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
            plan.Preview.Items.Add(new ExamAutoAssignmentRulePreviewDto
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

    private static bool MatchesRule(Question question, ExamAutoQuestionRule rule)
    {
        return question.QuestionTypeId == rule.QuestionTypeId
            && (!rule.Difficulty.HasValue || question.Difficulty == rule.Difficulty.Value);
    }

    private static string NormalizeCode(string code)
    {
        return Check.NotNullOrWhiteSpace(code, nameof(code), ExamConsts.MaxCodeLength).Trim();
    }

    private static IQueryable<Exam> ApplySorting(IQueryable<Exam> query, string? sorting)
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

    private static IEnumerable<ExamQuestionDto> ApplyExamQuestionSorting(IEnumerable<ExamQuestionDto> examQuestions, string? sorting)
    {
        return sorting?.Trim().ToLowerInvariant() switch
        {
            "questiontitle" => examQuestions.OrderBy(x => x.QuestionTitle),
            "questiontitle desc" => examQuestions.OrderByDescending(x => x.QuestionTitle),
            "sortorder desc" => examQuestions.OrderByDescending(x => x.SortOrder),
            _ => examQuestions.OrderBy(x => x.SortOrder).ThenBy(x => x.QuestionTitle)
        };
    }

    private static ExamDto MapToDto(Exam exam, int assignedQuestionCount)
    {
        return new ExamDto
        {
            Id = exam.Id,
            Code = exam.Code,
            Title = exam.Title,
            Description = exam.Description,
            Status = exam.Status,
            AccessLevel = exam.AccessLevel,
            SelectionMode = exam.SelectionMode,
            DurationMinutes = exam.DurationMinutes,
            TotalQuestionCount = exam.TotalQuestionCount,
            AssignedQuestionCount = assignedQuestionCount,
            PassingScore = exam.PassingScore,
            ShuffleQuestions = exam.ShuffleQuestions,
            ShuffleOptions = exam.ShuffleOptions,
            IsActive = exam.IsActive,
            SortOrder = exam.SortOrder,
            PublishedTime = exam.PublishedTime,
            ArchivedTime = exam.ArchivedTime,
            CreationTime = exam.CreationTime,
            CreatorId = exam.CreatorId,
            LastModificationTime = exam.LastModificationTime,
            LastModifierId = exam.LastModifierId,
            IsDeleted = exam.IsDeleted,
            DeleterId = exam.DeleterId,
            DeletionTime = exam.DeletionTime
        };
    }

    private static ExamQuestionDto MapToDto(
        ExamQuestion examQuestion,
        Question? question,
        IReadOnlyDictionary<Guid, QuestionType> questionTypeMap)
    {
        QuestionType? questionType = null;
        if (question != null)
        {
            questionTypeMap.TryGetValue(question.QuestionTypeId, out questionType);
        }

        return new ExamQuestionDto
        {
            Id = examQuestion.Id,
            ExamId = examQuestion.ExamId,
            QuestionId = examQuestion.QuestionId,
            QuestionTitle = question?.Title ?? string.Empty,
            QuestionContent = question?.Content ?? string.Empty,
            QuestionTypeName = questionType?.DisplayName ?? string.Empty,
            QuestionDifficulty = question?.Difficulty ?? QuestionDifficulty.Medium,
            QuestionStatus = question?.Status ?? QuestionStatus.Draft,
            QuestionIsActive = question?.IsActive ?? false,
            QuestionScore = question?.Score ?? 0,
            SortOrder = examQuestion.SortOrder,
            ScoreOverride = examQuestion.ScoreOverride,
            IsRequired = examQuestion.IsRequired,
            AssignmentSource = examQuestion.AssignmentSource,
            CreationTime = examQuestion.CreationTime,
            CreatorId = examQuestion.CreatorId,
            LastModificationTime = examQuestion.LastModificationTime,
            LastModifierId = examQuestion.LastModifierId,
            IsDeleted = examQuestion.IsDeleted,
            DeleterId = examQuestion.DeleterId,
            DeletionTime = examQuestion.DeletionTime
        };
    }

    private static ExamAutoQuestionRuleDto MapToDto(
        ExamAutoQuestionRule rule,
        IReadOnlyDictionary<Guid, QuestionType> questionTypeMap)
    {
        return new ExamAutoQuestionRuleDto
        {
            Id = rule.Id,
            ExamId = rule.ExamId,
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

    private sealed class ExamAutoAssignmentPlan
    {
        public ExamAutoAssignmentPreviewDto Preview { get; init; } = new();

        public List<Question> SelectedQuestions { get; } = [];
    }

    private sealed class ManualAssignmentQuestionRef<TAssignment>
    {
        public required TAssignment Assignment { get; init; }

        public required Question Question { get; init; }
    }
}

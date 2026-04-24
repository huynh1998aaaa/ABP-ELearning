using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Exams;
using Elearning.Permissions;
using Elearning.Practices;
using Elearning.QuestionTypes;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Elearning.Questions;

[Authorize(ElearningPermissions.Questions.Default)]
public class QuestionAppService : ElearningAppService, IQuestionAppService
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IRepository<Question, Guid> _questionRepository;
    private readonly IRepository<QuestionEssayAnswer, Guid> _essayAnswerRepository;
    private readonly IRepository<QuestionMatchingPair, Guid> _matchingPairRepository;
    private readonly IRepository<QuestionOption, Guid> _optionRepository;
    private readonly IRepository<QuestionType, Guid> _questionTypeRepository;
    private readonly IRepository<ExamQuestion, Guid> _examQuestionRepository;
    private readonly IRepository<PracticeQuestion, Guid> _practiceQuestionRepository;
    private readonly IDataFilter<ISoftDelete> _softDeleteFilter;

    public QuestionAppService(
        IRepository<Question, Guid> questionRepository,
        IRepository<QuestionOption, Guid> optionRepository,
        IRepository<QuestionMatchingPair, Guid> matchingPairRepository,
        IRepository<QuestionEssayAnswer, Guid> essayAnswerRepository,
        IRepository<QuestionType, Guid> questionTypeRepository,
        IRepository<ExamQuestion, Guid> examQuestionRepository,
        IRepository<PracticeQuestion, Guid> practiceQuestionRepository,
        IDataFilter<ISoftDelete> softDeleteFilter,
        IGuidGenerator guidGenerator)
    {
        _questionRepository = questionRepository;
        _optionRepository = optionRepository;
        _matchingPairRepository = matchingPairRepository;
        _essayAnswerRepository = essayAnswerRepository;
        _questionTypeRepository = questionTypeRepository;
        _examQuestionRepository = examQuestionRepository;
        _practiceQuestionRepository = practiceQuestionRepository;
        _softDeleteFilter = softDeleteFilter;
        _guidGenerator = guidGenerator;
    }

    public async Task<PagedResultDto<QuestionDto>> GetListAsync(GetQuestionListInput input)
    {
        var query = await _questionRepository.GetQueryableAsync();

        if (!input.Filter.IsNullOrWhiteSpace())
        {
            var filter = input.Filter!.Trim();
            query = query.Where(x =>
                x.Title.Contains(filter) ||
                x.Content.Contains(filter) ||
                (x.Explanation != null && x.Explanation.Contains(filter)));
        }

        if (input.QuestionTypeId.HasValue)
        {
            query = query.Where(x => x.QuestionTypeId == input.QuestionTypeId.Value);
        }

        if (input.Difficulty.HasValue)
        {
            query = query.Where(x => x.Difficulty == input.Difficulty.Value);
        }

        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }

        if (input.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == input.IsActive.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(query);
        var questions = await AsyncExecuter.ToListAsync(ApplySorting(query, input.Sorting)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount));

        var typeIds = questions.Select(x => x.QuestionTypeId).Distinct().ToList();
        var typeQuery = await _questionTypeRepository.GetQueryableAsync();
        var types = await AsyncExecuter.ToListAsync(typeQuery.Where(x => typeIds.Contains(x.Id)));
        var typeMap = types.ToDictionary(x => x.Id);

        return new PagedResultDto<QuestionDto>(
            totalCount,
            questions.Select(x => MapToDto(x, typeMap.GetValueOrDefault(x.QuestionTypeId))).ToList());
    }

    public async Task<QuestionDto> GetAsync(Guid id)
    {
        var question = await _questionRepository.GetAsync(id);
        var questionType = await _questionTypeRepository.GetAsync(question.QuestionTypeId);
        var options = await GetOptionsAsync(id);
        var matchingPairs = await GetMatchingPairsAsync(id);
        var essayAnswer = await GetEssayAnswerAsync(id);

        return MapToDto(question, questionType, options, matchingPairs, essayAnswer);
    }

    [Authorize(ElearningPermissions.Questions.Create)]
    public async Task<QuestionDto> CreateAsync(CreateQuestionDto input)
    {
        var questionType = await _questionTypeRepository.GetAsync(input.QuestionTypeId);
        if (!questionType.IsActive)
        {
            throw new BusinessException(ElearningDomainErrorCodes.InactiveQuestionTypeCannotBeUsed)
                .WithData(nameof(Question.QuestionTypeId), input.QuestionTypeId);
        }

        var normalizedOptions = NormalizeOptions(input.Options);
        var normalizedPairs = NormalizeMatchingPairs(input.MatchingPairs);
        ValidateAnswers(questionType, normalizedOptions, normalizedPairs);

        var question = new Question(
            _guidGenerator.Create(),
            input.QuestionTypeId,
            input.Title,
            input.Content,
            input.Difficulty,
            input.Score,
            input.SortOrder,
            input.Explanation);

        if (!input.IsActive)
        {
            question.Deactivate();
        }

        await _questionRepository.InsertAsync(question, autoSave: true);
        await ReplaceAnswersAsync(question.Id, questionType, normalizedOptions, normalizedPairs, input.EssayAnswer);

        return await GetAsync(question.Id);
    }

    [Authorize(ElearningPermissions.Questions.Update)]
    public async Task<QuestionDto> UpdateAsync(Guid id, UpdateQuestionDto input)
    {
        var question = await _questionRepository.GetAsync(id);
        question.EnsureQuestionType(input.QuestionTypeId);

        var questionType = await _questionTypeRepository.GetAsync(question.QuestionTypeId);
        var normalizedOptions = NormalizeOptions(input.Options);
        var normalizedPairs = NormalizeMatchingPairs(input.MatchingPairs);
        ValidateAnswers(questionType, normalizedOptions, normalizedPairs);

        question.UpdateDetails(
            input.Title,
            input.Content,
            input.Explanation,
            input.Difficulty,
            input.Score,
            input.SortOrder);

        await _questionRepository.UpdateAsync(question, autoSave: true);
        await ReplaceAnswersAsync(question.Id, questionType, normalizedOptions, normalizedPairs, input.EssayAnswer);

        return await GetAsync(question.Id);
    }

    [Authorize(ElearningPermissions.Questions.Update)]
    public async Task ActivateAsync(Guid id)
    {
        var question = await _questionRepository.GetAsync(id);
        EnsureCanActivate(question);
        question.Activate();
        await _questionRepository.UpdateAsync(question, autoSave: true);
    }

    [Authorize(ElearningPermissions.Questions.Update)]
    public async Task DeactivateAsync(Guid id)
    {
        var question = await _questionRepository.GetAsync(id);
        question.Deactivate();
        await _questionRepository.UpdateAsync(question, autoSave: true);
    }

    [Authorize(ElearningPermissions.Questions.Publish)]
    public async Task PublishAsync(Guid id)
    {
        var question = await _questionRepository.GetAsync(id);
        EnsureCanPublish(question);
        var questionType = await _questionTypeRepository.GetAsync(question.QuestionTypeId);
        var options = (await GetOptionsAsync(id))
            .Select(x => new QuestionOptionInputDto
            {
                Text = x.Text,
                IsCorrect = x.IsCorrect,
                SortOrder = x.SortOrder
            })
            .ToList();
        var matchingPairs = (await GetMatchingPairsAsync(id))
            .Select(x => new QuestionMatchingPairInputDto
            {
                LeftText = x.LeftText,
                RightText = x.RightText,
                SortOrder = x.SortOrder
            })
            .ToList();

        ValidateAnswers(questionType, options, matchingPairs);

        question.Publish();
        await _questionRepository.UpdateAsync(question, autoSave: true);
    }

    [Authorize(ElearningPermissions.Questions.Publish)]
    public async Task ArchiveAsync(Guid id)
    {
        var question = await _questionRepository.GetAsync(id);
        question.Archive();
        await _questionRepository.UpdateAsync(question, autoSave: true);
    }

    [Authorize(ElearningPermissions.Questions.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var question = await _questionRepository.GetAsync(id);
        await EnsureCanHardDeleteAsync(question);

        try
        {
            await _questionRepository.HardDeleteAsync(question, autoSave: true);
        }
        catch (Exception ex) when (IsReferentialIntegrityException(ex))
        {
            throw new UserFriendlyException(L["Questions:CannotHardDeleteInUse"]);
        }
    }

    [Authorize(ElearningPermissions.Questions.Publish)]
    public async Task<BulkQuestionActionResultDto> BulkPublishAsync(BulkQuestionActionInput input)
    {
        var questionIds = NormalizeQuestionIds(input);
        if (questionIds.Count == 0)
        {
            throw new UserFriendlyException(L["Questions:BulkNoSelection"]);
        }

        var result = CreateBulkResult(questionIds);
        foreach (var questionId in questionIds)
        {
            try
            {
                var question = await _questionRepository.GetAsync(questionId);
                if (question.Status == QuestionStatus.Published)
                {
                    result.SkippedCount++;
                    continue;
                }

                await PublishAsync(questionId);
                result.SucceededCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(BuildBulkErrorMessage(questionId, ex));
            }
        }

        return result;
    }

    [Authorize(ElearningPermissions.Questions.Publish)]
    public async Task<BulkQuestionActionResultDto> BulkArchiveAsync(BulkQuestionActionInput input)
    {
        var questionIds = NormalizeQuestionIds(input);
        if (questionIds.Count == 0)
        {
            throw new UserFriendlyException(L["Questions:BulkNoSelection"]);
        }

        var result = CreateBulkResult(questionIds);
        foreach (var questionId in questionIds)
        {
            try
            {
                var question = await _questionRepository.GetAsync(questionId);
                if (question.Status == QuestionStatus.Archived)
                {
                    result.SkippedCount++;
                    continue;
                }

                question.Archive();
                await _questionRepository.UpdateAsync(question, autoSave: true);
                result.SucceededCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(BuildBulkErrorMessage(questionId, ex));
            }
        }

        return result;
    }

    private async Task ReplaceAnswersAsync(
        Guid questionId,
        QuestionType questionType,
        IReadOnlyList<QuestionOptionInputDto> options,
        IReadOnlyList<QuestionMatchingPairInputDto> matchingPairs,
        QuestionEssayAnswerInputDto essayAnswer)
    {
        await DeleteAnswersAsync(questionId);

        if (questionType.InputKind is QuestionInputKind.SingleChoice or QuestionInputKind.MultipleChoice)
        {
            foreach (var option in options)
            {
                await _optionRepository.InsertAsync(new QuestionOption(
                    _guidGenerator.Create(),
                    questionId,
                    option.Text,
                    option.IsCorrect,
                    option.SortOrder));
            }
        }

        if (questionType.InputKind == QuestionInputKind.Matching)
        {
            foreach (var pair in matchingPairs)
            {
                await _matchingPairRepository.InsertAsync(new QuestionMatchingPair(
                    _guidGenerator.Create(),
                    questionId,
                    pair.LeftText,
                    pair.RightText,
                    pair.SortOrder));
            }
        }

        if (questionType.InputKind == QuestionInputKind.Essay)
        {
            await _essayAnswerRepository.InsertAsync(new QuestionEssayAnswer(
                _guidGenerator.Create(),
                questionId,
                essayAnswer.SampleAnswer,
                essayAnswer.Rubric,
                essayAnswer.MaxWords));
        }
    }

    private async Task DeleteAnswersAsync(Guid questionId)
    {
        foreach (var option in await GetOptionsAsync(questionId))
        {
            await _optionRepository.DeleteAsync(option);
        }

        foreach (var pair in await GetMatchingPairsAsync(questionId))
        {
            await _matchingPairRepository.DeleteAsync(pair);
        }

        var essayAnswer = await GetEssayAnswerAsync(questionId);
        if (essayAnswer != null)
        {
            await _essayAnswerRepository.DeleteAsync(essayAnswer);
        }
    }

    private async Task<List<QuestionOption>> GetOptionsAsync(Guid questionId)
    {
        var query = await _optionRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query
            .Where(x => x.QuestionId == questionId)
            .OrderBy(x => x.SortOrder));
    }

    private async Task<List<QuestionMatchingPair>> GetMatchingPairsAsync(Guid questionId)
    {
        var query = await _matchingPairRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query
            .Where(x => x.QuestionId == questionId)
            .OrderBy(x => x.SortOrder));
    }

    private async Task<QuestionEssayAnswer?> GetEssayAnswerAsync(Guid questionId)
    {
        var query = await _essayAnswerRepository.GetQueryableAsync();
        return await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.QuestionId == questionId));
    }

    private static List<QuestionOptionInputDto> NormalizeOptions(IEnumerable<QuestionOptionInputDto> options)
    {
        return options
            .Where(x => !x.Text.IsNullOrWhiteSpace())
            .Select((x, index) => new QuestionOptionInputDto
            {
                Text = x.Text.Trim(),
                IsCorrect = x.IsCorrect,
                SortOrder = x.SortOrder == 0 ? index + 1 : x.SortOrder
            })
            .ToList();
    }

    private static List<QuestionMatchingPairInputDto> NormalizeMatchingPairs(IEnumerable<QuestionMatchingPairInputDto> pairs)
    {
        return pairs
            .Where(x => !x.LeftText.IsNullOrWhiteSpace() || !x.RightText.IsNullOrWhiteSpace())
            .Select((x, index) => new QuestionMatchingPairInputDto
            {
                LeftText = x.LeftText.Trim(),
                RightText = x.RightText.Trim(),
                SortOrder = x.SortOrder == 0 ? index + 1 : x.SortOrder
            })
            .ToList();
    }

    private static void ValidateAnswers(
        QuestionType questionType,
        IReadOnlyList<QuestionOptionInputDto> options,
        IReadOnlyList<QuestionMatchingPairInputDto> matchingPairs)
    {
        if (questionType.InputKind == QuestionInputKind.SingleChoice)
        {
            ValidateMinimumOptions(questionType, options);
            if (options.Count(x => x.IsCorrect) != 1)
            {
                ThrowInvalidAnswers("Single choice questions must have exactly one correct option.");
            }
        }

        if (questionType.InputKind == QuestionInputKind.MultipleChoice)
        {
            ValidateMinimumOptions(questionType, options);
            if (options.Count(x => x.IsCorrect) < 1)
            {
                ThrowInvalidAnswers("Multiple choice questions must have at least one correct option.");
            }
        }

        if (questionType.InputKind == QuestionInputKind.Matching)
        {
            if (matchingPairs.Count < 2 || matchingPairs.Any(x => x.LeftText.IsNullOrWhiteSpace() || x.RightText.IsNullOrWhiteSpace()))
            {
                ThrowInvalidAnswers("Matching questions must have at least two complete matching pairs.");
            }
        }
    }

    private static void ValidateMinimumOptions(QuestionType questionType, IReadOnlyList<QuestionOptionInputDto> options)
    {
        var minimumOptions = questionType.MinimumOptions ?? 2;
        if (options.Count < minimumOptions)
        {
            ThrowInvalidAnswers($"Question requires at least {minimumOptions} options.");
        }
    }

    private static void ThrowInvalidAnswers(string message)
    {
        throw new BusinessException(ElearningDomainErrorCodes.InvalidQuestionAnswers)
            .WithData("Reason", message);
    }

    private void EnsureCanActivate(Question question)
    {
        if (question.Status == QuestionStatus.Archived)
        {
            throw new UserFriendlyException(L["Questions:ArchivedCannotBeActivated"]);
        }
    }

    private void EnsureCanPublish(Question question)
    {
        if (question.Status == QuestionStatus.Archived)
        {
            throw new UserFriendlyException(L["Questions:ArchivedCannotBePublished"]);
        }

        if (!question.IsActive)
        {
            throw new UserFriendlyException(L["Questions:InactiveCannotBePublished"]);
        }
    }

    private async Task EnsureCanHardDeleteAsync(Question question)
    {
        if (question.Status != QuestionStatus.Archived)
        {
            throw new UserFriendlyException(L["Questions:OnlyArchivedCanBeDeleted"]);
        }

        using (_softDeleteFilter.Disable())
        {
            var examQuestionQuery = await _examQuestionRepository.GetQueryableAsync();
            if (await AsyncExecuter.AnyAsync(examQuestionQuery.Where(x => x.QuestionId == question.Id)))
            {
                throw new UserFriendlyException(L["Questions:CannotHardDeleteInUse"]);
            }

            var practiceQuestionQuery = await _practiceQuestionRepository.GetQueryableAsync();
            if (await AsyncExecuter.AnyAsync(practiceQuestionQuery.Where(x => x.QuestionId == question.Id)))
            {
                throw new UserFriendlyException(L["Questions:CannotHardDeleteInUse"]);
            }
        }
    }

    private static bool IsReferentialIntegrityException(Exception exception)
    {
        var message = exception.GetBaseException().Message;
        return message.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("conflicted with the DELETE", StringComparison.OrdinalIgnoreCase);
    }

    private static List<Guid> NormalizeQuestionIds(BulkQuestionActionInput? input)
    {
        return input?.QuestionIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList() ?? [];
    }

    private static BulkQuestionActionResultDto CreateBulkResult(IReadOnlyCollection<Guid> questionIds)
    {
        return new BulkQuestionActionResultDto
        {
            RequestedCount = questionIds.Count
        };
    }

    private string BuildBulkErrorMessage(Guid questionId, Exception exception)
    {
        var message = exception switch
        {
            UserFriendlyException => exception.Message,
            BusinessException businessException when businessException.Data.Contains("Reason") => businessException.Data["Reason"]?.ToString() ?? businessException.Message,
            _ => L["Common:AjaxOperationFailed"].Value
        };

        return L["Questions:BulkItemError", questionId, message];
    }

    private static IQueryable<Question> ApplySorting(IQueryable<Question> query, string? sorting)
    {
        return sorting?.Trim().ToLowerInvariant() switch
        {
            "title" => query.OrderBy(x => x.Title),
            "title desc" => query.OrderByDescending(x => x.Title),
            "score" => query.OrderBy(x => x.Score),
            "score desc" => query.OrderByDescending(x => x.Score),
            "sortorder desc" => query.OrderByDescending(x => x.SortOrder).ThenBy(x => x.Title),
            _ => query.OrderBy(x => x.SortOrder).ThenByDescending(x => x.CreationTime)
        };
    }

    private static QuestionDto MapToDto(Question question, QuestionType? questionType)
    {
        return MapToDto(question, questionType, new List<QuestionOption>(), new List<QuestionMatchingPair>(), null);
    }

    private static QuestionDto MapToDto(
        Question question,
        QuestionType? questionType,
        IReadOnlyList<QuestionOption> options,
        IReadOnlyList<QuestionMatchingPair> matchingPairs,
        QuestionEssayAnswer? essayAnswer)
    {
        return new QuestionDto
        {
            Id = question.Id,
            QuestionTypeId = question.QuestionTypeId,
            QuestionTypeCode = questionType?.Code ?? string.Empty,
            QuestionTypeName = questionType?.DisplayName ?? string.Empty,
            Title = question.Title,
            Content = question.Content,
            Explanation = question.Explanation,
            Difficulty = question.Difficulty,
            Score = question.Score,
            IsActive = question.IsActive,
            SortOrder = question.SortOrder,
            Status = question.Status,
            Options = options.Select(x => new QuestionOptionDto
            {
                Id = x.Id,
                Text = x.Text,
                IsCorrect = x.IsCorrect,
                SortOrder = x.SortOrder
            }).ToList(),
            MatchingPairs = matchingPairs.Select(x => new QuestionMatchingPairDto
            {
                Id = x.Id,
                LeftText = x.LeftText,
                RightText = x.RightText,
                SortOrder = x.SortOrder
            }).ToList(),
            EssayAnswer = essayAnswer == null
                ? null
                : new QuestionEssayAnswerDto
                {
                    Id = essayAnswer.Id,
                    SampleAnswer = essayAnswer.SampleAnswer,
                    Rubric = essayAnswer.Rubric,
                    MaxWords = essayAnswer.MaxWords
                },
            CreationTime = question.CreationTime,
            CreatorId = question.CreatorId,
            LastModificationTime = question.LastModificationTime,
            LastModifierId = question.LastModifierId,
            IsDeleted = question.IsDeleted,
            DeleterId = question.DeleterId,
            DeletionTime = question.DeletionTime
        };
    }
}

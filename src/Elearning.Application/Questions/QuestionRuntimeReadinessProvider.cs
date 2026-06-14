using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.QuestionTypes;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace Elearning.Questions;

public class QuestionRuntimeReadinessProvider : ITransientDependency
{
    private readonly IRepository<Question, Guid> _questionRepository;
    private readonly IRepository<QuestionType, Guid> _questionTypeRepository;
    private readonly IRepository<QuestionOption, Guid> _questionOptionRepository;
    private readonly IRepository<QuestionMatchingPair, Guid> _questionMatchingPairRepository;
    private readonly IRepository<QuestionEssayAnswer, Guid> _questionEssayAnswerRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;

    public QuestionRuntimeReadinessProvider(
        IRepository<Question, Guid> questionRepository,
        IRepository<QuestionType, Guid> questionTypeRepository,
        IRepository<QuestionOption, Guid> questionOptionRepository,
        IRepository<QuestionMatchingPair, Guid> questionMatchingPairRepository,
        IRepository<QuestionEssayAnswer, Guid> questionEssayAnswerRepository,
        IAsyncQueryableExecuter asyncExecuter)
    {
        _questionRepository = questionRepository;
        _questionTypeRepository = questionTypeRepository;
        _questionOptionRepository = questionOptionRepository;
        _questionMatchingPairRepository = questionMatchingPairRepository;
        _questionEssayAnswerRepository = questionEssayAnswerRepository;
        _asyncExecuter = asyncExecuter;
    }

    public async Task<Dictionary<Guid, bool>> GetReadinessMapAsync(IReadOnlyList<Guid> questionIds)
    {
        var distinctQuestionIds = questionIds.Distinct().ToList();
        if (distinctQuestionIds.Count == 0)
        {
            return new Dictionary<Guid, bool>();
        }

        var questionQuery = await _questionRepository.GetQueryableAsync();
        var questions = await _asyncExecuter.ToListAsync(questionQuery.Where(x => distinctQuestionIds.Contains(x.Id)));

        var questionTypeIds = questions.Select(x => x.QuestionTypeId).Distinct().ToList();
        var questionTypeQuery = await _questionTypeRepository.GetQueryableAsync();
        var questionTypeMap = (await _asyncExecuter.ToListAsync(questionTypeQuery.Where(x => questionTypeIds.Contains(x.Id))))
            .ToDictionary(x => x.Id);

        var optionQuery = await _questionOptionRepository.GetQueryableAsync();
        var optionLookup = (await _asyncExecuter.ToListAsync(optionQuery.Where(x => distinctQuestionIds.Contains(x.QuestionId))))
            .GroupBy(x => x.QuestionId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<QuestionOption>)x.ToList());

        var matchingPairQuery = await _questionMatchingPairRepository.GetQueryableAsync();
        var matchingPairLookup = (await _asyncExecuter.ToListAsync(matchingPairQuery.Where(x => distinctQuestionIds.Contains(x.QuestionId))))
            .GroupBy(x => x.QuestionId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<QuestionMatchingPair>)x.ToList());

        var essayAnswerQuery = await _questionEssayAnswerRepository.GetQueryableAsync();
        var essayAnswerLookup = (await _asyncExecuter.ToListAsync(essayAnswerQuery.Where(x => distinctQuestionIds.Contains(x.QuestionId))))
            .GroupBy(x => x.QuestionId)
            .ToDictionary(x => x.Key, x => x.FirstOrDefault());

        var result = distinctQuestionIds.ToDictionary(x => x, _ => false);
        foreach (var question in questions)
        {
            questionTypeMap.TryGetValue(question.QuestionTypeId, out var questionType);
            result[question.Id] = IsReady(
                question,
                questionType,
                optionLookup.GetValueOrDefault(question.Id, []),
                matchingPairLookup.GetValueOrDefault(question.Id, []),
                essayAnswerLookup.GetValueOrDefault(question.Id));
        }

        return result;
    }

    public static bool IsSupportedQuestionType(string questionTypeCode)
    {
        return string.Equals(questionTypeCode, QuestionTypeCodes.SingleChoice, StringComparison.Ordinal) ||
               string.Equals(questionTypeCode, QuestionTypeCodes.MultipleChoice, StringComparison.Ordinal) ||
               string.Equals(questionTypeCode, QuestionTypeCodes.Matching, StringComparison.Ordinal) ||
               string.Equals(questionTypeCode, QuestionTypeCodes.Essay, StringComparison.Ordinal);
    }

    private static bool IsReady(
        Question question,
        QuestionType? questionType,
        IReadOnlyList<QuestionOption> options,
        IReadOnlyList<QuestionMatchingPair> matchingPairs,
        QuestionEssayAnswer? essayAnswer)
    {
        if (questionType == null || !IsSupportedQuestionType(questionType.Code))
        {
            return false;
        }

        if (string.Equals(questionType.Code, QuestionTypeCodes.Matching, StringComparison.Ordinal))
        {
            return matchingPairs.Count >= 2 &&
                   matchingPairs.Select(x => x.RightText).Distinct(StringComparer.Ordinal).Count() == matchingPairs.Count;
        }

        if (string.Equals(questionType.Code, QuestionTypeCodes.Essay, StringComparison.Ordinal))
        {
            return essayAnswer != null;
        }

        if (options.Count == 0)
        {
            return false;
        }

        var correctCount = options.Count(x => x.IsCorrect);
        if (correctCount == 0)
        {
            return false;
        }

        return !string.Equals(questionType.Code, QuestionTypeCodes.SingleChoice, StringComparison.Ordinal) || correctCount == 1;
    }
}

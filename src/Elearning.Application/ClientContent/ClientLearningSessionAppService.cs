using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Elearning.Exams;
using Elearning.LearningSessions;
using Elearning.Permissions;
using Elearning.Practices;
using Elearning.PremiumSubscriptions;
using Elearning.Questions;
using Elearning.QuestionTypes;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Elearning.ClientContent;

[Authorize]
public class ClientLearningSessionAppService : ElearningAppService, IClientLearningSessionAppService
{
    private readonly ICurrentUserPremiumAppService _currentUserPremiumAppService;
    private readonly IRepository<Exam, Guid> _examRepository;
    private readonly IRepository<ExamQuestion, Guid> _examQuestionRepository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IRepository<LearningSession, Guid> _learningSessionRepository;
    private readonly IRepository<LearningSessionAnswer, Guid> _learningSessionAnswerRepository;
    private readonly IRepository<LearningSessionQuestionEssayAnswer, Guid> _learningSessionQuestionEssayAnswerRepository;
    private readonly IRepository<LearningSessionQuestion, Guid> _learningSessionQuestionRepository;
    private readonly IRepository<LearningSessionQuestionMatchingPair, Guid> _learningSessionQuestionMatchingPairRepository;
    private readonly IRepository<LearningSessionQuestionOption, Guid> _learningSessionQuestionOptionRepository;
    private readonly IRepository<PracticeSet, Guid> _practiceSetRepository;
    private readonly IRepository<PracticeQuestion, Guid> _practiceQuestionRepository;
    private readonly IRepository<QuestionEssayAnswer, Guid> _questionEssayAnswerRepository;
    private readonly IRepository<Question, Guid> _questionRepository;
    private readonly IRepository<QuestionMatchingPair, Guid> _questionMatchingPairRepository;
    private readonly IRepository<QuestionOption, Guid> _questionOptionRepository;
    private readonly IRepository<QuestionType, Guid> _questionTypeRepository;

    public ClientLearningSessionAppService(
        IRepository<LearningSession, Guid> learningSessionRepository,
        IRepository<LearningSessionQuestion, Guid> learningSessionQuestionRepository,
        IRepository<LearningSessionQuestionEssayAnswer, Guid> learningSessionQuestionEssayAnswerRepository,
        IRepository<LearningSessionQuestionMatchingPair, Guid> learningSessionQuestionMatchingPairRepository,
        IRepository<LearningSessionQuestionOption, Guid> learningSessionQuestionOptionRepository,
        IRepository<LearningSessionAnswer, Guid> learningSessionAnswerRepository,
        IRepository<Exam, Guid> examRepository,
        IRepository<ExamQuestion, Guid> examQuestionRepository,
        IRepository<PracticeSet, Guid> practiceSetRepository,
        IRepository<PracticeQuestion, Guid> practiceQuestionRepository,
        IRepository<Question, Guid> questionRepository,
        IRepository<QuestionEssayAnswer, Guid> questionEssayAnswerRepository,
        IRepository<QuestionMatchingPair, Guid> questionMatchingPairRepository,
        IRepository<QuestionOption, Guid> questionOptionRepository,
        IRepository<QuestionType, Guid> questionTypeRepository,
        ICurrentUserPremiumAppService currentUserPremiumAppService,
        IGuidGenerator guidGenerator)
    {
        _learningSessionRepository = learningSessionRepository;
        _learningSessionQuestionRepository = learningSessionQuestionRepository;
        _learningSessionQuestionEssayAnswerRepository = learningSessionQuestionEssayAnswerRepository;
        _learningSessionQuestionMatchingPairRepository = learningSessionQuestionMatchingPairRepository;
        _learningSessionQuestionOptionRepository = learningSessionQuestionOptionRepository;
        _learningSessionAnswerRepository = learningSessionAnswerRepository;
        _examRepository = examRepository;
        _examQuestionRepository = examQuestionRepository;
        _practiceSetRepository = practiceSetRepository;
        _practiceQuestionRepository = practiceQuestionRepository;
        _questionRepository = questionRepository;
        _questionEssayAnswerRepository = questionEssayAnswerRepository;
        _questionMatchingPairRepository = questionMatchingPairRepository;
        _questionOptionRepository = questionOptionRepository;
        _questionTypeRepository = questionTypeRepository;
        _currentUserPremiumAppService = currentUserPremiumAppService;
        _guidGenerator = guidGenerator;
    }

    public async Task<ClientLearningLaunchResultDto> StartOrResumeAsync(ClientLearningItemKind kind, Guid id)
    {
        var userId = GetCurrentUserId();
        var sourceKind = MapSourceKind(kind);
        var existing = await FindInProgressSessionAsync(userId, sourceKind, id);

        if (existing != null)
        {
            await AutoSubmitIfExpiredAsync(existing);

            if (existing.Status == LearningSessionStatus.InProgress)
            {
                return new ClientLearningLaunchResultDto
                {
                    SessionId = existing.Id
                };
            }
        }

        var premiumStatus = await _currentUserPremiumAppService.GetCurrentUserPremiumStatusAsync();
        var prepared = kind == ClientLearningItemKind.Exam
            ? await PrepareExamSessionAsync(id, premiumStatus.IsPremium)
            : await PreparePracticeSessionAsync(id, premiumStatus.IsPremium);

        var startedAt = Clock.Now;
        var session = new LearningSession(
            _guidGenerator.Create(),
            userId,
            prepared.SourceKind,
            prepared.SourceId,
            prepared.SourceCode,
            prepared.Title,
            prepared.Description,
            prepared.IsPremiumContent,
            prepared.ShowExplanation,
            prepared.DurationMinutes,
            prepared.TotalQuestionCount,
            startedAt);

        await _learningSessionRepository.InsertAsync(session, autoSave: true);

        foreach (var preparedQuestion in prepared.Questions)
        {
            var sessionQuestion = new LearningSessionQuestion(
                _guidGenerator.Create(),
                session.Id,
                preparedQuestion.OriginalQuestionId,
                preparedQuestion.QuestionTypeId,
                preparedQuestion.QuestionTypeCode,
                preparedQuestion.QuestionTypeName,
                preparedQuestion.Title,
                preparedQuestion.Content,
                preparedQuestion.Explanation,
                preparedQuestion.SortOrder,
                preparedQuestion.Score);

            await _learningSessionQuestionRepository.InsertAsync(sessionQuestion);

            foreach (var preparedOption in preparedQuestion.Options)
            {
                await _learningSessionQuestionOptionRepository.InsertAsync(new LearningSessionQuestionOption(
                    _guidGenerator.Create(),
                    sessionQuestion.Id,
                    preparedOption.OriginalOptionId,
                    preparedOption.Text,
                    preparedOption.IsCorrect,
                    preparedOption.SortOrder));
            }

            foreach (var preparedMatchingPair in preparedQuestion.MatchingPairs)
            {
                await _learningSessionQuestionMatchingPairRepository.InsertAsync(new LearningSessionQuestionMatchingPair(
                    _guidGenerator.Create(),
                    sessionQuestion.Id,
                    preparedMatchingPair.OriginalMatchingPairId,
                    preparedMatchingPair.LeftText,
                    preparedMatchingPair.RightText,
                    preparedMatchingPair.SortOrder));
            }

            if (preparedQuestion.EssayAnswer != null)
            {
                await _learningSessionQuestionEssayAnswerRepository.InsertAsync(new LearningSessionQuestionEssayAnswer(
                    _guidGenerator.Create(),
                    sessionQuestion.Id,
                    preparedQuestion.EssayAnswer.SampleAnswer,
                    preparedQuestion.EssayAnswer.Rubric,
                    preparedQuestion.EssayAnswer.MaxWords));
            }

            await _learningSessionAnswerRepository.InsertAsync(new LearningSessionAnswer(
                _guidGenerator.Create(),
                session.Id,
                sessionQuestion.Id));
        }

        if (CurrentUnitOfWork != null)
        {
            await CurrentUnitOfWork.SaveChangesAsync();
        }

        return new ClientLearningLaunchResultDto
        {
            SessionId = session.Id
        };
    }

    public async Task<ClientLearningSessionDto> GetAsync(Guid sessionId)
    {
        var session = await GetOwnedSessionAsync(sessionId);
        await AutoSubmitIfExpiredAsync(session);
        return await BuildSessionDtoAsync(session.Id, includeCorrectAnswers: false, includeExplanation: false);
    }

    public async Task SaveAnswerAsync(Guid sessionId, SaveClientLearningAnswerDto input)
    {
        var session = await GetOwnedSessionAsync(sessionId);
        await EnsureSessionCanBeAnsweredAsync(session);

        var sessionQuestion = await _learningSessionQuestionRepository.FindAsync(x =>
            x.LearningSessionId == session.Id &&
            x.Id == input.QuestionId);

        if (sessionQuestion == null)
        {
            throw new EntityNotFoundException(typeof(LearningSessionQuestion), input.QuestionId);
        }

        var answer = await _learningSessionAnswerRepository.FindAsync(x => x.LearningSessionQuestionId == sessionQuestion.Id);
        if (answer == null)
        {
            throw new EntityNotFoundException(typeof(LearningSessionAnswer), sessionQuestion.Id);
        }

        if (string.Equals(sessionQuestion.QuestionTypeCode, QuestionTypeCodes.Matching, StringComparison.Ordinal))
        {
            var matchingPairQuery = await _learningSessionQuestionMatchingPairRepository.GetQueryableAsync();
            var matchingPairs = await AsyncExecuter.ToListAsync(matchingPairQuery
                .Where(x => x.LearningSessionQuestionId == sessionQuestion.Id)
                .OrderBy(x => x.SortOrder));

            var validatedAnswers = ValidateMatchingAnswerInput(matchingPairs, input.MatchingAnswers);
            var answeredPairs = validatedAnswers
                .Where(x => !x.SelectedRightText.IsNullOrWhiteSpace())
                .ToList();
            var isAnswered = answeredPairs.Count > 0;
            var isCorrect = matchingPairs.Count > 0 &&
                            answeredPairs.Count == matchingPairs.Count &&
                            matchingPairs.All(pair =>
                                string.Equals(
                                    validatedAnswers.FirstOrDefault(x => x.PairId == pair.Id)?.SelectedRightText,
                                    pair.RightText,
                                    StringComparison.Ordinal));
            var serialized = isAnswered ? JsonSerializer.Serialize(validatedAnswers) : null;

            answer.SaveMatching(serialized, isAnswered, isCorrect, Clock.Now);
        }
        else if (string.Equals(sessionQuestion.QuestionTypeCode, QuestionTypeCodes.Essay, StringComparison.Ordinal))
        {
            var essayAnswerQuery = await _learningSessionQuestionEssayAnswerRepository.GetQueryableAsync();
            var essayAnswer = await AsyncExecuter.FirstOrDefaultAsync(
                essayAnswerQuery.Where(x => x.LearningSessionQuestionId == sessionQuestion.Id));

            var normalizedEssayAnswer = ValidateEssayAnswerInput(essayAnswer, input.EssayAnswerText);
            answer.SaveEssay(normalizedEssayAnswer, !normalizedEssayAnswer.IsNullOrWhiteSpace(), Clock.Now);
        }
        else
        {
            var optionQuery = await _learningSessionQuestionOptionRepository.GetQueryableAsync();
            var options = await AsyncExecuter.ToListAsync(optionQuery
                .Where(x => x.LearningSessionQuestionId == sessionQuestion.Id)
                .OrderBy(x => x.SortOrder));

            ValidateObjectiveAnswerInput(sessionQuestion.QuestionTypeCode, options, input.SelectedOptionIds);

            var selectedOptionIds = input.SelectedOptionIds.Distinct().ToList();
            var isAnswered = selectedOptionIds.Count > 0;
            var correctOptionIds = options.Where(x => x.IsCorrect).Select(x => x.Id).OrderBy(x => x).ToList();
            var selectedOrdered = selectedOptionIds.OrderBy(x => x).ToList();
            var isCorrect = isAnswered && correctOptionIds.SequenceEqual(selectedOrdered);
            var serialized = isAnswered ? JsonSerializer.Serialize(selectedOptionIds) : null;

            answer.SaveSelection(serialized, isAnswered, isCorrect, Clock.Now);
        }

        await _learningSessionAnswerRepository.UpdateAsync(answer, autoSave: true);
    }

    public async Task<ClientLearningSessionResultDto> SubmitAsync(Guid sessionId)
    {
        var session = await GetOwnedSessionAsync(sessionId);

        if (session.Status == LearningSessionStatus.InProgress)
        {
            await SubmitInternalAsync(session, Clock.Now);
        }

        return await GetResultAsync(sessionId);
    }

    public async Task<ClientLearningSessionResultDto> GetResultAsync(Guid sessionId)
    {
        var session = await GetOwnedSessionAsync(sessionId);

        if (session.Status == LearningSessionStatus.InProgress)
        {
            await AutoSubmitIfExpiredAsync(session);
            if (session.Status == LearningSessionStatus.InProgress)
            {
                throw new UserFriendlyException(L["Client:ResultNotReady"]);
            }
        }

        var dto = await BuildSessionDtoAsync(
            session.Id,
            includeCorrectAnswers: true,
            includeExplanation: session.ShowExplanation && session.SourceKind == LearningSessionSourceKind.Practice);

        return new ClientLearningSessionResultDto
        {
            Id = dto.Id,
            SourceKind = dto.SourceKind,
            Title = dto.Title,
            ShowExplanation = dto.ShowExplanation,
            StartedAt = dto.StartedAt,
            SubmittedAt = session.SubmittedAt,
            Score = session.Score,
            CorrectCount = session.CorrectCount,
            AnsweredCount = session.AnsweredCount,
            TotalQuestionCount = session.TotalQuestionCount,
            PendingManualGradingCount = dto.Questions.Count(x =>
                string.Equals(x.QuestionTypeCode, QuestionTypeCodes.Essay, StringComparison.Ordinal) &&
                x.IsAnswered),
            Questions = dto.Questions
        };
    }

    private async Task<PreparedLearningSession> PrepareExamSessionAsync(Guid examId, bool isPremiumUser)
    {
        var exam = await _examRepository.GetAsync(examId);

        if (exam.Status != ExamStatus.Published || !exam.IsActive)
        {
            throw new UserFriendlyException(L["Client:ItemNotAvailable"]);
        }

        if (exam.AccessLevel == ExamAccessLevel.Premium && !isPremiumUser)
        {
            throw new UserFriendlyException(L["Client:PremiumRequired"]);
        }

        var query = await _examQuestionRepository.GetQueryableAsync();
        var assignedQuestions = await AsyncExecuter.ToListAsync(query
            .Where(x => x.ExamId == exam.Id)
            .OrderBy(x => x.SortOrder));

        if (assignedQuestions.Count < exam.TotalQuestionCount)
        {
            throw new UserFriendlyException(L["Client:ItemNotReady"]);
        }

        var selectedAssignments = exam.SelectionMode == ExamSelectionMode.Random
            ? assignedQuestions.OrderBy(_ => Guid.NewGuid()).Take(exam.TotalQuestionCount).ToList()
            : assignedQuestions.Take(exam.TotalQuestionCount).ToList();

        var preparedQuestions = await PrepareQuestionsAsync(
            selectedAssignments
                .Select((x, index) => new PreparedAssignment(x.QuestionId, index + 1, x.ScoreOverride))
                .ToList());

        return new PreparedLearningSession(
            LearningSessionSourceKind.Exam,
            exam.Id,
            exam.Code,
            exam.Title,
            exam.Description,
            exam.AccessLevel == ExamAccessLevel.Premium,
            false,
            exam.DurationMinutes,
            exam.TotalQuestionCount,
            preparedQuestions);
    }

    private async Task<PreparedLearningSession> PreparePracticeSessionAsync(Guid practiceSetId, bool isPremiumUser)
    {
        var practiceSet = await _practiceSetRepository.GetAsync(practiceSetId);

        if (practiceSet.Status != PracticeStatus.Published || !practiceSet.IsActive)
        {
            throw new UserFriendlyException(L["Client:ItemNotAvailable"]);
        }

        if (practiceSet.AccessLevel == PracticeAccessLevel.Premium && !isPremiumUser)
        {
            throw new UserFriendlyException(L["Client:PremiumRequired"]);
        }

        var query = await _practiceQuestionRepository.GetQueryableAsync();
        var assignedQuestions = await AsyncExecuter.ToListAsync(query
            .Where(x => x.PracticeSetId == practiceSet.Id)
            .OrderBy(x => x.SortOrder));

        if (assignedQuestions.Count < practiceSet.TotalQuestionCount)
        {
            throw new UserFriendlyException(L["Client:ItemNotReady"]);
        }

        var selectedAssignments = practiceSet.SelectionMode == PracticeSelectionMode.Random
            ? assignedQuestions.OrderBy(_ => Guid.NewGuid()).Take(practiceSet.TotalQuestionCount).ToList()
            : assignedQuestions.Take(practiceSet.TotalQuestionCount).ToList();

        var preparedQuestions = await PrepareQuestionsAsync(
            selectedAssignments
                .Select((x, index) => new PreparedAssignment(x.QuestionId, index + 1, null))
                .ToList());

        return new PreparedLearningSession(
            LearningSessionSourceKind.Practice,
            practiceSet.Id,
            practiceSet.Code,
            practiceSet.Title,
            practiceSet.Description,
            practiceSet.AccessLevel == PracticeAccessLevel.Premium,
            practiceSet.ShowExplanation,
            null,
            practiceSet.TotalQuestionCount,
            preparedQuestions);
    }

    private async Task<List<PreparedSessionQuestion>> PrepareQuestionsAsync(List<PreparedAssignment> assignments)
    {
        var questionIds = assignments.Select(x => x.QuestionId).ToList();
        var questionQuery = await _questionRepository.GetQueryableAsync();
        var questions = await AsyncExecuter.ToListAsync(questionQuery.Where(x => questionIds.Contains(x.Id)));
        var questionTypeIds = questions.Select(x => x.QuestionTypeId).Distinct().ToList();
        var questionTypeQuery = await _questionTypeRepository.GetQueryableAsync();
        var questionTypes = await AsyncExecuter.ToListAsync(questionTypeQuery.Where(x => questionTypeIds.Contains(x.Id)));
        var essayAnswerQuery = await _questionEssayAnswerRepository.GetQueryableAsync();
        var essayAnswers = await AsyncExecuter.ToListAsync(essayAnswerQuery.Where(x => questionIds.Contains(x.QuestionId)));
        var matchingPairQuery = await _questionMatchingPairRepository.GetQueryableAsync();
        var matchingPairs = await AsyncExecuter.ToListAsync(matchingPairQuery
            .Where(x => questionIds.Contains(x.QuestionId))
            .OrderBy(x => x.SortOrder));
        var optionQuery = await _questionOptionRepository.GetQueryableAsync();
        var options = await AsyncExecuter.ToListAsync(optionQuery
            .Where(x => questionIds.Contains(x.QuestionId))
            .OrderBy(x => x.SortOrder));

        var questionLookup = questions.ToDictionary(x => x.Id);
        var typeLookup = questionTypes.ToDictionary(x => x.Id);
        var essayAnswerLookup = essayAnswers.ToDictionary(x => x.QuestionId);
        var matchingPairLookup = matchingPairs.GroupBy(x => x.QuestionId).ToDictionary(x => x.Key, x => x.ToList());
        var optionLookup = options.GroupBy(x => x.QuestionId).ToDictionary(x => x.Key, x => x.ToList());
        var preparedQuestions = new List<PreparedSessionQuestion>();

        foreach (var assignment in assignments)
        {
            if (!questionLookup.TryGetValue(assignment.QuestionId, out var question))
            {
                throw new UserFriendlyException(L["Client:ItemNotReady"]);
            }

            if (!typeLookup.TryGetValue(question.QuestionTypeId, out var questionType))
            {
                throw new UserFriendlyException(L["Client:ItemNotReady"]);
            }

            if (question.Status != QuestionStatus.Published || !question.IsActive)
            {
                throw new UserFriendlyException(L["Client:ItemNotReady"]);
            }

            if (!IsSupportedQuestionType(questionType.Code))
            {
                throw new UserFriendlyException(L["Client:RuntimeUnsupportedQuestionType", questionType.DisplayName]);
            }

            var questionOptions = optionLookup.GetValueOrDefault(question.Id, new List<QuestionOption>())
                .OrderBy(x => x.SortOrder)
                .ToList();
            var questionMatchingPairs = matchingPairLookup.GetValueOrDefault(question.Id, new List<QuestionMatchingPair>())
                .OrderBy(x => x.SortOrder)
                .ToList();
            var questionEssayAnswer = essayAnswerLookup.GetValueOrDefault(question.Id);

            if (string.Equals(questionType.Code, QuestionTypeCodes.Matching, StringComparison.Ordinal))
            {
                if (questionMatchingPairs.Count < 2)
                {
                    throw new UserFriendlyException(L["Client:ItemNotReady"]);
                }

                if (questionMatchingPairs.Select(x => x.RightText).Distinct(StringComparer.Ordinal).Count() != questionMatchingPairs.Count)
                {
                    throw new UserFriendlyException(L["Client:ItemNotReady"]);
                }
            }
            else if (string.Equals(questionType.Code, QuestionTypeCodes.Essay, StringComparison.Ordinal))
            {
                if (questionEssayAnswer == null)
                {
                    throw new UserFriendlyException(L["Client:ItemNotReady"]);
                }
            }
            else
            {
                if (questionOptions.Count == 0)
                {
                    throw new UserFriendlyException(L["Client:ItemNotReady"]);
                }

                var correctCount = questionOptions.Count(x => x.IsCorrect);
                if (correctCount == 0)
                {
                    throw new UserFriendlyException(L["Client:ItemNotReady"]);
                }

                if (string.Equals(questionType.Code, QuestionTypeCodes.SingleChoice, StringComparison.Ordinal) && correctCount != 1)
                {
                    throw new UserFriendlyException(L["Client:ItemNotReady"]);
                }
            }

            preparedQuestions.Add(new PreparedSessionQuestion(
                question.Id,
                question.QuestionTypeId,
                questionType.Code,
                questionType.DisplayName,
                question.Title,
                question.Content,
                question.Explanation,
                assignment.SortOrder,
                assignment.ScoreOverride ?? question.Score,
                questionOptions.Select(x => new PreparedSessionQuestionOption(
                    x.Id,
                    x.Text,
                    x.IsCorrect,
                    x.SortOrder)).ToList(),
                questionEssayAnswer == null
                    ? null
                    : new PreparedSessionQuestionEssayAnswer(
                        questionEssayAnswer.SampleAnswer,
                        questionEssayAnswer.Rubric,
                        questionEssayAnswer.MaxWords),
                questionMatchingPairs.Select(x => new PreparedSessionQuestionMatchingPair(
                    x.Id,
                    x.LeftText,
                    x.RightText,
                    x.SortOrder)).ToList()));
        }

        return preparedQuestions;
    }

    private async Task<ClientLearningSessionDto> BuildSessionDtoAsync(Guid sessionId, bool includeCorrectAnswers, bool includeExplanation)
    {
        var session = await _learningSessionRepository.GetAsync(sessionId);
        var questionQuery = await _learningSessionQuestionRepository.GetQueryableAsync();
        var questions = await AsyncExecuter.ToListAsync(questionQuery
            .Where(x => x.LearningSessionId == session.Id)
            .OrderBy(x => x.SortOrder));
        var questionIds = questions.Select(x => x.Id).ToList();

        var optionQuery = await _learningSessionQuestionOptionRepository.GetQueryableAsync();
        var options = await AsyncExecuter.ToListAsync(optionQuery
            .Where(x => questionIds.Contains(x.LearningSessionQuestionId))
            .OrderBy(x => x.SortOrder));
        var essayAnswerQuery = await _learningSessionQuestionEssayAnswerRepository.GetQueryableAsync();
        var essayAnswers = await AsyncExecuter.ToListAsync(essayAnswerQuery
            .Where(x => questionIds.Contains(x.LearningSessionQuestionId)));
        var matchingPairQuery = await _learningSessionQuestionMatchingPairRepository.GetQueryableAsync();
        var matchingPairs = await AsyncExecuter.ToListAsync(matchingPairQuery
            .Where(x => questionIds.Contains(x.LearningSessionQuestionId))
            .OrderBy(x => x.SortOrder));

        var answerQuery = await _learningSessionAnswerRepository.GetQueryableAsync();
        var answers = await AsyncExecuter.ToListAsync(answerQuery.Where(x => x.LearningSessionId == session.Id));

        var optionLookup = options.GroupBy(x => x.LearningSessionQuestionId).ToDictionary(x => x.Key, x => x.ToList());
        var essayAnswerLookup = essayAnswers.ToDictionary(x => x.LearningSessionQuestionId);
        var matchingPairLookup = matchingPairs.GroupBy(x => x.LearningSessionQuestionId).ToDictionary(x => x.Key, x => x.ToList());
        var answerLookup = answers.ToDictionary(x => x.LearningSessionQuestionId);
        var answeredCount = answers.Count(x => x.IsAnswered);

        return new ClientLearningSessionDto
        {
            Id = session.Id,
            SourceKind = session.SourceKind,
            SourceId = session.SourceId,
            SourceCode = session.SourceCode,
            Title = session.Title,
            Description = session.Description,
            IsPremiumContent = session.IsPremiumContent,
            ShowExplanation = session.ShowExplanation,
            DurationMinutes = session.DurationMinutes,
            StartedAt = session.StartedAt,
            EndsAt = session.EndsAt,
            Status = session.Status,
            AnsweredCount = session.Status == LearningSessionStatus.Submitted ? session.AnsweredCount : answeredCount,
            TotalQuestionCount = session.TotalQuestionCount,
            Score = session.Score,
            CorrectCount = session.CorrectCount,
            Questions = questions.Select(question =>
            {
                var answer = answerLookup.GetValueOrDefault(question.Id);
                var selectedOptionIds = DeserializeSelectedOptionIds(answer?.SelectedOptionIdsJson);
                var matchingAnswers = DeserializeMatchingAnswers(answer?.MatchingAnswerJson);
                var questionEssayAnswer = essayAnswerLookup.GetValueOrDefault(question.Id);
                var questionMatchingPairs = matchingPairLookup.GetValueOrDefault(question.Id, new List<LearningSessionQuestionMatchingPair>());

                return new ClientLearningSessionQuestionDto
                {
                    Id = question.Id,
                    OriginalQuestionId = question.OriginalQuestionId,
                    QuestionTypeCode = question.QuestionTypeCode,
                    QuestionTypeName = question.QuestionTypeName,
                    Title = question.Title,
                    Content = question.Content,
                    Explanation = includeExplanation ? question.Explanation : null,
                    SortOrder = question.SortOrder,
                    Score = question.Score,
                    IsAnswered = answer?.IsAnswered ?? false,
                    IsCorrect = includeCorrectAnswers && (answer?.IsCorrect ?? false),
                    EssayAnswerText = answer?.EssayAnswerText,
                    EssaySampleAnswer = includeCorrectAnswers ? questionEssayAnswer?.SampleAnswer : null,
                    EssayRubric = includeCorrectAnswers ? questionEssayAnswer?.Rubric : null,
                    EssayMaxWords = questionEssayAnswer?.MaxWords,
                    SelectedOptionIds = selectedOptionIds,
                    Options = optionLookup.GetValueOrDefault(question.Id, new List<LearningSessionQuestionOption>())
                        .Select(option => new ClientLearningSessionOptionDto
                        {
                            Id = option.Id,
                            Text = option.Text,
                            SortOrder = option.SortOrder,
                            IsSelected = selectedOptionIds.Contains(option.Id),
                            IsCorrect = includeCorrectAnswers && option.IsCorrect
                        })
                        .ToList(),
                    MatchingChoices = questionMatchingPairs
                        .Select(x => x.RightText)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(x => x)
                        .ToList(),
                    MatchingPairs = questionMatchingPairs
                        .Select(pair => new ClientLearningSessionMatchingPairDto
                        {
                            Id = pair.Id,
                            OriginalMatchingPairId = pair.OriginalQuestionMatchingPairId,
                            LeftText = pair.LeftText,
                            CorrectRightText = includeCorrectAnswers ? pair.RightText : string.Empty,
                            SelectedRightText = matchingAnswers.FirstOrDefault(x => x.PairId == pair.Id)?.SelectedRightText,
                            SortOrder = pair.SortOrder,
                            IsCorrect = includeCorrectAnswers &&
                                        string.Equals(
                                            matchingAnswers.FirstOrDefault(x => x.PairId == pair.Id)?.SelectedRightText,
                                            pair.RightText,
                                            StringComparison.Ordinal)
                        })
                        .ToList()
                };
            }).ToList()
        };
    }

    private async Task SubmitInternalAsync(LearningSession session, DateTime submittedAt)
    {
        if (session.Status != LearningSessionStatus.InProgress)
        {
            return;
        }

        var questionQuery = await _learningSessionQuestionRepository.GetQueryableAsync();
        var questions = await AsyncExecuter.ToListAsync(questionQuery.Where(x => x.LearningSessionId == session.Id));
        var answerQuery = await _learningSessionAnswerRepository.GetQueryableAsync();
        var answers = await AsyncExecuter.ToListAsync(answerQuery.Where(x => x.LearningSessionId == session.Id));
        var answerLookup = answers.ToDictionary(x => x.LearningSessionQuestionId);

        var correctCount = 0;
        var answeredCount = 0;
        decimal score = 0;

        foreach (var question in questions)
        {
            if (!answerLookup.TryGetValue(question.Id, out var answer))
            {
                continue;
            }

            if (answer.IsAnswered)
            {
                answeredCount++;
            }

            if (answer.IsCorrect)
            {
                correctCount++;
                score += question.Score;
            }
        }

        session.Submit(submittedAt, score, correctCount, answeredCount);
        await _learningSessionRepository.UpdateAsync(session, autoSave: true);
    }

    private async Task EnsureSessionCanBeAnsweredAsync(LearningSession session)
    {
        await AutoSubmitIfExpiredAsync(session);

        if (session.Status != LearningSessionStatus.InProgress)
        {
            throw new UserFriendlyException(L["Client:SessionNotWritable"]);
        }
    }

    private async Task AutoSubmitIfExpiredAsync(LearningSession session)
    {
        if (session.Status == LearningSessionStatus.InProgress &&
            session.EndsAt.HasValue &&
            session.EndsAt.Value <= Clock.Now)
        {
            await SubmitInternalAsync(session, session.EndsAt.Value);
        }
    }

    private async Task<LearningSession?> FindInProgressSessionAsync(Guid userId, LearningSessionSourceKind sourceKind, Guid sourceId)
    {
        var query = await _learningSessionRepository.GetQueryableAsync();
        return await AsyncExecuter.FirstOrDefaultAsync(query
            .Where(x => x.UserId == userId &&
                        x.SourceKind == sourceKind &&
                        x.SourceId == sourceId &&
                        x.Status == LearningSessionStatus.InProgress)
            .OrderByDescending(x => x.StartedAt));
    }

    private async Task<LearningSession> GetOwnedSessionAsync(Guid sessionId)
    {
        var userId = GetCurrentUserId();
        var session = await _learningSessionRepository.GetAsync(sessionId);

        if (session.UserId != userId)
        {
            throw new UserFriendlyException(L["Client:SessionNotFound"]);
        }

        return session;
    }

    private void ValidateObjectiveAnswerInput(
        string questionTypeCode,
        List<LearningSessionQuestionOption> options,
        List<Guid> selectedOptionIds)
    {
        var normalized = selectedOptionIds.Distinct().ToList();
        var optionIdSet = options.Select(x => x.Id).ToHashSet();

        if (normalized.Any(x => !optionIdSet.Contains(x)))
        {
            throw new UserFriendlyException(L["Client:InvalidAnswerSelection"]);
        }

        if (questionTypeCode == QuestionTypeCodes.SingleChoice && normalized.Count > 1)
        {
            throw new UserFriendlyException(L["Client:SingleChoiceOnlyOneAnswer"]);
        }
    }

    private List<SaveClientLearningMatchingAnswerDto> ValidateMatchingAnswerInput(
        List<LearningSessionQuestionMatchingPair> matchingPairs,
        List<SaveClientLearningMatchingAnswerDto> matchingAnswers)
    {
        var normalized = matchingAnswers
            .GroupBy(x => x.PairId)
            .Select(x => x.Last())
            .ToList();
        var pairIdSet = matchingPairs.Select(x => x.Id).ToHashSet();
        var allowedRightTexts = matchingPairs
            .Select(x => x.RightText)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        if (normalized.Any(x => !pairIdSet.Contains(x.PairId)))
        {
            throw new UserFriendlyException(L["Client:InvalidMatchingSelection"]);
        }

        var selectedRightTexts = normalized
            .Where(x => !x.SelectedRightText.IsNullOrWhiteSpace())
            .Select(x => x.SelectedRightText!.Trim())
            .ToList();

        if (selectedRightTexts.Any(x => !allowedRightTexts.Contains(x)))
        {
            throw new UserFriendlyException(L["Client:InvalidMatchingSelection"]);
        }

        if (selectedRightTexts.Count != selectedRightTexts.Distinct(StringComparer.Ordinal).Count())
        {
            throw new UserFriendlyException(L["Client:MatchingRightChoiceAlreadyUsed"]);
        }

        return normalized.Select(x => new SaveClientLearningMatchingAnswerDto
        {
            PairId = x.PairId,
            SelectedRightText = x.SelectedRightText?.Trim()
        }).ToList();
    }

    private string? ValidateEssayAnswerInput(
        LearningSessionQuestionEssayAnswer? essayAnswer,
        string? essayAnswerText)
    {
        if (essayAnswer == null)
        {
            throw new UserFriendlyException(L["Client:ItemNotReady"]);
        }

        var normalized = essayAnswerText?.Trim();
        if (normalized.IsNullOrWhiteSpace())
        {
            return null;
        }

        if (normalized.Length > LearningSessionConsts.MaxEssayAnswerTextLength)
        {
            throw new UserFriendlyException(L["Client:EssayAnswerTooLong"]);
        }

        if (essayAnswer.MaxWords.HasValue && CountWords(normalized) > essayAnswer.MaxWords.Value)
        {
            throw new UserFriendlyException(L["Client:EssayMaxWordsExceeded", essayAnswer.MaxWords.Value]);
        }

        return normalized;
    }

    private static bool IsSupportedQuestionType(string questionTypeCode)
    {
        return string.Equals(questionTypeCode, QuestionTypeCodes.SingleChoice, StringComparison.Ordinal) ||
               string.Equals(questionTypeCode, QuestionTypeCodes.MultipleChoice, StringComparison.Ordinal) ||
               string.Equals(questionTypeCode, QuestionTypeCodes.Matching, StringComparison.Ordinal) ||
               string.Equals(questionTypeCode, QuestionTypeCodes.Essay, StringComparison.Ordinal);
    }

    private static LearningSessionSourceKind MapSourceKind(ClientLearningItemKind kind)
    {
        return kind == ClientLearningItemKind.Exam
            ? LearningSessionSourceKind.Exam
            : LearningSessionSourceKind.Practice;
    }

    private Guid GetCurrentUserId()
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new UserFriendlyException(L["Client:AuthRequired"]);
        }

        return CurrentUser.Id.Value;
    }

    private static List<Guid> DeserializeSelectedOptionIds(string? selectedOptionIdsJson)
    {
        if (string.IsNullOrWhiteSpace(selectedOptionIdsJson))
        {
            return new List<Guid>();
        }

        return JsonSerializer.Deserialize<List<Guid>>(selectedOptionIdsJson) ?? new List<Guid>();
    }

    private static List<SaveClientLearningMatchingAnswerDto> DeserializeMatchingAnswers(string? matchingAnswerJson)
    {
        if (string.IsNullOrWhiteSpace(matchingAnswerJson))
        {
            return new List<SaveClientLearningMatchingAnswerDto>();
        }

        return JsonSerializer.Deserialize<List<SaveClientLearningMatchingAnswerDto>>(matchingAnswerJson) ??
               new List<SaveClientLearningMatchingAnswerDto>();
    }

    private static int CountWords(string text)
    {
        return text
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Length;
    }

    private sealed record PreparedLearningSession(
        LearningSessionSourceKind SourceKind,
        Guid SourceId,
        string SourceCode,
        string Title,
        string? Description,
        bool IsPremiumContent,
        bool ShowExplanation,
        int? DurationMinutes,
        int TotalQuestionCount,
        List<PreparedSessionQuestion> Questions);

    private sealed record PreparedAssignment(Guid QuestionId, int SortOrder, decimal? ScoreOverride);

    private sealed record PreparedSessionQuestion(
        Guid OriginalQuestionId,
        Guid QuestionTypeId,
        string QuestionTypeCode,
        string QuestionTypeName,
        string Title,
        string Content,
        string? Explanation,
        int SortOrder,
        decimal Score,
        List<PreparedSessionQuestionOption> Options,
        PreparedSessionQuestionEssayAnswer? EssayAnswer,
        List<PreparedSessionQuestionMatchingPair> MatchingPairs);

    private sealed record PreparedSessionQuestionOption(
        Guid OriginalOptionId,
        string Text,
        bool IsCorrect,
        int SortOrder);

    private sealed record PreparedSessionQuestionEssayAnswer(
        string? SampleAnswer,
        string? Rubric,
        int? MaxWords);

    private sealed record PreparedSessionQuestionMatchingPair(
        Guid OriginalMatchingPairId,
        string LeftText,
        string RightText,
        int SortOrder);
}

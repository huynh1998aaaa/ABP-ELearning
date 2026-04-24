using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.ClientContent;
using Elearning.Exams;
using Elearning.LearningSessions;
using Elearning.Questions;
using Elearning.QuestionTypes;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Elearning.EntityFrameworkCore.Applications;

[Collection(ElearningTestConsts.CollectionDefinitionName)]
public class ClientLearningSessionAppServiceTests : ElearningEntityFrameworkCoreTestBase
{
    private readonly IClientLearningSessionAppService _clientLearningSessionAppService;
    private readonly IExamAppService _examAppService;
    private readonly IQuestionAppService _questionAppService;
    private readonly IRepository<QuestionType, Guid> _questionTypeRepository;

    public ClientLearningSessionAppServiceTests()
    {
        _clientLearningSessionAppService = GetRequiredService<IClientLearningSessionAppService>();
        _examAppService = GetRequiredService<IExamAppService>();
        _questionAppService = GetRequiredService<IQuestionAppService>();
        _questionTypeRepository = GetRequiredService<IRepository<QuestionType, Guid>>();
    }

    [Fact]
    public async Task StartOrResume_Should_Reuse_InProgress_Session()
    {
        var exam = await CreatePublishedExamAsync(QuestionTypeCodes.SingleChoice);

        var firstLaunch = await _clientLearningSessionAppService.StartOrResumeAsync(ClientLearningItemKind.Exam, exam.Id);
        var secondLaunch = await _clientLearningSessionAppService.StartOrResumeAsync(ClientLearningItemKind.Exam, exam.Id);

        Assert.Equal(firstLaunch.SessionId, secondLaunch.SessionId);
    }

    [Fact]
    public async Task Submit_Should_Calculate_Objective_Score()
    {
        var exam = await CreatePublishedExamAsync(QuestionTypeCodes.SingleChoice);
        var launch = await _clientLearningSessionAppService.StartOrResumeAsync(ClientLearningItemKind.Exam, exam.Id);
        var session = await _clientLearningSessionAppService.GetAsync(launch.SessionId);
        var firstQuestion = Assert.Single(session.Questions);
        var correctOption = Assert.Single(firstQuestion.Options, x => x.Text == "Option A");

        await _clientLearningSessionAppService.SaveAnswerAsync(session.Id, new SaveClientLearningAnswerDto
        {
            QuestionId = firstQuestion.Id,
            SelectedOptionIds = new List<Guid> { correctOption.Id }
        });

        var result = await _clientLearningSessionAppService.SubmitAsync(session.Id);

        Assert.Equal(1, result.CorrectCount);
        Assert.Equal(1, result.AnsweredCount);
        Assert.Equal(1m, result.Score);
        Assert.Single(result.Questions);
        Assert.True(result.Questions[0].IsCorrect);
    }

    [Fact]
    public async Task Submit_Should_Calculate_Matching_Score()
    {
        var exam = await CreatePublishedExamAsync(QuestionTypeCodes.Matching);
        var launch = await _clientLearningSessionAppService.StartOrResumeAsync(ClientLearningItemKind.Exam, exam.Id);
        var session = await _clientLearningSessionAppService.GetAsync(launch.SessionId);
        var firstQuestion = Assert.Single(session.Questions);

        Assert.Equal(QuestionTypeCodes.Matching, firstQuestion.QuestionTypeCode);
        Assert.Equal(2, firstQuestion.MatchingPairs.Count);
        Assert.Equal(2, firstQuestion.MatchingChoices.Count);

        await _clientLearningSessionAppService.SaveAnswerAsync(session.Id, new SaveClientLearningAnswerDto
        {
            QuestionId = firstQuestion.Id,
            MatchingAnswers = new List<SaveClientLearningMatchingAnswerDto>
            {
                new()
                {
                    PairId = firstQuestion.MatchingPairs[0].Id,
                    SelectedRightText = "1"
                },
                new()
                {
                    PairId = firstQuestion.MatchingPairs[1].Id,
                    SelectedRightText = "2"
                }
            }
        });

        var result = await _clientLearningSessionAppService.SubmitAsync(session.Id);

        Assert.Equal(1, result.CorrectCount);
        Assert.Equal(1, result.AnsweredCount);
        Assert.Equal(1m, result.Score);
        Assert.Single(result.Questions);
        Assert.True(result.Questions[0].IsCorrect);
    }

    [Fact]
    public async Task Submit_Should_Keep_Essay_As_Pending_Manual_Grading()
    {
        var exam = await CreatePublishedExamAsync(QuestionTypeCodes.Essay);
        var launch = await _clientLearningSessionAppService.StartOrResumeAsync(ClientLearningItemKind.Exam, exam.Id);
        var session = await _clientLearningSessionAppService.GetAsync(launch.SessionId);
        var firstQuestion = Assert.Single(session.Questions);

        Assert.Equal(QuestionTypeCodes.Essay, firstQuestion.QuestionTypeCode);
        Assert.Equal(500, firstQuestion.EssayMaxWords);

        await _clientLearningSessionAppService.SaveAnswerAsync(session.Id, new SaveClientLearningAnswerDto
        {
            QuestionId = firstQuestion.Id,
            EssayAnswerText = "This is the submitted essay answer."
        });

        var refreshedSession = await _clientLearningSessionAppService.GetAsync(session.Id);
        Assert.Equal("This is the submitted essay answer.", Assert.Single(refreshedSession.Questions).EssayAnswerText);

        var result = await _clientLearningSessionAppService.SubmitAsync(session.Id);

        Assert.Equal(0, result.CorrectCount);
        Assert.Equal(1, result.AnsweredCount);
        Assert.Equal(0m, result.Score);
        Assert.Equal(1, result.PendingManualGradingCount);
        Assert.Single(result.Questions);
        Assert.Equal("This is the submitted essay answer.", result.Questions[0].EssayAnswerText);
        Assert.False(result.Questions[0].IsCorrect);
    }

    [Fact]
    public async Task SaveAnswer_Should_Reject_Essay_That_Exceeds_MaxWords()
    {
        var exam = await CreatePublishedExamAsync(QuestionTypeCodes.Essay);
        var launch = await _clientLearningSessionAppService.StartOrResumeAsync(ClientLearningItemKind.Exam, exam.Id);
        var session = await _clientLearningSessionAppService.GetAsync(launch.SessionId);
        var firstQuestion = Assert.Single(session.Questions);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _clientLearningSessionAppService.SaveAnswerAsync(session.Id, new SaveClientLearningAnswerDto
            {
                QuestionId = firstQuestion.Id,
                EssayAnswerText = CreateLongEssay(501)
            }));
    }

    private async Task<ExamDto> CreatePublishedExamAsync(string questionTypeCode)
    {
        var questionType = await _questionTypeRepository.GetAsync(x => x.Code == questionTypeCode);
        var question = questionTypeCode == QuestionTypeCodes.Matching
            ? await _questionAppService.CreateAsync(new CreateQuestionDto
            {
                QuestionTypeId = questionType.Id,
                Title = $"Question {Guid.NewGuid():N}"[..18],
                Content = "Match the correct answers",
                Difficulty = QuestionDifficulty.Medium,
                Score = 1,
                SortOrder = 100,
                IsActive = true,
                MatchingPairs = new List<QuestionMatchingPairInputDto>
                {
                    new() { LeftText = "A", RightText = "1", SortOrder = 1 },
                    new() { LeftText = "B", RightText = "2", SortOrder = 2 }
                },
                Options = new List<QuestionOptionInputDto>(),
                EssayAnswer = new QuestionEssayAnswerInputDto()
            })
            : questionTypeCode == QuestionTypeCodes.Essay
            ? await _questionAppService.CreateAsync(new CreateQuestionDto
            {
                QuestionTypeId = questionType.Id,
                Title = $"Question {Guid.NewGuid():N}"[..18],
                Content = "Explain the answer",
                Difficulty = QuestionDifficulty.Medium,
                Score = 1,
                SortOrder = 100,
                IsActive = true,
                Options = new List<QuestionOptionInputDto>(),
                MatchingPairs = new List<QuestionMatchingPairInputDto>(),
                EssayAnswer = new QuestionEssayAnswerInputDto
                {
                    SampleAnswer = "Sample answer",
                    Rubric = "Rubric",
                    MaxWords = 500
                }
            })
            : await _questionAppService.CreateAsync(new CreateQuestionDto
            {
                QuestionTypeId = questionType.Id,
                Title = $"Question {Guid.NewGuid():N}"[..18],
                Content = "What is the correct answer?",
                Difficulty = QuestionDifficulty.Medium,
                Score = 1,
                SortOrder = 100,
                IsActive = true,
                Options = new List<QuestionOptionInputDto>
                {
                    new() { Text = "Option A", IsCorrect = true, SortOrder = 1 },
                    new() { Text = "Option B", IsCorrect = false, SortOrder = 2 }
                },
                MatchingPairs = new List<QuestionMatchingPairInputDto>(),
                EssayAnswer = new QuestionEssayAnswerInputDto()
            });

        await _questionAppService.PublishAsync(question.Id);

        var exam = await _examAppService.CreateAsync(new CreateExamDto
        {
            Code = $"runtime_{Guid.NewGuid():N}"[..18],
            Title = "Runtime exam",
            AccessLevel = ExamAccessLevel.Free,
            SelectionMode = ExamSelectionMode.Fixed,
            DurationMinutes = 60,
            TotalQuestionCount = 1,
            SortOrder = 100,
            IsActive = true
        });

        await _examAppService.AddQuestionAsync(exam.Id, new AddExamQuestionDto
        {
            QuestionId = question.Id
        });

        await _examAppService.PublishAsync(exam.Id);
        return exam;
    }

    private static string CreateLongEssay(int wordCount)
    {
        return string.Join(' ', Enumerable.Repeat("essay", wordCount));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elearning.Exams;
using Elearning.PremiumSubscriptions;
using Elearning.Practices;
using Elearning.Questions;
using Elearning.QuestionTypes;
using Elearning.Subjects;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Xunit;

namespace Elearning.EntityFrameworkCore.Applications;

[Collection(ElearningTestConsts.CollectionDefinitionName)]
public class AdminModuleValidationTests : ElearningEntityFrameworkCoreTestBase
{
    private readonly IExamAppService _examAppService;
    private readonly IUserPremiumSubscriptionAppService _premiumSubscriptionAppService;
    private readonly IPracticeSetAppService _practiceSetAppService;
    private readonly IQuestionAppService _questionAppService;
    private readonly IQuestionTypeAppService _questionTypeAppService;
    private readonly ISubjectAppService _subjectAppService;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<PremiumPlan, Guid> _premiumPlanRepository;
    private readonly IRepository<PracticeQuestion, Guid> _practiceQuestionRepository;
    private readonly IRepository<UserPremiumSubscription, Guid> _subscriptionRepository;

    public AdminModuleValidationTests()
    {
        _examAppService = GetRequiredService<IExamAppService>();
        _premiumSubscriptionAppService = GetRequiredService<IUserPremiumSubscriptionAppService>();
        _practiceSetAppService = GetRequiredService<IPracticeSetAppService>();
        _questionAppService = GetRequiredService<IQuestionAppService>();
        _questionTypeAppService = GetRequiredService<IQuestionTypeAppService>();
        _subjectAppService = GetRequiredService<ISubjectAppService>();
        _identityUserRepository = GetRequiredService<IRepository<IdentityUser, Guid>>();
        _premiumPlanRepository = GetRequiredService<IRepository<PremiumPlan, Guid>>();
        _practiceQuestionRepository = GetRequiredService<IRepository<PracticeQuestion, Guid>>();
        _subscriptionRepository = GetRequiredService<IRepository<UserPremiumSubscription, Guid>>();
    }

    [Fact]
    public async Task Exam_AddQuestion_Should_Reject_Draft_Question()
    {
        var questionTypeId = await CreateChoiceQuestionTypeAsync();
        var draftQuestion = await CreateChoiceQuestionAsync(questionTypeId, publish: false);
        var exam = await CreateExamAsync();

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _examAppService.AddQuestionAsync(exam.Id, new AddExamQuestionDto
            {
                QuestionId = draftQuestion.Id
            }));
    }

    [Fact]
    public async Task Exam_Publish_Should_Reject_Inactive_Exam()
    {
        var questionTypeId = await CreateChoiceQuestionTypeAsync();
        var publishedQuestion = await CreateChoiceQuestionAsync(questionTypeId, publish: true);
        var exam = await CreateExamAsync();

        await _examAppService.AddQuestionAsync(exam.Id, new AddExamQuestionDto
        {
            QuestionId = publishedQuestion.Id
        });

        await _examAppService.DeactivateAsync(exam.Id);

        await Assert.ThrowsAsync<UserFriendlyException>(() => _examAppService.PublishAsync(exam.Id));
    }

    [Fact]
    public async Task Practice_Publish_Should_Reject_Assigned_Unpublished_Question()
    {
        var questionTypeId = await CreateChoiceQuestionTypeAsync();
        var draftQuestion = await CreateChoiceQuestionAsync(questionTypeId, publish: false);
        var practiceSet = await CreatePracticeSetAsync();

        await _practiceQuestionRepository.InsertAsync(new PracticeQuestion(
            Guid.NewGuid(),
            practiceSet.Id,
            draftQuestion.Id,
            10,
            isRequired: false), autoSave: true);

        await Assert.ThrowsAsync<UserFriendlyException>(() => _practiceSetAppService.PublishAsync(practiceSet.Id));
    }

    [Fact]
    public async Task QuestionType_Create_Should_Reject_Invalid_Option_Range()
    {
        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _questionTypeAppService.CreateAsync(new CreateQuestionTypeDto
            {
                Code = $"choice_{Guid.NewGuid():N}"[..14],
                DisplayName = "Invalid choice",
                InputKind = QuestionInputKind.SingleChoice,
                ScoringKind = QuestionScoringKind.Auto,
                SupportsOptions = true,
                MinimumOptions = 4,
                MaximumOptions = 2,
                SortOrder = 100,
                IsActive = true
            }));
    }

    [Fact]
    public async Task Subject_Create_Should_Reject_Duplicate_Code_Even_After_Soft_Delete()
    {
        var created = await _subjectAppService.CreateAsync(new CreateSubjectDto
        {
            Code = $"subject_{Guid.NewGuid():N}"[..16],
            Name = "Networking",
            SortOrder = 10,
            IsActive = true
        });

        await _subjectAppService.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _subjectAppService.CreateAsync(new CreateSubjectDto
            {
                Code = created.Code,
                Name = "Networking copy",
                SortOrder = 20,
                IsActive = true
            }));
    }

    [Fact]
    public async Task Subject_Update_Should_Reject_Duplicate_Code()
    {
        var first = await _subjectAppService.CreateAsync(new CreateSubjectDto
        {
            Code = $"subject_{Guid.NewGuid():N}"[..16],
            Name = "Security",
            SortOrder = 10,
            IsActive = true
        });

        var second = await _subjectAppService.CreateAsync(new CreateSubjectDto
        {
            Code = $"subject_{Guid.NewGuid():N}"[..16],
            Name = "Automation",
            SortOrder = 20,
            IsActive = true
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _subjectAppService.UpdateAsync(second.Id, new UpdateSubjectDto
            {
                Code = first.Code,
                Name = "Automation updated",
                Description = "desc",
                SortOrder = 30
            }));
    }

    [Fact]
    public async Task Premium_Extend_Should_Reject_Expired_Subscription()
    {
        var userId = await GetAdminUserIdAsync();
        var planId = Guid.NewGuid();

        await _premiumPlanRepository.InsertAsync(new PremiumPlan(
            planId,
            $"premium_{Guid.NewGuid():N}"[..18],
            "Premium 6 months",
            PremiumPlanType.SixMonths,
            6,
            0,
            "VND",
            100), autoSave: true);

        var expiredSubscription = new UserPremiumSubscription(
            Guid.NewGuid(),
            userId,
            planId,
            activationNumber: 1,
            activatedTime: DateTime.UtcNow.AddMonths(-7),
            durationMonths: 6);

        await _subscriptionRepository.InsertAsync(expiredSubscription, autoSave: true);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _premiumSubscriptionAppService.ExtendAsync(expiredSubscription.Id));
    }

    [Fact]
    public async Task Premium_Delete_Should_Reject_Active_Subscription()
    {
        var userId = await GetAdminUserIdAsync();
        var planId = await CreatePremiumPlanAsync();

        var activeSubscription = new UserPremiumSubscription(
            Guid.NewGuid(),
            userId,
            planId,
            activationNumber: 1,
            activatedTime: DateTime.UtcNow,
            durationMonths: 6);

        await _subscriptionRepository.InsertAsync(activeSubscription, autoSave: true);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _premiumSubscriptionAppService.DeleteAsync(activeSubscription.Id));
    }

    [Fact]
    public async Task Premium_Delete_Should_SoftDelete_Cancelled_Subscription()
    {
        var userId = await GetAdminUserIdAsync();
        var planId = await CreatePremiumPlanAsync();

        var cancelledSubscription = new UserPremiumSubscription(
            Guid.NewGuid(),
            userId,
            planId,
            activationNumber: 1,
            activatedTime: DateTime.UtcNow.AddMonths(-1),
            durationMonths: 6);

        cancelledSubscription.Cancel(DateTime.UtcNow, "cleanup");
        await _subscriptionRepository.InsertAsync(cancelledSubscription, autoSave: true);

        await _premiumSubscriptionAppService.DeleteAsync(cancelledSubscription.Id);

        var visibleItems = await _premiumSubscriptionAppService.GetListAsync(new GetUserPremiumSubscriptionListInput
        {
            MaxResultCount = 50,
            SkipCount = 0
        });

        Assert.DoesNotContain(visibleItems.Items, x => x.Id == cancelledSubscription.Id);
    }

    [Fact]
    public async Task Exam_AutoAssign_Should_Assign_Partial_And_Preserve_Manual()
    {
        var questionTypeId = await CreateChoiceQuestionTypeAsync();
        var manualQuestion = await CreateChoiceQuestionAsync(questionTypeId, publish: true, QuestionDifficulty.Medium);
        var autoQuestion1 = await CreateChoiceQuestionAsync(questionTypeId, publish: true, QuestionDifficulty.Medium);
        var autoQuestion2 = await CreateChoiceQuestionAsync(questionTypeId, publish: true, QuestionDifficulty.Medium);
        var exam = await _examAppService.CreateAsync(new CreateExamDto
        {
            Code = $"exam_{Guid.NewGuid():N}"[..18],
            Title = "Auto assign exam",
            AccessLevel = ExamAccessLevel.Free,
            SelectionMode = ExamSelectionMode.Fixed,
            DurationMinutes = 60,
            TotalQuestionCount = 4,
            SortOrder = 100,
            IsActive = true
        });

        await _examAppService.AddQuestionAsync(exam.Id, new AddExamQuestionDto
        {
            QuestionId = manualQuestion.Id
        });

        await _examAppService.AddAutoQuestionRuleAsync(exam.Id, new CreateExamAutoQuestionRuleDto
        {
            QuestionTypeId = questionTypeId,
            Difficulty = QuestionDifficulty.Medium,
            TargetCount = 4,
            SortOrder = 10
        });

        var result = await _examAppService.ApplyAutoAssignAsync(exam.Id);

        Assert.Equal(4, result.RequestedCount);
        Assert.Equal(1, result.FulfilledByManualCount);
        Assert.Equal(2, result.AssignedCount);
        Assert.Equal(1, result.ShortageCount);
        Assert.Equal(1, result.PreservedManualCount);

        var assignedQuestions = (await _examAppService.GetQuestionsAsync(exam.Id, new GetExamQuestionListInput
        {
            MaxResultCount = 20,
            SkipCount = 0
        })).Items;

        Assert.Equal(3, assignedQuestions.Count);
        Assert.Single(assignedQuestions, x => x.AssignmentSource == QuestionAssignmentSource.Manual);
        Assert.Equal(2, assignedQuestions.Count(x => x.AssignmentSource == QuestionAssignmentSource.Auto));

        await Assert.ThrowsAsync<UserFriendlyException>(() => _examAppService.PublishAsync(exam.Id));
    }

    [Fact]
    public async Task Practice_AutoAssign_Should_Assign_Available_Questions()
    {
        var questionTypeId = await CreateChoiceQuestionTypeAsync();
        await CreateChoiceQuestionAsync(questionTypeId, publish: true, QuestionDifficulty.Easy);
        await CreateChoiceQuestionAsync(questionTypeId, publish: true, QuestionDifficulty.Easy);
        var practiceSet = await _practiceSetAppService.CreateAsync(new CreatePracticeSetDto
        {
            Code = $"practice_{Guid.NewGuid():N}"[..20],
            Title = "Auto assign practice",
            AccessLevel = PracticeAccessLevel.Free,
            SelectionMode = PracticeSelectionMode.Fixed,
            TotalQuestionCount = 2,
            SortOrder = 100,
            IsActive = true,
            ShowExplanation = true
        });

        await _practiceSetAppService.AddAutoQuestionRuleAsync(practiceSet.Id, new CreatePracticeAutoQuestionRuleDto
        {
            QuestionTypeId = questionTypeId,
            Difficulty = QuestionDifficulty.Easy,
            TargetCount = 2,
            SortOrder = 10
        });

        var preview = await _practiceSetAppService.PreviewAutoAssignAsync(practiceSet.Id);
        Assert.Equal(2, preview.AutoAssignableCount);
        Assert.Equal(0, preview.ShortageCount);

        var result = await _practiceSetAppService.ApplyAutoAssignAsync(practiceSet.Id);

        Assert.Equal(2, result.AssignedCount);
        Assert.Equal(0, result.ShortageCount);

        var assignedQuestions = (await _practiceSetAppService.GetQuestionsAsync(practiceSet.Id, new GetPracticeQuestionListInput
        {
            MaxResultCount = 20,
            SkipCount = 0
        })).Items;

        Assert.Equal(2, assignedQuestions.Count);
        Assert.All(assignedQuestions, x => Assert.Equal(QuestionAssignmentSource.Auto, x.AssignmentSource));
    }

    private async Task<Guid> CreateChoiceQuestionTypeAsync()
    {
        var result = await _questionTypeAppService.CreateAsync(new CreateQuestionTypeDto
        {
            Code = $"single_{Guid.NewGuid():N}"[..16],
            DisplayName = "Single choice",
            InputKind = QuestionInputKind.SingleChoice,
            ScoringKind = QuestionScoringKind.Auto,
            SupportsOptions = true,
            SupportsAnswerPairs = false,
            RequiresManualGrading = false,
            AllowMultipleCorrectAnswers = false,
            MinimumOptions = 2,
            MaximumOptions = 4,
            SortOrder = 100,
            IsActive = true
        });

        return result.Id;
    }

    private async Task<QuestionDto> CreateChoiceQuestionAsync(Guid questionTypeId, bool publish, QuestionDifficulty difficulty = QuestionDifficulty.Medium)
    {
        var question = await _questionAppService.CreateAsync(new CreateQuestionDto
        {
            QuestionTypeId = questionTypeId,
            Title = $"Question {Guid.NewGuid():N}"[..18],
            Content = "What is the correct answer?",
            Difficulty = difficulty,
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

        if (publish)
        {
            await _questionAppService.PublishAsync(question.Id);
            question = await _questionAppService.GetAsync(question.Id);
        }

        return question;
    }

    private async Task<ExamDto> CreateExamAsync()
    {
        return await _examAppService.CreateAsync(new CreateExamDto
        {
            Code = $"exam_{Guid.NewGuid():N}"[..18],
            Title = "Validation exam",
            AccessLevel = ExamAccessLevel.Free,
            SelectionMode = ExamSelectionMode.Fixed,
            DurationMinutes = 60,
            TotalQuestionCount = 1,
            SortOrder = 100,
            IsActive = true
        });
    }

    private async Task<PracticeSetDto> CreatePracticeSetAsync()
    {
        return await _practiceSetAppService.CreateAsync(new CreatePracticeSetDto
        {
            Code = $"practice_{Guid.NewGuid():N}"[..20],
            Title = "Validation practice",
            AccessLevel = PracticeAccessLevel.Free,
            SelectionMode = PracticeSelectionMode.Fixed,
            TotalQuestionCount = 1,
            SortOrder = 100,
            IsActive = true,
            ShowExplanation = true
        });
    }

    private async Task<Guid> CreatePremiumPlanAsync()
    {
        var planId = Guid.NewGuid();

        await _premiumPlanRepository.InsertAsync(new PremiumPlan(
            planId,
            $"premium_{Guid.NewGuid():N}"[..18],
            "Premium 6 months",
            PremiumPlanType.SixMonths,
            6,
            0,
            "VND",
            100), autoSave: true);

        return planId;
    }

    private async Task<Guid> GetAdminUserIdAsync()
    {
        var adminUser = await _identityUserRepository.FindAsync(x => x.UserName == "admin");
        Assert.NotNull(adminUser);
        return adminUser!.Id;
    }
}

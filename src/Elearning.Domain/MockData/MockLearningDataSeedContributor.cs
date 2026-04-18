using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Elearning.Exams;
using Elearning.LearningSessions;
using Elearning.Practices;
using Elearning.PremiumSubscriptions;
using Elearning.Questions;
using Elearning.QuestionTypes;
using Elearning.Subjects;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.Uow;

namespace Elearning.MockData;

public class MockLearningDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private const string MockUserPassword = "Mock@123456";

    private readonly IRepository<Subject, Guid> _subjectRepository;
    private readonly IRepository<QuestionType, Guid> _questionTypeRepository;
    private readonly IRepository<Question, Guid> _questionRepository;
    private readonly IRepository<QuestionOption, Guid> _questionOptionRepository;
    private readonly IRepository<QuestionMatchingPair, Guid> _questionMatchingPairRepository;
    private readonly IRepository<QuestionEssayAnswer, Guid> _questionEssayAnswerRepository;
    private readonly IRepository<Exam, Guid> _examRepository;
    private readonly IRepository<ExamQuestion, Guid> _examQuestionRepository;
    private readonly IRepository<PracticeSet, Guid> _practiceSetRepository;
    private readonly IRepository<PracticeQuestion, Guid> _practiceQuestionRepository;
    private readonly IRepository<PremiumPlan, Guid> _premiumPlanRepository;
    private readonly IRepository<UserPremiumSubscription, Guid> _userPremiumSubscriptionRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<LearningSession, Guid> _learningSessionRepository;
    private readonly IRepository<LearningSessionQuestion, Guid> _learningSessionQuestionRepository;
    private readonly IRepository<LearningSessionQuestionOption, Guid> _learningSessionQuestionOptionRepository;
    private readonly IRepository<LearningSessionQuestionMatchingPair, Guid> _learningSessionQuestionMatchingPairRepository;
    private readonly IRepository<LearningSessionQuestionEssayAnswer, Guid> _learningSessionQuestionEssayAnswerRepository;
    private readonly IRepository<LearningSessionAnswer, Guid> _learningSessionAnswerRepository;
    private readonly IdentityUserManager _identityUserManager;
    private readonly QuestionTypeDataSeedContributor _questionTypeDataSeedContributor;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public MockLearningDataSeedContributor(
        IRepository<Subject, Guid> subjectRepository,
        IRepository<QuestionType, Guid> questionTypeRepository,
        IRepository<Question, Guid> questionRepository,
        IRepository<QuestionOption, Guid> questionOptionRepository,
        IRepository<QuestionMatchingPair, Guid> questionMatchingPairRepository,
        IRepository<QuestionEssayAnswer, Guid> questionEssayAnswerRepository,
        IRepository<Exam, Guid> examRepository,
        IRepository<ExamQuestion, Guid> examQuestionRepository,
        IRepository<PracticeSet, Guid> practiceSetRepository,
        IRepository<PracticeQuestion, Guid> practiceQuestionRepository,
        IRepository<PremiumPlan, Guid> premiumPlanRepository,
        IRepository<UserPremiumSubscription, Guid> userPremiumSubscriptionRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<LearningSession, Guid> learningSessionRepository,
        IRepository<LearningSessionQuestion, Guid> learningSessionQuestionRepository,
        IRepository<LearningSessionQuestionOption, Guid> learningSessionQuestionOptionRepository,
        IRepository<LearningSessionQuestionMatchingPair, Guid> learningSessionQuestionMatchingPairRepository,
        IRepository<LearningSessionQuestionEssayAnswer, Guid> learningSessionQuestionEssayAnswerRepository,
        IRepository<LearningSessionAnswer, Guid> learningSessionAnswerRepository,
        IdentityUserManager identityUserManager,
        QuestionTypeDataSeedContributor questionTypeDataSeedContributor,
        IGuidGenerator guidGenerator,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _subjectRepository = subjectRepository;
        _questionTypeRepository = questionTypeRepository;
        _questionRepository = questionRepository;
        _questionOptionRepository = questionOptionRepository;
        _questionMatchingPairRepository = questionMatchingPairRepository;
        _questionEssayAnswerRepository = questionEssayAnswerRepository;
        _examRepository = examRepository;
        _examQuestionRepository = examQuestionRepository;
        _practiceSetRepository = practiceSetRepository;
        _practiceQuestionRepository = practiceQuestionRepository;
        _premiumPlanRepository = premiumPlanRepository;
        _userPremiumSubscriptionRepository = userPremiumSubscriptionRepository;
        _identityUserRepository = identityUserRepository;
        _learningSessionRepository = learningSessionRepository;
        _learningSessionQuestionRepository = learningSessionQuestionRepository;
        _learningSessionQuestionOptionRepository = learningSessionQuestionOptionRepository;
        _learningSessionQuestionMatchingPairRepository = learningSessionQuestionMatchingPairRepository;
        _learningSessionQuestionEssayAnswerRepository = learningSessionQuestionEssayAnswerRepository;
        _learningSessionAnswerRepository = learningSessionAnswerRepository;
        _identityUserManager = identityUserManager;
        _questionTypeDataSeedContributor = questionTypeDataSeedContributor;
        _guidGenerator = guidGenerator;
        _unitOfWorkManager = unitOfWorkManager;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        await _unitOfWorkManager.Current!.SaveChangesAsync();
        await _questionTypeDataSeedContributor.SeedAsync(context);
        await _unitOfWorkManager.Current.SaveChangesAsync();

        await SeedSubjectsAsync();
        await SeedQuestionCoverageAsync();
        await _unitOfWorkManager.Current!.SaveChangesAsync();

        await SeedExamsAsync();
        await SeedPracticeSetsAsync();
        await _unitOfWorkManager.Current.SaveChangesAsync();

        var mockUsers = await SeedMockUsersAsync();
        await SeedPremiumSubscriptionsAsync(mockUsers);
        await _unitOfWorkManager.Current.SaveChangesAsync();

        await SeedLearningSessionHistoryAsync(mockUsers);
    }

    private async Task SeedSubjectsAsync()
    {
        await CreateSubjectIfNotExistsAsync("mock_general", "Môn tổng hợp", 10, "Danh mục môn thi mẫu để test CRUD admin.");
        await CreateSubjectIfNotExistsAsync("mock_bidding", "Đấu thầu", 20, "Môn thi mẫu cho nội dung đấu thầu và hợp đồng.");
        await CreateSubjectIfNotExistsAsync("mock_contracts", "Hợp đồng", 30, "Môn thi mẫu cho các tình huống hợp đồng và pháp lý.");
    }

    private async Task SeedQuestionCoverageAsync()
    {
        await CreateMatchingQuestionIfNotExistsAsync(
            "[MOCK-MATCH-01] Ghép khái niệm pháp lý",
            "Nối từng thuật ngữ với mô tả phù hợp nhất.",
            "Đối chiếu đúng cặp khái niệm và mô tả.",
            10010,
            new[]
            {
                ("Bảo lãnh dự thầu", "Cam kết tài chính bảo đảm trách nhiệm tham dự"),
                ("Hợp đồng trọn gói", "Giá hợp đồng cố định cho toàn bộ khối lượng"),
                ("Hồ sơ mời thầu", "Tài liệu phát hành để nhà thầu chuẩn bị dự thầu")
            });

        await CreateMatchingQuestionIfNotExistsAsync(
            "[MOCK-MATCH-02] Ghép giai đoạn quy trình",
            "Nối từng giai đoạn với đầu việc tương ứng trong quy trình đấu thầu.",
            "Ghép đúng trình tự nghiệp vụ.",
            10020,
            new[]
            {
                ("Lập kế hoạch", "Xác định nhu cầu, phạm vi và dự toán"),
                ("Đánh giá hồ sơ", "Thẩm định năng lực và đề xuất kỹ thuật"),
                ("Ký hợp đồng", "Chốt điều khoản và hiệu lực thực hiện")
            });

        await CreateMatchingQuestionIfNotExistsAsync(
            "[MOCK-MATCH-03] Ghép loại hợp đồng",
            "Nối loại hợp đồng với đặc điểm nổi bật.",
            "Mỗi loại hợp đồng có đặc trưng quản trị khác nhau.",
            10030,
            new[]
            {
                ("Theo đơn giá cố định", "Thanh toán theo khối lượng thực tế với đơn giá không đổi"),
                ("Theo thời gian", "Chi phí phụ thuộc thời lượng chuyên gia hoặc nhân sự"),
                ("Theo tỷ lệ", "Chi phí tính trên phần trăm giá trị công việc")
            });

        await CreateMatchingQuestionIfNotExistsAsync(
            "[MOCK-MATCH-04] Ghép vai trò hệ thống",
            "Nối vai trò với phạm vi thao tác trong hệ thống elearning.",
            "Phân biệt đúng phạm vi giữa admin và user.",
            10040,
            new[]
            {
                ("Admin", "Quản trị nội dung, tài khoản và cấu hình"),
                ("Premium user", "Được mở nội dung premium đã publish"),
                ("Free user", "Chỉ truy cập nội dung free")
            });

        await CreateEssayQuestionIfNotExistsAsync(
            "[MOCK-ESSAY-01] Phân tích điều kiện dự thầu",
            "Trình bày các yếu tố cần kiểm tra trước khi quyết định tham gia một gói thầu.",
            "Cần nêu phạm vi, năng lực, pháp lý, tài chính và rủi ro.",
            10110,
            250);

        await CreateEssayQuestionIfNotExistsAsync(
            "[MOCK-ESSAY-02] So sánh đề thi và ôn tập",
            "Giải thích sự khác nhau giữa chế độ đề thi và chế độ ôn tập trong hệ thống.",
            "Nên đề cập giới hạn thời gian, lời giải và mục tiêu sử dụng.",
            10120,
            200);

        await CreateEssayQuestionIfNotExistsAsync(
            "[MOCK-ESSAY-03] Xử lý phản hồi người dùng",
            "Nếu user báo nhập Excel bị lỗi định dạng, admin nên kiểm tra và phản hồi theo trình tự nào?",
            "Ưu tiên mô tả bước kiểm tra template, validate cột và log import.",
            10130,
            220);

        await CreateEssayQuestionIfNotExistsAsync(
            "[MOCK-ESSAY-04] Định hướng mở rộng hệ thống",
            "Đề xuất cách mở rộng hệ thống khi số lượng đề thi và người dùng tăng nhanh.",
            "Nên đề cập dữ liệu, phân quyền, hiệu năng và vận hành.",
            10140,
            300);
    }

    private async Task SeedExamsAsync()
    {
        var publishedQuestions = await GetPublishedQuestionsAsync();
        var objectiveQuestions = publishedQuestions
            .Where(x => x.QuestionTypeCode is QuestionTypeCodes.SingleChoice or QuestionTypeCodes.MultipleChoice)
            .OrderBy(x => x.SortOrder)
            .ToList();
        var matchingQuestions = publishedQuestions
            .Where(x => x.QuestionTypeCode == QuestionTypeCodes.Matching)
            .OrderBy(x => x.SortOrder)
            .ToList();
        var essayQuestions = publishedQuestions
            .Where(x => x.QuestionTypeCode == QuestionTypeCodes.Essay)
            .OrderBy(x => x.SortOrder)
            .ToList();

        await CreateExamIfNotExistsAsync(
            code: "mock_exam_free_fixed_20",
            title: "[MOCK] Đề thi Free cố định 20 câu",
            description: "Bộ đề thi mẫu để test luồng client/admin với 20 câu objective cố định.",
            accessLevel: ExamAccessLevel.Free,
            selectionMode: ExamSelectionMode.Fixed,
            durationMinutes: 30,
            totalQuestionCount: 20,
            sortOrder: 910,
            questionIds: objectiveQuestions.Take(20).Select(x => x.Id).ToList());

        await CreateExamIfNotExistsAsync(
            code: "mock_exam_random_30",
            title: "[MOCK] Đề thi Random 30 câu",
            description: "Bộ đề mẫu random để test pool câu hỏi lớn hơn tổng số câu cần dùng.",
            accessLevel: ExamAccessLevel.Free,
            selectionMode: ExamSelectionMode.Random,
            durationMinutes: 45,
            totalQuestionCount: 30,
            sortOrder: 920,
            questionIds: objectiveQuestions.Take(40).Select(x => x.Id).ToList());

        await CreateExamIfNotExistsAsync(
            code: "mock_exam_premium_mixed_12",
            title: "[MOCK] Đề thi Premium hỗn hợp 12 câu",
            description: "Bộ đề mẫu có objective, matching và essay để test full runtime.",
            accessLevel: ExamAccessLevel.Premium,
            selectionMode: ExamSelectionMode.Fixed,
            durationMinutes: 40,
            totalQuestionCount: 12,
            sortOrder: 930,
            questionIds: objectiveQuestions.Take(8).Select(x => x.Id)
                .Concat(matchingQuestions.Take(2).Select(x => x.Id))
                .Concat(essayQuestions.Take(2).Select(x => x.Id))
                .ToList());
    }

    private async Task SeedPracticeSetsAsync()
    {
        var publishedQuestions = await GetPublishedQuestionsAsync();
        var objectiveQuestions = publishedQuestions
            .Where(x => x.QuestionTypeCode is QuestionTypeCodes.SingleChoice or QuestionTypeCodes.MultipleChoice)
            .OrderBy(x => x.SortOrder)
            .ToList();
        var matchingQuestions = publishedQuestions
            .Where(x => x.QuestionTypeCode == QuestionTypeCodes.Matching)
            .OrderBy(x => x.SortOrder)
            .ToList();
        var essayQuestions = publishedQuestions
            .Where(x => x.QuestionTypeCode == QuestionTypeCodes.Essay)
            .OrderBy(x => x.SortOrder)
            .ToList();

        await CreatePracticeIfNotExistsAsync(
            code: "mock_practice_free_review_20",
            title: "[MOCK] Ôn tập Free 20 câu",
            description: "Bộ ôn tập mẫu có lời giải để test trang client.",
            accessLevel: PracticeAccessLevel.Free,
            selectionMode: PracticeSelectionMode.Fixed,
            totalQuestionCount: 20,
            sortOrder: 910,
            showExplanation: true,
            questionIds: objectiveQuestions.Skip(20).Take(20).Select(x => x.Id).ToList());

        await CreatePracticeIfNotExistsAsync(
            code: "mock_practice_random_25",
            title: "[MOCK] Ôn tập Random 25 câu",
            description: "Bộ ôn tập random dùng để test số lượng câu lớn hơn tổng cần làm.",
            accessLevel: PracticeAccessLevel.Free,
            selectionMode: PracticeSelectionMode.Random,
            totalQuestionCount: 25,
            sortOrder: 920,
            showExplanation: true,
            questionIds: objectiveQuestions.Take(35).Select(x => x.Id).ToList());

        await CreatePracticeIfNotExistsAsync(
            code: "mock_practice_premium_mixed_10",
            title: "[MOCK] Ôn tập Premium hỗn hợp 10 câu",
            description: "Bộ ôn tập mẫu có matching và essay để test review sau submit.",
            accessLevel: PracticeAccessLevel.Premium,
            selectionMode: PracticeSelectionMode.Fixed,
            totalQuestionCount: 10,
            sortOrder: 930,
            showExplanation: true,
            questionIds: objectiveQuestions.Take(6).Select(x => x.Id)
                .Concat(matchingQuestions.Take(2).Select(x => x.Id))
                .Concat(essayQuestions.Take(2).Select(x => x.Id))
                .ToList());
    }

    private async Task<MockRuntimeUsers> SeedMockUsersAsync()
    {
        var freeUser = await EnsureMockUserAsync("mock.free", "mock.free@elearning.local");
        var premiumUser = await EnsureMockUserAsync("mock.premium", "mock.premium@elearning.local");
        var expiredUser = await EnsureMockUserAsync("mock.expired", "mock.expired@elearning.local");

        return new MockRuntimeUsers(freeUser, premiumUser, expiredUser);
    }

    private async Task SeedPremiumSubscriptionsAsync(MockRuntimeUsers users)
    {
        var premiumPlan = await _premiumPlanRepository.GetAsync(x => x.Code == PremiumPlanConsts.SixMonthsCode);

        await CreateSubscriptionIfNotExistsAsync(
            users.PremiumUser.Id,
            premiumPlan.Id,
            activationNumber: 1,
            activatedTime: CreateUtc(2025, 5, 10, 1, 0),
            note: "[MOCK] Premium hết hạn lần 1",
            markExpired: true);

        await CreateSubscriptionIfNotExistsAsync(
            users.PremiumUser.Id,
            premiumPlan.Id,
            activationNumber: 2,
            activatedTime: CreateUtc(2026, 2, 15, 1, 0),
            note: "[MOCK] Premium đang hoạt động lần 2",
            markExpired: false);

        await CreateSubscriptionIfNotExistsAsync(
            users.ExpiredUser.Id,
            premiumPlan.Id,
            activationNumber: 1,
            activatedTime: CreateUtc(2025, 7, 1, 1, 0),
            note: "[MOCK] Premium đã hết hạn",
            markExpired: true);
    }

    private async Task SeedLearningSessionHistoryAsync(MockRuntimeUsers users)
    {
        var freeExam = await FindExamHistorySourceAsync("mock_exam_free_fixed_20");
        var premiumExam = await FindExamHistorySourceAsync("mock_exam_premium_mixed_12");
        var randomExam = await FindExamHistorySourceAsync("mock_exam_random_30");
        var freePractice = await FindPracticeHistorySourceAsync("mock_practice_free_review_20");
        var premiumPractice = await FindPracticeHistorySourceAsync("mock_practice_premium_mixed_10");

        if (freeExam != null)
        {
            await CreateHistorySessionIfNotExistsAsync(
                users.FreeUser.Id,
                freeExam,
                CreateUtc(2026, 4, 12, 1, 0),
                MockSessionBehavior.CreateSubmitted(objectiveCorrectCount: 6, objectiveWrongCount: 6));
        }

        if (freePractice != null)
        {
            await CreateHistorySessionIfNotExistsAsync(
                users.FreeUser.Id,
                freePractice,
                CreateUtc(2026, 4, 13, 3, 0),
                MockSessionBehavior.CreateAbandoned(objectiveCorrectCount: 2, objectiveWrongCount: 1));
        }

        if (premiumExam != null)
        {
            await CreateHistorySessionIfNotExistsAsync(
                users.PremiumUser.Id,
                premiumExam,
                CreateUtc(2026, 4, 15, 2, 0),
                MockSessionBehavior.CreateSubmitted(objectiveCorrectCount: 6, objectiveWrongCount: 2, answerMatchingCorrectly: true, answerEssay: true));
        }

        if (premiumPractice != null)
        {
            await CreateHistorySessionIfNotExistsAsync(
                users.PremiumUser.Id,
                premiumPractice,
                CreateUtc(2026, 4, 16, 2, 30),
                MockSessionBehavior.CreateSubmitted(objectiveCorrectCount: 4, objectiveWrongCount: 2, answerMatchingCorrectly: true, answerEssay: true));

            await CreateHistorySessionIfNotExistsAsync(
                users.ExpiredUser.Id,
                premiumPractice,
                CreateUtc(2025, 8, 18, 1, 30),
                MockSessionBehavior.CreateSubmitted(objectiveCorrectCount: 3, objectiveWrongCount: 3, answerMatchingCorrectly: false, answerEssay: true));
        }

        if (randomExam != null)
        {
            await CreateHistorySessionIfNotExistsAsync(
                users.ExpiredUser.Id,
                randomExam,
                CreateUtc(2026, 3, 20, 1, 0),
                MockSessionBehavior.CreateSubmitted(objectiveCorrectCount: 10, objectiveWrongCount: 10));
        }
    }

    private async Task CreateSubjectIfNotExistsAsync(string code, string name, int sortOrder, string description)
    {
        if (await _subjectRepository.FindAsync(x => x.Code == code) != null)
        {
            return;
        }

        await _subjectRepository.InsertAsync(new Subject(
            _guidGenerator.Create(),
            code,
            name,
            sortOrder,
            description));
    }

    private async Task CreateMatchingQuestionIfNotExistsAsync(
        string title,
        string content,
        string explanation,
        int sortOrder,
        IEnumerable<(string LeftText, string RightText)> pairs)
    {
        if (await _questionRepository.FindAsync(x => x.Title == title) != null)
        {
            return;
        }

        var question = new Question(
            _guidGenerator.Create(),
            await GetQuestionTypeIdAsync(QuestionTypeCodes.Matching),
            title,
            content,
            QuestionDifficulty.Medium,
            1,
            sortOrder,
            explanation);

        question.Publish();
        await _questionRepository.InsertAsync(question);

        var index = 1;
        foreach (var pair in pairs)
        {
            await _questionMatchingPairRepository.InsertAsync(new QuestionMatchingPair(
                _guidGenerator.Create(),
                question.Id,
                pair.LeftText,
                pair.RightText,
                index++));
        }
    }

    private async Task CreateEssayQuestionIfNotExistsAsync(
        string title,
        string content,
        string explanation,
        int sortOrder,
        int maxWords)
    {
        if (await _questionRepository.FindAsync(x => x.Title == title) != null)
        {
            return;
        }

        var question = new Question(
            _guidGenerator.Create(),
            await GetQuestionTypeIdAsync(QuestionTypeCodes.Essay),
            title,
            content,
            QuestionDifficulty.Medium,
            1,
            sortOrder,
            explanation);

        question.Publish();
        await _questionRepository.InsertAsync(question);

        await _questionEssayAnswerRepository.InsertAsync(new QuestionEssayAnswer(
            _guidGenerator.Create(),
            question.Id,
            "Bài làm nên có mở đầu, luận điểm chính, ví dụ minh họa và kết luận rõ ràng.",
            "Chấm theo độ đầy đủ ý, logic trình bày và tính thực tiễn của câu trả lời.",
            maxWords));
    }

    private async Task CreateExamIfNotExistsAsync(
        string code,
        string title,
        string description,
        ExamAccessLevel accessLevel,
        ExamSelectionMode selectionMode,
        int durationMinutes,
        int totalQuestionCount,
        int sortOrder,
        List<Guid> questionIds)
    {
        if (await _examRepository.FindAsync(x => x.Code == code) != null || questionIds.Count < totalQuestionCount)
        {
            return;
        }

        var exam = new Exam(
            _guidGenerator.Create(),
            code,
            title,
            accessLevel,
            selectionMode,
            durationMinutes,
            totalQuestionCount,
            sortOrder,
            description,
            passingScore: 0,
            shuffleQuestions: selectionMode == ExamSelectionMode.Random,
            shuffleOptions: true);

        exam.Publish(DateTime.UtcNow);
        await _examRepository.InsertAsync(exam);

        for (var i = 0; i < questionIds.Count; i++)
        {
            await _examQuestionRepository.InsertAsync(new ExamQuestion(
                _guidGenerator.Create(),
                exam.Id,
                questionIds[i],
                i + 1));
        }
    }

    private async Task CreatePracticeIfNotExistsAsync(
        string code,
        string title,
        string description,
        PracticeAccessLevel accessLevel,
        PracticeSelectionMode selectionMode,
        int totalQuestionCount,
        int sortOrder,
        bool showExplanation,
        List<Guid> questionIds)
    {
        if (await _practiceSetRepository.FindAsync(x => x.Code == code) != null || questionIds.Count < totalQuestionCount)
        {
            return;
        }

        var practiceSet = new PracticeSet(
            _guidGenerator.Create(),
            code,
            title,
            accessLevel,
            selectionMode,
            totalQuestionCount,
            sortOrder,
            description,
            shuffleQuestions: selectionMode == PracticeSelectionMode.Random,
            showExplanation: showExplanation);

        practiceSet.Publish(DateTime.UtcNow);
        await _practiceSetRepository.InsertAsync(practiceSet);

        for (var i = 0; i < questionIds.Count; i++)
        {
            await _practiceQuestionRepository.InsertAsync(new PracticeQuestion(
                _guidGenerator.Create(),
                practiceSet.Id,
                questionIds[i],
                i + 1));
        }
    }

    private async Task<IdentityUser> EnsureMockUserAsync(string userName, string email)
    {
        var existingUser = await _identityUserRepository.FindAsync(x => x.UserName == userName);
        if (existingUser != null)
        {
            await ResetMockUserPasswordAsync(existingUser);
            existingUser.SetEmailConfirmed(true);
            await _identityUserRepository.UpdateAsync(existingUser, autoSave: true);
            return existingUser;
        }

        var user = new IdentityUser(_guidGenerator.Create(), userName, email, null);
        user.SetEmailConfirmed(true);

        EnsureIdentitySucceeded(await _identityUserManager.CreateAsync(user, MockUserPassword), $"create user '{userName}'");
        return user;
    }

    private async Task ResetMockUserPasswordAsync(IdentityUser user)
    {
        if (await _identityUserManager.HasPasswordAsync(user))
        {
            EnsureIdentitySucceeded(await _identityUserManager.RemovePasswordAsync(user), $"remove password for '{user.UserName}'");
        }

        EnsureIdentitySucceeded(await _identityUserManager.AddPasswordAsync(user, MockUserPassword), $"set password for '{user.UserName}'");
    }

    private async Task CreateSubscriptionIfNotExistsAsync(
        Guid userId,
        Guid premiumPlanId,
        int activationNumber,
        DateTime activatedTime,
        string note,
        bool markExpired)
    {
        if (await _userPremiumSubscriptionRepository.FindAsync(x => x.UserId == userId && x.ActivationNumber == activationNumber) != null)
        {
            return;
        }

        var subscription = new UserPremiumSubscription(
            _guidGenerator.Create(),
            userId,
            premiumPlanId,
            activationNumber,
            activatedTime,
            PremiumPlanConsts.SixMonthsDuration,
            note);

        if (markExpired)
        {
            subscription.MarkExpired();
        }

        await _userPremiumSubscriptionRepository.InsertAsync(subscription);
    }

    private async Task CreateHistorySessionIfNotExistsAsync(
        Guid userId,
        PreparedHistorySource source,
        DateTime startedAt,
        MockSessionBehavior behavior)
    {
        if (await _learningSessionRepository.FindAsync(x =>
                x.UserId == userId &&
                x.SourceKind == source.SourceKind &&
                x.SourceCode == source.SourceCode &&
                x.StartedAt == startedAt) != null)
        {
            return;
        }

        var session = new LearningSession(
            _guidGenerator.Create(),
            userId,
            source.SourceKind,
            source.SourceId,
            source.SourceCode,
            source.Title,
            source.Description,
            source.IsPremiumContent,
            source.ShowExplanation,
            source.DurationMinutes,
            source.TotalQuestionCount,
            startedAt);

        await _learningSessionRepository.InsertAsync(session);

        var objectiveIndex = 0;
        var answeredCount = 0;
        var correctCount = 0;
        decimal score = 0;

        foreach (var sourceQuestion in source.Questions)
        {
            var sessionQuestion = new LearningSessionQuestion(
                _guidGenerator.Create(),
                session.Id,
                sourceQuestion.OriginalQuestionId,
                sourceQuestion.QuestionTypeId,
                sourceQuestion.QuestionTypeCode,
                sourceQuestion.QuestionTypeName,
                sourceQuestion.Title,
                sourceQuestion.Content,
                sourceQuestion.Explanation,
                sourceQuestion.SortOrder,
                sourceQuestion.Score);

            await _learningSessionQuestionRepository.InsertAsync(sessionQuestion);

            var sessionOptions = new List<LearningSessionQuestionOption>();
            foreach (var sourceOption in sourceQuestion.Options)
            {
                var sessionOption = new LearningSessionQuestionOption(
                    _guidGenerator.Create(),
                    sessionQuestion.Id,
                    sourceOption.OriginalOptionId,
                    sourceOption.Text,
                    sourceOption.IsCorrect,
                    sourceOption.SortOrder);

                sessionOptions.Add(sessionOption);
                await _learningSessionQuestionOptionRepository.InsertAsync(sessionOption);
            }

            var sessionMatchingPairs = new List<LearningSessionQuestionMatchingPair>();
            foreach (var sourceMatchingPair in sourceQuestion.MatchingPairs)
            {
                var sessionMatchingPair = new LearningSessionQuestionMatchingPair(
                    _guidGenerator.Create(),
                    sessionQuestion.Id,
                    sourceMatchingPair.OriginalMatchingPairId,
                    sourceMatchingPair.LeftText,
                    sourceMatchingPair.RightText,
                    sourceMatchingPair.SortOrder);

                sessionMatchingPairs.Add(sessionMatchingPair);
                await _learningSessionQuestionMatchingPairRepository.InsertAsync(sessionMatchingPair);
            }

            if (sourceQuestion.EssayAnswer != null)
            {
                await _learningSessionQuestionEssayAnswerRepository.InsertAsync(new LearningSessionQuestionEssayAnswer(
                    _guidGenerator.Create(),
                    sessionQuestion.Id,
                    sourceQuestion.EssayAnswer.SampleAnswer,
                    sourceQuestion.EssayAnswer.Rubric,
                    sourceQuestion.EssayAnswer.MaxWords));
            }

            var answer = new LearningSessionAnswer(
                _guidGenerator.Create(),
                session.Id,
                sessionQuestion.Id);

            var answeredAt = startedAt.AddMinutes(Math.Max(1, sessionQuestion.SortOrder));

            if (sourceQuestion.QuestionTypeCode == QuestionTypeCodes.Matching)
            {
                if (behavior.AnswerMatchingCorrectly.HasValue)
                {
                    var matchingAnswers = behavior.AnswerMatchingCorrectly.Value
                        ? sessionMatchingPairs.Select(x => new MockMatchingAnswer
                        {
                            PairId = x.Id,
                            SelectedRightText = x.RightText
                        }).ToList()
                        : BuildIncorrectMatchingAnswers(sessionMatchingPairs);

                    answer.SaveMatching(JsonSerializer.Serialize(matchingAnswers), true, behavior.AnswerMatchingCorrectly.Value, answeredAt);
                }
            }
            else if (sourceQuestion.QuestionTypeCode == QuestionTypeCodes.Essay)
            {
                if (behavior.AnswerEssay)
                {
                    answer.SaveEssay($"[MOCK] Bài làm cho {sourceQuestion.Title}", true, answeredAt);
                }
            }
            else
            {
                objectiveIndex++;
                if (objectiveIndex <= behavior.ObjectiveCorrectCount)
                {
                    var correctOptionIds = sessionOptions.Where(x => x.IsCorrect).Select(x => x.Id).ToList();
                    answer.SaveSelection(JsonSerializer.Serialize(correctOptionIds), true, true, answeredAt);
                }
                else if (objectiveIndex <= behavior.ObjectiveCorrectCount + behavior.ObjectiveWrongCount)
                {
                    var incorrectOptionIds = BuildIncorrectObjectiveAnswer(sourceQuestion.QuestionTypeCode, sessionOptions);
                    answer.SaveSelection(JsonSerializer.Serialize(incorrectOptionIds), true, false, answeredAt);
                }
            }

            if (answer.IsAnswered)
            {
                answeredCount++;
            }

            if (answer.IsCorrect)
            {
                correctCount++;
                score += sessionQuestion.Score;
            }

            await _learningSessionAnswerRepository.InsertAsync(answer);
        }

        if (behavior.Status == LearningSessionStatus.Submitted)
        {
            var submittedAt = startedAt.AddMinutes(source.DurationMinutes.HasValue
                ? Math.Max(5, Math.Min(source.DurationMinutes.Value - 1, source.TotalQuestionCount + 8))
                : Math.Max(5, source.TotalQuestionCount + 3));
            session.Submit(submittedAt, score, correctCount, answeredCount);
        }
        else if (behavior.Status == LearningSessionStatus.Abandoned)
        {
            session.Abandon();
        }

        await _learningSessionRepository.UpdateAsync(session);
    }

    private async Task<Guid> GetQuestionTypeIdAsync(string code)
    {
        var questionType = await _questionTypeRepository.FindAsync(x => x.Code == code);
        return questionType?.Id ?? throw new InvalidOperationException($"Question type '{code}' was not found.");
    }

    private async Task<List<PublishedQuestionItem>> GetPublishedQuestionsAsync()
    {
        var questionTypes = await _questionTypeRepository.GetListAsync();
        var publishedQuestions = (await _questionRepository.GetListAsync())
            .Where(x => x.Status == QuestionStatus.Published && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToList();

        return publishedQuestions
            .Join(
                questionTypes,
                question => question.QuestionTypeId,
                questionType => questionType.Id,
                (question, questionType) => new PublishedQuestionItem(question.Id, questionType.Code, question.SortOrder))
            .ToList();
    }

    private async Task<PreparedHistorySource?> FindExamHistorySourceAsync(string code)
    {
        var exam = await _examRepository.FindAsync(x => x.Code == code);
        if (exam == null)
        {
            return null;
        }

        var assignments = (await _examQuestionRepository.GetListAsync())
            .Where(x => x.ExamId == exam.Id)
            .OrderBy(x => x.SortOrder)
            .Take(exam.TotalQuestionCount)
            .Select(x => new HistoryQuestionAssignment(x.QuestionId, x.SortOrder, x.ScoreOverride))
            .ToList();

        return new PreparedHistorySource(
            LearningSessionSourceKind.Exam,
            exam.Id,
            exam.Code,
            exam.Title,
            exam.Description,
            exam.AccessLevel == ExamAccessLevel.Premium,
            false,
            exam.DurationMinutes,
            exam.TotalQuestionCount,
            await LoadHistoryQuestionsAsync(assignments));
    }

    private async Task<PreparedHistorySource?> FindPracticeHistorySourceAsync(string code)
    {
        var practiceSet = await _practiceSetRepository.FindAsync(x => x.Code == code);
        if (practiceSet == null)
        {
            return null;
        }

        var assignments = (await _practiceQuestionRepository.GetListAsync())
            .Where(x => x.PracticeSetId == practiceSet.Id)
            .OrderBy(x => x.SortOrder)
            .Take(practiceSet.TotalQuestionCount)
            .Select(x => new HistoryQuestionAssignment(x.QuestionId, x.SortOrder, null))
            .ToList();

        return new PreparedHistorySource(
            LearningSessionSourceKind.Practice,
            practiceSet.Id,
            practiceSet.Code,
            practiceSet.Title,
            practiceSet.Description,
            practiceSet.AccessLevel == PracticeAccessLevel.Premium,
            practiceSet.ShowExplanation,
            null,
            practiceSet.TotalQuestionCount,
            await LoadHistoryQuestionsAsync(assignments));
    }

    private async Task<List<PreparedHistoryQuestion>> LoadHistoryQuestionsAsync(List<HistoryQuestionAssignment> assignments)
    {
        var questionIds = assignments.Select(x => x.QuestionId).Distinct().ToList();
        var questions = (await _questionRepository.GetListAsync())
            .Where(x => questionIds.Contains(x.Id))
            .ToDictionary(x => x.Id);
        var questionTypes = (await _questionTypeRepository.GetListAsync()).ToDictionary(x => x.Id);
        var optionsByQuestionId = (await _questionOptionRepository.GetListAsync())
            .Where(x => questionIds.Contains(x.QuestionId))
            .GroupBy(x => x.QuestionId)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.SortOrder).ToList());
        var matchingPairsByQuestionId = (await _questionMatchingPairRepository.GetListAsync())
            .Where(x => questionIds.Contains(x.QuestionId))
            .GroupBy(x => x.QuestionId)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.SortOrder).ToList());
        var essayAnswersByQuestionId = (await _questionEssayAnswerRepository.GetListAsync())
            .Where(x => questionIds.Contains(x.QuestionId))
            .GroupBy(x => x.QuestionId)
            .ToDictionary(x => x.Key, x => x.First());

        return assignments.Select(assignment =>
        {
            var question = questions[assignment.QuestionId];
            var questionType = questionTypes[question.QuestionTypeId];

            return new PreparedHistoryQuestion(
                question.Id,
                questionType.Id,
                questionType.Code,
                questionType.DisplayName,
                question.Title,
                question.Content,
                question.Explanation,
                assignment.SortOrder,
                assignment.ScoreOverride ?? question.Score,
                optionsByQuestionId.GetValueOrDefault(question.Id, new List<QuestionOption>())
                    .Select(x => new PreparedHistoryOption(x.Id, x.Text, x.IsCorrect, x.SortOrder))
                    .ToList(),
                essayAnswersByQuestionId.TryGetValue(question.Id, out var essayAnswer)
                    ? new PreparedHistoryEssayAnswer(essayAnswer.SampleAnswer, essayAnswer.Rubric, essayAnswer.MaxWords)
                    : null,
                matchingPairsByQuestionId.GetValueOrDefault(question.Id, new List<QuestionMatchingPair>())
                    .Select(x => new PreparedHistoryMatchingPair(x.Id, x.LeftText, x.RightText, x.SortOrder))
                    .ToList());
        }).ToList();
    }

    private static List<Guid> BuildIncorrectObjectiveAnswer(string questionTypeCode, List<LearningSessionQuestionOption> options)
    {
        var correctOptionIds = options.Where(x => x.IsCorrect).OrderBy(x => x.SortOrder).Select(x => x.Id).ToList();
        var wrongOptionIds = options.Where(x => !x.IsCorrect).OrderBy(x => x.SortOrder).Select(x => x.Id).ToList();

        if (questionTypeCode == QuestionTypeCodes.MultipleChoice)
        {
            if (wrongOptionIds.Count > 0)
            {
                return correctOptionIds.Take(Math.Max(0, correctOptionIds.Count - 1))
                    .Concat(new[] { wrongOptionIds[0] })
                    .Distinct()
                    .ToList();
            }

            return correctOptionIds.Take(Math.Max(1, correctOptionIds.Count - 1)).ToList();
        }

        if (wrongOptionIds.Count > 0)
        {
            return new List<Guid> { wrongOptionIds[0] };
        }

        return correctOptionIds.Take(1).ToList();
    }

    private static List<MockMatchingAnswer> BuildIncorrectMatchingAnswers(List<LearningSessionQuestionMatchingPair> pairs)
    {
        if (pairs.Count <= 1)
        {
            return pairs.Select(x => new MockMatchingAnswer
            {
                PairId = x.Id,
                SelectedRightText = null
            }).ToList();
        }

        var shiftedRightTexts = pairs.Skip(1).Select(x => x.RightText)
            .Concat(new[] { pairs[0].RightText })
            .ToList();

        return pairs.Select((pair, index) => new MockMatchingAnswer
        {
            PairId = pair.Id,
            SelectedRightText = shiftedRightTexts[index]
        }).ToList();
    }

    private static void EnsureIdentitySucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new InvalidOperationException($"Unable to {operation}: {string.Join("; ", result.Errors.Select(x => x.Description))}");
    }

    private static DateTime CreateUtc(int year, int month, int day, int hour, int minute)
    {
        return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
    }

    private sealed record PublishedQuestionItem(Guid Id, string QuestionTypeCode, int SortOrder);

    private sealed record MockRuntimeUsers(IdentityUser FreeUser, IdentityUser PremiumUser, IdentityUser ExpiredUser);

    private sealed record PreparedHistorySource(
        LearningSessionSourceKind SourceKind,
        Guid SourceId,
        string SourceCode,
        string Title,
        string? Description,
        bool IsPremiumContent,
        bool ShowExplanation,
        int? DurationMinutes,
        int TotalQuestionCount,
        List<PreparedHistoryQuestion> Questions);

    private sealed record HistoryQuestionAssignment(Guid QuestionId, int SortOrder, decimal? ScoreOverride);

    private sealed record PreparedHistoryQuestion(
        Guid OriginalQuestionId,
        Guid QuestionTypeId,
        string QuestionTypeCode,
        string QuestionTypeName,
        string Title,
        string Content,
        string? Explanation,
        int SortOrder,
        decimal Score,
        List<PreparedHistoryOption> Options,
        PreparedHistoryEssayAnswer? EssayAnswer,
        List<PreparedHistoryMatchingPair> MatchingPairs);

    private sealed record PreparedHistoryOption(Guid OriginalOptionId, string Text, bool IsCorrect, int SortOrder);

    private sealed record PreparedHistoryEssayAnswer(string? SampleAnswer, string? Rubric, int? MaxWords);

    private sealed record PreparedHistoryMatchingPair(Guid OriginalMatchingPairId, string LeftText, string RightText, int SortOrder);

    private sealed record MockMatchingAnswer
    {
        public Guid PairId { get; init; }

        public string? SelectedRightText { get; init; }
    }

    private sealed record MockSessionBehavior(
        LearningSessionStatus Status,
        int ObjectiveCorrectCount,
        int ObjectiveWrongCount,
        bool? AnswerMatchingCorrectly,
        bool AnswerEssay)
    {
        public static MockSessionBehavior CreateSubmitted(
            int objectiveCorrectCount,
            int objectiveWrongCount,
            bool? answerMatchingCorrectly = null,
            bool answerEssay = false)
        {
            return new MockSessionBehavior(
                LearningSessionStatus.Submitted,
                objectiveCorrectCount,
                objectiveWrongCount,
                answerMatchingCorrectly,
                answerEssay);
        }

        public static MockSessionBehavior CreateAbandoned(int objectiveCorrectCount, int objectiveWrongCount)
        {
            return new MockSessionBehavior(
                LearningSessionStatus.Abandoned,
                objectiveCorrectCount,
                objectiveWrongCount,
                null,
                false);
        }
    }
}

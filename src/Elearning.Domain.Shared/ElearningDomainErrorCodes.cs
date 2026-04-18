namespace Elearning;

public static class ElearningDomainErrorCodes
{
    /* You can add your business exception error codes here, as constants */
    public const string QuestionTypeCodeAlreadyExists = "Elearning:QuestionType:0001";
    public const string InvalidQuestionTypeCode = "Elearning:QuestionType:0002";
    public const string QuestionTypeCodeCannotBeChanged = "Elearning:QuestionType:0003";
    public const string SystemQuestionTypeCannotBeDeleted = "Elearning:QuestionType:0004";
    public const string InactiveQuestionTypeCannotBeUsed = "Elearning:Question:0001";
    public const string QuestionTypeCannotBeChanged = "Elearning:Question:0002";
    public const string InvalidQuestionAnswers = "Elearning:Question:0003";
    public const string PremiumPlanCodeAlreadyExists = "Elearning:Premium:0001";
    public const string InactivePremiumPlanCannotBeUsed = "Elearning:Premium:0002";
    public const string PremiumSubscriptionDateRangeInvalid = "Elearning:Premium:0003";
    public const string UserAlreadyHasActivePremium = "Elearning:Premium:0004";
    public const string CancelledPremiumSubscriptionCannotBeChanged = "Elearning:Premium:0005";
    public const string PremiumPlanCodeCannotBeChanged = "Elearning:Premium:0006";
    public const string ExamCodeAlreadyExists = "Elearning:Exam:0001";
    public const string ExamCannotPublishWithoutQuestions = "Elearning:Exam:0002";
    public const string ExamQuestionAlreadyExists = "Elearning:Exam:0003";
    public const string InactiveQuestionCannotBeUsedInExam = "Elearning:Exam:0004";
    public const string RandomExamQuestionCountExceedsPool = "Elearning:Exam:0005";
    public const string PracticeSetCodeAlreadyExists = "Elearning:Practice:0001";
    public const string PracticeSetCannotPublishWithoutQuestions = "Elearning:Practice:0002";
    public const string PracticeQuestionAlreadyExists = "Elearning:Practice:0003";
    public const string InactiveQuestionCannotBeUsedInPractice = "Elearning:Practice:0004";
    public const string RandomPracticeQuestionCountExceedsPool = "Elearning:Practice:0005";
}

using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.LearningSessions;

public class LearningSessionAnswer : FullAuditedEntity<Guid>
{
    public Guid LearningSessionId { get; private set; }

    public Guid LearningSessionQuestionId { get; private set; }

    public string? SelectedOptionIdsJson { get; private set; }

    public string? MatchingAnswerJson { get; private set; }

    public string? EssayAnswerText { get; private set; }

    public bool IsAnswered { get; private set; }

    public bool IsCorrect { get; private set; }

    public DateTime? AnsweredAt { get; private set; }

    protected LearningSessionAnswer()
    {
    }

    public LearningSessionAnswer(Guid id, Guid learningSessionId, Guid learningSessionQuestionId)
        : base(id)
    {
        LearningSessionId = learningSessionId;
        LearningSessionQuestionId = learningSessionQuestionId;
        IsAnswered = false;
        IsCorrect = false;
    }

    public void SaveSelection(string? selectedOptionIdsJson, bool isAnswered, bool isCorrect, DateTime answeredAt)
    {
        SelectedOptionIdsJson = Check.Length(selectedOptionIdsJson, nameof(selectedOptionIdsJson), LearningSessionConsts.MaxSelectedOptionIdsJsonLength);
        MatchingAnswerJson = null;
        EssayAnswerText = null;
        IsAnswered = isAnswered;
        IsCorrect = isCorrect;
        AnsweredAt = answeredAt;
    }

    public void SaveMatching(string? matchingAnswerJson, bool isAnswered, bool isCorrect, DateTime answeredAt)
    {
        MatchingAnswerJson = Check.Length(matchingAnswerJson, nameof(matchingAnswerJson), LearningSessionConsts.MaxMatchingAnswerJsonLength);
        SelectedOptionIdsJson = null;
        EssayAnswerText = null;
        IsAnswered = isAnswered;
        IsCorrect = isCorrect;
        AnsweredAt = answeredAt;
    }

    public void SaveEssay(string? essayAnswerText, bool isAnswered, DateTime answeredAt)
    {
        EssayAnswerText = Check.Length(essayAnswerText, nameof(essayAnswerText), LearningSessionConsts.MaxEssayAnswerTextLength);
        SelectedOptionIdsJson = null;
        MatchingAnswerJson = null;
        IsAnswered = isAnswered;
        IsCorrect = false;
        AnsweredAt = answeredAt;
    }
}

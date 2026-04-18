using System;
using Elearning.Questions;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.LearningSessions;

public class LearningSessionQuestionEssayAnswer : FullAuditedEntity<Guid>
{
    public Guid LearningSessionQuestionId { get; private set; }

    public string? SampleAnswer { get; private set; }

    public string? Rubric { get; private set; }

    public int? MaxWords { get; private set; }

    protected LearningSessionQuestionEssayAnswer()
    {
    }

    public LearningSessionQuestionEssayAnswer(Guid id, Guid learningSessionQuestionId, string? sampleAnswer, string? rubric, int? maxWords)
        : base(id)
    {
        LearningSessionQuestionId = learningSessionQuestionId;
        SampleAnswer = Check.Length(sampleAnswer, nameof(sampleAnswer), QuestionConsts.MaxSampleAnswerLength);
        Rubric = Check.Length(rubric, nameof(rubric), QuestionConsts.MaxRubricLength);
        MaxWords = maxWords;
    }
}

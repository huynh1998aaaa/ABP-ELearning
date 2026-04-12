using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.Questions;

public class QuestionEssayAnswer : FullAuditedEntity<Guid>
{
    public Guid QuestionId { get; private set; }

    public string? SampleAnswer { get; private set; }

    public string? Rubric { get; private set; }

    public int? MaxWords { get; private set; }

    protected QuestionEssayAnswer()
    {
    }

    public QuestionEssayAnswer(Guid id, Guid questionId, string? sampleAnswer, string? rubric, int? maxWords)
        : base(id)
    {
        QuestionId = questionId;
        Update(sampleAnswer, rubric, maxWords);
    }

    public void Update(string? sampleAnswer, string? rubric, int? maxWords)
    {
        SampleAnswer = Check.Length(sampleAnswer, nameof(sampleAnswer), QuestionConsts.MaxSampleAnswerLength);
        Rubric = Check.Length(rubric, nameof(rubric), QuestionConsts.MaxRubricLength);
        MaxWords = maxWords;
    }
}

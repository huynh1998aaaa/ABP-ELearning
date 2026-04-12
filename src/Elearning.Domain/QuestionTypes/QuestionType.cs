using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.QuestionTypes;

public class QuestionType : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public QuestionInputKind InputKind { get; private set; }

    public QuestionScoringKind ScoringKind { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsSystem { get; private set; }

    public int SortOrder { get; private set; }

    public bool SupportsOptions { get; private set; }

    public bool SupportsAnswerPairs { get; private set; }

    public bool RequiresManualGrading { get; private set; }

    public bool AllowMultipleCorrectAnswers { get; private set; }

    public int? MinimumOptions { get; private set; }

    public int? MaximumOptions { get; private set; }

    protected QuestionType()
    {
    }

    public QuestionType(
        Guid id,
        string code,
        string displayName,
        QuestionInputKind inputKind,
        QuestionScoringKind scoringKind,
        bool supportsOptions,
        bool supportsAnswerPairs,
        bool requiresManualGrading,
        bool allowMultipleCorrectAnswers,
        int sortOrder,
        bool isSystem = false,
        string? description = null,
        int? minimumOptions = null,
        int? maximumOptions = null)
        : base(id)
    {
        SetCode(code);
        UpdateDetails(
            displayName,
            description,
            inputKind,
            scoringKind,
            supportsOptions,
            supportsAnswerPairs,
            requiresManualGrading,
            allowMultipleCorrectAnswers,
            sortOrder,
            minimumOptions,
            maximumOptions);

        IsSystem = isSystem;
        IsActive = true;
    }

    public void UpdateDetails(
        string displayName,
        string? description,
        QuestionInputKind inputKind,
        QuestionScoringKind scoringKind,
        bool supportsOptions,
        bool supportsAnswerPairs,
        bool requiresManualGrading,
        bool allowMultipleCorrectAnswers,
        int sortOrder,
        int? minimumOptions,
        int? maximumOptions)
    {
        DisplayName = Check.NotNullOrWhiteSpace(displayName, nameof(displayName), QuestionTypeConsts.MaxDisplayNameLength);
        Description = Check.Length(description, nameof(description), QuestionTypeConsts.MaxDescriptionLength);
        InputKind = inputKind;
        ScoringKind = scoringKind;
        SupportsOptions = supportsOptions;
        SupportsAnswerPairs = supportsAnswerPairs;
        RequiresManualGrading = requiresManualGrading;
        AllowMultipleCorrectAnswers = allowMultipleCorrectAnswers;
        SortOrder = sortOrder;

        if (!supportsOptions)
        {
            MinimumOptions = null;
            MaximumOptions = null;
            return;
        }

        MinimumOptions = minimumOptions;
        MaximumOptions = maximumOptions;
    }

    public void SetCode(string code)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), QuestionTypeConsts.MaxCodeLength);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

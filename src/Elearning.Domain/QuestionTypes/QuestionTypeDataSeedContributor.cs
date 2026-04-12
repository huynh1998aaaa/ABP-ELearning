using System;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Uow;

namespace Elearning.QuestionTypes;

public class QuestionTypeDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IRepository<QuestionType, Guid> _questionTypeRepository;

    public QuestionTypeDataSeedContributor(
        IRepository<QuestionType, Guid> questionTypeRepository,
        IGuidGenerator guidGenerator)
    {
        _questionTypeRepository = questionTypeRepository;
        _guidGenerator = guidGenerator;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        await CreateIfNotExistsAsync(
            QuestionTypeCodes.SingleChoice,
            "Trắc nghiệm 1 đáp án",
            QuestionInputKind.SingleChoice,
            QuestionScoringKind.Auto,
            supportsOptions: true,
            supportsAnswerPairs: false,
            requiresManualGrading: false,
            allowMultipleCorrectAnswers: false,
            sortOrder: 10,
            minimumOptions: 2);

        await CreateIfNotExistsAsync(
            QuestionTypeCodes.MultipleChoice,
            "Trắc nghiệm nhiều đáp án",
            QuestionInputKind.MultipleChoice,
            QuestionScoringKind.Auto,
            supportsOptions: true,
            supportsAnswerPairs: false,
            requiresManualGrading: false,
            allowMultipleCorrectAnswers: true,
            sortOrder: 20,
            minimumOptions: 2);

        await CreateIfNotExistsAsync(
            QuestionTypeCodes.Matching,
            "Nối đáp án đúng",
            QuestionInputKind.Matching,
            QuestionScoringKind.Auto,
            supportsOptions: false,
            supportsAnswerPairs: true,
            requiresManualGrading: false,
            allowMultipleCorrectAnswers: false,
            sortOrder: 30);

        await CreateIfNotExistsAsync(
            QuestionTypeCodes.Essay,
            "Tự luận",
            QuestionInputKind.Essay,
            QuestionScoringKind.Manual,
            supportsOptions: false,
            supportsAnswerPairs: false,
            requiresManualGrading: true,
            allowMultipleCorrectAnswers: false,
            sortOrder: 40);
    }

    private async Task CreateIfNotExistsAsync(
        string code,
        string displayName,
        QuestionInputKind inputKind,
        QuestionScoringKind scoringKind,
        bool supportsOptions,
        bool supportsAnswerPairs,
        bool requiresManualGrading,
        bool allowMultipleCorrectAnswers,
        int sortOrder,
        int? minimumOptions = null,
        int? maximumOptions = null)
    {
        if (await _questionTypeRepository.FindAsync(x => x.Code == code) != null)
        {
            return;
        }

        await _questionTypeRepository.InsertAsync(new QuestionType(
            _guidGenerator.Create(),
            code,
            displayName,
            inputKind,
            scoringKind,
            supportsOptions,
            supportsAnswerPairs,
            requiresManualGrading,
            allowMultipleCorrectAnswers,
            sortOrder,
            isSystem: true,
            minimumOptions: minimumOptions,
            maximumOptions: maximumOptions));
    }
}

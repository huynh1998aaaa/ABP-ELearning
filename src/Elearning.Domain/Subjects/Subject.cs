using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Elearning.Subjects;

public class Subject : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    protected Subject()
    {
    }

    public Subject(
        Guid id,
        string code,
        string name,
        int sortOrder,
        string? description = null)
        : base(id)
    {
        IsActive = true;
        UpdateDetails(code, name, description, sortOrder);
    }

    public void UpdateDetails(
        string code,
        string name,
        string? description,
        int sortOrder)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), SubjectConsts.MaxCodeLength);
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), SubjectConsts.MaxNameLength);
        Description = Check.Length(description, nameof(description), SubjectConsts.MaxDescriptionLength);
        SortOrder = sortOrder;
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

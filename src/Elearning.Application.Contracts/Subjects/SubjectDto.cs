using System;
using Volo.Abp.Application.Dtos;

namespace Elearning.Subjects;

public class SubjectDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Elearning.Subjects;

public class CreateSubjectDto
{
    [Required]
    [StringLength(SubjectConsts.MaxCodeLength)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(SubjectConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [StringLength(SubjectConsts.MaxDescriptionLength)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 100;
}

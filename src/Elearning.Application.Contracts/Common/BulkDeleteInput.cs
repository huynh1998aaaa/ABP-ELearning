using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Elearning.Common;

public class BulkDeleteInput
{
    [Required]
    [MinLength(1)]
    public List<Guid> Ids { get; set; } = new();
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Elearning.Web.Pages.Admin.Exams;

public abstract class ExamFormPageModel : ElearningAdminPageModel
{
    public List<SelectListItem> AccessLevelOptions { get; private set; } = new();

    public List<SelectListItem> SelectionModeOptions { get; private set; } = new();

    protected void LoadSelectOptions()
    {
        AccessLevelOptions = Enum.GetValues<Elearning.Exams.ExamAccessLevel>()
            .Select(x => new SelectListItem(L[$"Enum:ExamAccessLevel:{x}"], x.ToString()))
            .ToList();

        SelectionModeOptions = Enum.GetValues<Elearning.Exams.ExamSelectionMode>()
            .Select(x => new SelectListItem(L[$"Enum:ExamSelectionMode:{x}"], x.ToString()))
            .ToList();
    }
}

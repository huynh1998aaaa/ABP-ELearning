using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Elearning.Web.Pages.Admin.Practices;

public abstract class PracticeSetFormPageModel : ElearningAdminPageModel
{
    public List<SelectListItem> AccessLevelOptions { get; private set; } = new();

    public List<SelectListItem> SelectionModeOptions { get; private set; } = new();

    protected void LoadSelectOptions()
    {
        AccessLevelOptions = Enum.GetValues<Elearning.Practices.PracticeAccessLevel>()
            .Select(x => new SelectListItem(L[$"Enum:PracticeAccessLevel:{x}"], x.ToString()))
            .ToList();

        SelectionModeOptions = Enum.GetValues<Elearning.Practices.PracticeSelectionMode>()
            .Select(x => new SelectListItem(L[$"Enum:PracticeSelectionMode:{x}"], x.ToString()))
            .ToList();
    }
}

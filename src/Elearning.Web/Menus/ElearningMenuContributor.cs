using System.Threading.Tasks;
using Elearning.Localization;
using Elearning.MultiTenancy;
using Elearning.Permissions;
using Volo.Abp.Identity.Web.Navigation;
using Volo.Abp.SettingManagement.Web.Navigation;
using Volo.Abp.TenantManagement.Web.Navigation;
using Volo.Abp.UI.Navigation;

namespace Elearning.Web.Menus;

public class ElearningMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var administration = context.Menu.GetAdministration();
        var l = context.GetLocalizer<ElearningResource>();

        context.Menu.Items.Clear();

        context.Menu.Items.Insert(
            0,
            new ApplicationMenuItem(
                ElearningMenus.Home,
                l["Menu:Home"],
                "~/",
                icon: "fas fa-home",
                order: 0
            )
        );

        context.Menu.Items.Insert(
            1,
            new ApplicationMenuItem(
                ElearningMenus.Client,
                l["Menu:Client"],
                "/client",
                icon: "fas fa-compass",
                order: 1
            )
        );

        var adminMenuItem = new ApplicationMenuItem(
            ElearningMenus.Admin,
            l["Menu:Admin"],
            "/admin",
            icon: "fas fa-user-shield",
            order: 2
        );

        adminMenuItem.AddItem(
            new ApplicationMenuItem(
                ElearningMenus.AdminAccounts,
                l["Menu:Accounts"],
                "/Admin/Accounts",
                icon: "fas fa-users"
            )
        );

        adminMenuItem.AddItem(
            new ApplicationMenuItem(
                ElearningMenus.AdminQuestionTypes,
                l["Menu:QuestionTypes"],
                "/Admin/QuestionTypes",
                icon: "fas fa-list-check",
                requiredPermissionName: ElearningPermissions.QuestionTypes.Default
            )
        );

        adminMenuItem.AddItem(
            new ApplicationMenuItem(
                ElearningMenus.AdminQuestions,
                l["Menu:Questions"],
                "/Admin/Questions",
                icon: "fas fa-circle-question",
                requiredPermissionName: ElearningPermissions.Questions.Default
            )
        );

        adminMenuItem.AddItem(
            new ApplicationMenuItem(
                ElearningMenus.AdminPremiumSubscriptions,
                l["Menu:PremiumSubscriptions"],
                "/Admin/PremiumSubscriptions",
                icon: "fas fa-crown",
                requiredPermissionName: ElearningPermissions.PremiumSubscriptions.Default
            )
        );

        context.Menu.Items.Insert(2, adminMenuItem);

        if (MultiTenancyConsts.IsEnabled)
        {
            administration.SetSubItemOrder(TenantManagementMenuNames.GroupName, 1);
        }

        administration.SetSubItemOrder(IdentityMenuNames.GroupName, 2);
        administration.SetSubItemOrder(SettingManagementMenuNames.GroupName, 3);

        return Task.CompletedTask;
    }
}

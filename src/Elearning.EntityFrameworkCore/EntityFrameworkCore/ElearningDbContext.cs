using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using Elearning.Questions;
using Elearning.QuestionTypes;
using Elearning.PremiumSubscriptions;

namespace Elearning.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class ElearningDbContext :
    AbpDbContext<ElearningDbContext>,
    IIdentityDbContext,
    ITenantManagementDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */
    public DbSet<QuestionType> QuestionTypes { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<QuestionOption> QuestionOptions { get; set; }
    public DbSet<QuestionMatchingPair> QuestionMatchingPairs { get; set; }
    public DbSet<QuestionEssayAnswer> QuestionEssayAnswers { get; set; }
    public DbSet<PremiumPlan> PremiumPlans { get; set; }
    public DbSet<UserPremiumSubscription> UserPremiumSubscriptions { get; set; }

    #region Entities from the modules

    /* Notice: We only implemented IIdentityDbContext and ITenantManagementDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityDbContext and ITenantManagementDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    //Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }
    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    #endregion

    public ElearningDbContext(DbContextOptions<ElearningDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureFeatureManagement();
        builder.ConfigureTenantManagement();

        /* Configure your own tables/entities inside here */

        builder.Entity<QuestionType>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "QuestionTypes", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Code).IsRequired().HasMaxLength(QuestionTypeConsts.MaxCodeLength);
            b.Property(x => x.DisplayName).IsRequired().HasMaxLength(QuestionTypeConsts.MaxDisplayNameLength);
            b.Property(x => x.Description).HasMaxLength(QuestionTypeConsts.MaxDescriptionLength);
            b.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<Question>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "Questions", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Title).IsRequired().HasMaxLength(QuestionConsts.MaxTitleLength);
            b.Property(x => x.Content).IsRequired().HasMaxLength(QuestionConsts.MaxContentLength);
            b.Property(x => x.Explanation).HasMaxLength(QuestionConsts.MaxExplanationLength);
            b.Property(x => x.Score).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.QuestionTypeId);
            b.HasOne<QuestionType>().WithMany().HasForeignKey(x => x.QuestionTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<QuestionOption>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "QuestionOptions", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Text).IsRequired().HasMaxLength(QuestionConsts.MaxOptionTextLength);
            b.HasIndex(x => x.QuestionId);
            b.HasOne<Question>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QuestionMatchingPair>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "QuestionMatchingPairs", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.LeftText).IsRequired().HasMaxLength(QuestionConsts.MaxMatchingTextLength);
            b.Property(x => x.RightText).IsRequired().HasMaxLength(QuestionConsts.MaxMatchingTextLength);
            b.HasIndex(x => x.QuestionId);
            b.HasOne<Question>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QuestionEssayAnswer>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "QuestionEssayAnswers", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.SampleAnswer).HasMaxLength(QuestionConsts.MaxSampleAnswerLength);
            b.Property(x => x.Rubric).HasMaxLength(QuestionConsts.MaxRubricLength);
            b.HasIndex(x => x.QuestionId).IsUnique();
            b.HasOne<Question>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PremiumPlan>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "PremiumPlans", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Code).IsRequired().HasMaxLength(PremiumPlanConsts.MaxCodeLength);
            b.Property(x => x.DisplayName).IsRequired().HasMaxLength(PremiumPlanConsts.MaxDisplayNameLength);
            b.Property(x => x.Description).HasMaxLength(PremiumPlanConsts.MaxDescriptionLength);
            b.Property(x => x.Currency).IsRequired().HasMaxLength(PremiumPlanConsts.MaxCurrencyLength);
            b.Property(x => x.Price).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<UserPremiumSubscription>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "UserPremiumSubscriptions", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Note).HasMaxLength(PremiumSubscriptionConsts.MaxNoteLength);
            b.Property(x => x.CancellationReason).HasMaxLength(PremiumSubscriptionConsts.MaxCancellationReasonLength);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.EndTime);
            b.HasIndex(x => x.ActivatedTime);
            b.HasIndex(x => new { x.UserId, x.ActivationNumber });
            b.HasOne<PremiumPlan>().WithMany().HasForeignKey(x => x.PremiumPlanId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

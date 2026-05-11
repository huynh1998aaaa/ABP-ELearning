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
using Elearning.Exams;
using Elearning.LearningSessions;
using Elearning.Practices;
using Elearning.Questions;
using Elearning.QuestionTypes;
using Elearning.PremiumSubscriptions;
using Elearning.Subjects;
using Elearning.UserLoginSessions;

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
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<QuestionOption> QuestionOptions { get; set; }
    public DbSet<QuestionMatchingPair> QuestionMatchingPairs { get; set; }
    public DbSet<QuestionEssayAnswer> QuestionEssayAnswers { get; set; }
    public DbSet<LearningSession> LearningSessions { get; set; }
    public DbSet<LearningSessionQuestion> LearningSessionQuestions { get; set; }
    public DbSet<LearningSessionQuestionOption> LearningSessionQuestionOptions { get; set; }
    public DbSet<LearningSessionQuestionEssayAnswer> LearningSessionQuestionEssayAnswers { get; set; }
    public DbSet<LearningSessionQuestionMatchingPair> LearningSessionQuestionMatchingPairs { get; set; }
    public DbSet<LearningSessionAnswer> LearningSessionAnswers { get; set; }
    public DbSet<PremiumPlan> PremiumPlans { get; set; }
    public DbSet<UserPremiumSubscription> UserPremiumSubscriptions { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<ExamQuestion> ExamQuestions { get; set; }
    public DbSet<ExamAutoQuestionRule> ExamAutoQuestionRules { get; set; }
    public DbSet<PracticeSet> PracticeSets { get; set; }
    public DbSet<PracticeQuestion> PracticeQuestions { get; set; }
    public DbSet<PracticeAutoQuestionRule> PracticeAutoQuestionRules { get; set; }
    public DbSet<UserLoginSession> UserLoginSessions { get; set; }

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

        builder.Entity<Subject>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "Subjects", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Code).IsRequired().HasMaxLength(SubjectConsts.MaxCodeLength);
            b.Property(x => x.Name).IsRequired().HasMaxLength(SubjectConsts.MaxNameLength);
            b.Property(x => x.Description).HasMaxLength(SubjectConsts.MaxDescriptionLength);
            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => x.Name);
            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => x.SortOrder);
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

        builder.Entity<LearningSession>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "LearningSessions", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.SourceCode).IsRequired().HasMaxLength(LearningSessionConsts.MaxSourceCodeLength);
            b.Property(x => x.Title).IsRequired().HasMaxLength(LearningSessionConsts.MaxTitleLength);
            b.Property(x => x.Description).HasMaxLength(LearningSessionConsts.MaxDescriptionLength);
            b.Property(x => x.Score).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.UserId, x.SourceKind, x.SourceId, x.Status });
            b.HasIndex(x => x.StartedAt);
        });

        builder.Entity<LearningSessionQuestion>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "LearningSessionQuestions", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.QuestionTypeCode).IsRequired().HasMaxLength(LearningSessionConsts.MaxQuestionTypeCodeLength);
            b.Property(x => x.QuestionTypeName).IsRequired().HasMaxLength(LearningSessionConsts.MaxQuestionTypeNameLength);
            b.Property(x => x.Title).IsRequired().HasMaxLength(LearningSessionConsts.MaxTitleLength);
            b.Property(x => x.Content).IsRequired().HasMaxLength(QuestionConsts.MaxContentLength);
            b.Property(x => x.Explanation).HasMaxLength(QuestionConsts.MaxExplanationLength);
            b.Property(x => x.Score).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.LearningSessionId);
            b.HasIndex(x => new { x.LearningSessionId, x.SortOrder });
            b.HasOne<LearningSession>().WithMany().HasForeignKey(x => x.LearningSessionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LearningSessionQuestionOption>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "LearningSessionQuestionOptions", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Text).IsRequired().HasMaxLength(QuestionConsts.MaxOptionTextLength);
            b.HasIndex(x => x.LearningSessionQuestionId);
            b.HasIndex(x => new { x.LearningSessionQuestionId, x.SortOrder });
            b.HasOne<LearningSessionQuestion>().WithMany().HasForeignKey(x => x.LearningSessionQuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LearningSessionQuestionMatchingPair>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "LearningSessionQuestionMatchingPairs", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.LeftText).IsRequired().HasMaxLength(QuestionConsts.MaxMatchingTextLength);
            b.Property(x => x.RightText).IsRequired().HasMaxLength(QuestionConsts.MaxMatchingTextLength);
            b.HasIndex(x => x.LearningSessionQuestionId);
            b.HasIndex(x => new { x.LearningSessionQuestionId, x.SortOrder });
            b.HasOne<LearningSessionQuestion>().WithMany().HasForeignKey(x => x.LearningSessionQuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LearningSessionQuestionEssayAnswer>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "LearningSessionQuestionEssayAnswers", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.SampleAnswer).HasMaxLength(QuestionConsts.MaxSampleAnswerLength);
            b.Property(x => x.Rubric).HasMaxLength(QuestionConsts.MaxRubricLength);
            b.HasIndex(x => x.LearningSessionQuestionId).IsUnique();
            b.HasOne<LearningSessionQuestion>().WithMany().HasForeignKey(x => x.LearningSessionQuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LearningSessionAnswer>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "LearningSessionAnswers", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.SelectedOptionIdsJson).HasMaxLength(LearningSessionConsts.MaxSelectedOptionIdsJsonLength);
            b.Property(x => x.MatchingAnswerJson).HasMaxLength(LearningSessionConsts.MaxMatchingAnswerJsonLength);
            b.Property(x => x.EssayAnswerText).HasMaxLength(LearningSessionConsts.MaxEssayAnswerTextLength);
            b.HasIndex(x => x.LearningSessionId);
            b.HasIndex(x => x.LearningSessionQuestionId).IsUnique();
            b.HasOne<LearningSession>().WithMany().HasForeignKey(x => x.LearningSessionId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<LearningSessionQuestion>().WithMany().HasForeignKey(x => x.LearningSessionQuestionId).OnDelete(DeleteBehavior.Restrict);
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

        builder.Entity<Exam>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "Exams", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Code).IsRequired().HasMaxLength(ExamConsts.MaxCodeLength);
            b.Property(x => x.Title).IsRequired().HasMaxLength(ExamConsts.MaxTitleLength);
            b.Property(x => x.Description).HasMaxLength(ExamConsts.MaxDescriptionLength);
            b.Property(x => x.PassingScore).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.AccessLevel);
            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => x.SortOrder);
        });

        builder.Entity<ExamQuestion>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "ExamQuestions", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.ScoreOverride).HasColumnType("decimal(18,2)");
            b.HasIndex(x => new { x.ExamId, x.QuestionId }).IsUnique();
            b.HasIndex(x => x.QuestionId);
            b.HasIndex(x => x.SortOrder);
            b.HasOne<Exam>().WithMany().HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<Question>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ExamAutoQuestionRule>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "ExamAutoQuestionRules", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.HasIndex(x => x.ExamId);
            b.HasIndex(x => new { x.ExamId, x.SortOrder });
            b.HasOne<Exam>().WithMany().HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<QuestionType>().WithMany().HasForeignKey(x => x.QuestionTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PracticeSet>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "PracticeSets", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Code).IsRequired().HasMaxLength(PracticeSetConsts.MaxCodeLength);
            b.Property(x => x.Title).IsRequired().HasMaxLength(PracticeSetConsts.MaxTitleLength);
            b.Property(x => x.Description).HasMaxLength(PracticeSetConsts.MaxDescriptionLength);
            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.AccessLevel);
            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => x.SortOrder);
        });

        builder.Entity<PracticeQuestion>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "PracticeQuestions", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.HasIndex(x => new { x.PracticeSetId, x.QuestionId }).IsUnique();
            b.HasIndex(x => x.QuestionId);
            b.HasIndex(x => x.SortOrder);
            b.HasOne<PracticeSet>().WithMany().HasForeignKey(x => x.PracticeSetId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<Question>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PracticeAutoQuestionRule>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "PracticeAutoQuestionRules", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.HasIndex(x => x.PracticeSetId);
            b.HasIndex(x => new { x.PracticeSetId, x.SortOrder });
            b.HasOne<PracticeSet>().WithMany().HasForeignKey(x => x.PracticeSetId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<QuestionType>().WithMany().HasForeignKey(x => x.QuestionTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserLoginSession>(b =>
        {
            b.ToTable(ElearningConsts.DbTablePrefix + "UserLoginSessions", ElearningConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.DeviceId).IsRequired().HasMaxLength(LoginSessions.UserLoginSessionConsts.MaxDeviceIdLength);
            b.Property(x => x.SessionKey).IsRequired().HasMaxLength(LoginSessions.UserLoginSessionConsts.MaxSessionKeyLength);
            b.Property(x => x.Channel).IsRequired().HasMaxLength(LoginSessions.UserLoginSessionConsts.MaxChannelLength);
            b.Property(x => x.Provider).IsRequired().HasMaxLength(LoginSessions.UserLoginSessionConsts.MaxProviderLength);
            b.Property(x => x.RevokedReason).HasMaxLength(LoginSessions.UserLoginSessionConsts.MaxRevokedReasonLength);
            b.Property(x => x.ClientIp).HasMaxLength(LoginSessions.UserLoginSessionConsts.MaxClientIpLength);
            b.Property(x => x.UserAgent).HasMaxLength(LoginSessions.UserLoginSessionConsts.MaxUserAgentLength);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.DeviceId);
            b.HasIndex(x => x.SessionKey).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.UserId, x.IsCurrent })
                .HasFilter("[IsCurrent] = 1")
                .IsUnique();
        });
    }
}

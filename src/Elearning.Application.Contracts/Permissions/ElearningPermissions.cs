namespace Elearning.Permissions;

public static class ElearningPermissions
{
    public const string GroupName = "Elearning";

    public static class AdminPortal
    {
        public const string Access = GroupName + ".AdminPortal";
    }

    public static class QuestionTypes
    {
        public const string Default = GroupName + ".QuestionTypes";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class Subjects
    {
        public const string Default = GroupName + ".Subjects";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class Questions
    {
        public const string Default = GroupName + ".Questions";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Publish = Default + ".Publish";
        public const string Import = Default + ".Import";
        public const string Delete = Default + ".Delete";
    }

    public static class PremiumSubscriptions
    {
        public const string Default = GroupName + ".PremiumSubscriptions";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Cancel = Default + ".Cancel";
    }

    public static class Exams
    {
        public const string Default = GroupName + ".Exams";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Publish = Default + ".Publish";
        public const string ManageQuestions = Default + ".ManageQuestions";
    }

    public static class Practices
    {
        public const string Default = GroupName + ".Practices";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Publish = Default + ".Publish";
        public const string ManageQuestions = Default + ".ManageQuestions";
    }
}

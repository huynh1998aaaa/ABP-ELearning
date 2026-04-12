namespace Elearning.Permissions;

public static class ElearningPermissions
{
    public const string GroupName = "Elearning";

    //Add your own permission names. Example:
    //public const string MyPermission1 = GroupName + ".MyPermission1";
    public static class QuestionTypes
    {
        public const string Default = GroupName + ".QuestionTypes";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class Questions
    {
        public const string Default = GroupName + ".Questions";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string Publish = Default + ".Publish";
        public const string Import = Default + ".Import";
    }

    public static class PremiumSubscriptions
    {
        public const string Default = GroupName + ".PremiumSubscriptions";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Cancel = Default + ".Cancel";
    }
}

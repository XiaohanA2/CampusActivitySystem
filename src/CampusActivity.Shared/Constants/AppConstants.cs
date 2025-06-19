namespace CampusActivity.Shared.Constants;

public static class AppConstants
{
    public static class Roles
    {
        public const string Student = "Student";
        public const string Teacher = "Teacher";
        public const string Admin = "Admin";
    }

    public static class Policies
    {
        public const string RequireStudentRole = "RequireStudentRole";
        public const string RequireTeacherRole = "RequireTeacherRole";
        public const string RequireAdminRole = "RequireAdminRole";
        public const string RequireStaffRole = "RequireStaffRole"; // Teacher or Admin
    }

    public static class CacheKeys
    {
        public const string Activities = "activities";
        public const string Categories = "categories";
        public const string Users = "users";
        public const string PopularActivities = "popular_activities";
        public const string RecommendedActivities = "recommended_activities_{0}";
    }

    public static class CacheExpiration
    {
        public static readonly TimeSpan Short = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan Medium = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan Long = TimeSpan.FromHours(1);
        public static readonly TimeSpan Daily = TimeSpan.FromDays(1);
    }

    public static class FileUpload
    {
        public const int MaxFileSizeInMB = 5;
        public const long MaxFileSizeInBytes = MaxFileSizeInMB * 1024 * 1024;
        public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        public const string DefaultAvatarPath = "/images/default-avatar.png";
        public const string DefaultActivityImagePath = "/images/default-activity.png";
    }

    public static class Pagination
    {
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 100;
    }

    public static class Recommendation
    {
        public const int MaxRecommendedActivities = 20;
        public const double MinRecommendationScore = 0.1;
        public const int UserHistoryDays = 30;
    }

    public static class Validation
    {
        public const int MinPasswordLength = 6;
        public const int MaxPasswordLength = 100;
        public const int MinUsernameLength = 3;
        public const int MaxUsernameLength = 50;
        public const int MaxActivityTitleLength = 200;
        public const int MaxActivityDescriptionLength = 2000;
        public const int MaxLocationLength = 200;
    }
} 
using System;

namespace CampusActivity.Shared.DTOs
{
    public class AdminActivityDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public int MaxParticipants { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Creator { get; set; } = string.Empty;
        public int RegistrationCount { get; set; }
        public string? ImageUrl { get; set; }
    }
} 
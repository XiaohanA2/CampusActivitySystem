namespace CampusActivity.Shared.Enums;

public enum ScheduleItemType
{
    Personal = 1,      // 个人计划
    Activity = 2,      // 活动安排
    Reminder = 3,      // 提醒事项
    Meeting = 4,       // 会议
    Study = 5,         // 学习
    Other = 6          // 其他
}

public enum ScheduleItemPriority
{
    Low = 1,       // 低优先级
    Medium = 2,    // 中优先级
    High = 3,      // 高优先级
    Urgent = 4     // 紧急
} 
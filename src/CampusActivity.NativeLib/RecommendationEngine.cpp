#include "RecommendationEngine.h"
#include <algorithm>
#include <cmath>

using namespace System;
using namespace System::Collections::Generic;
using namespace CampusActivity::NativeLib;

// 用于排序的比较函数
ref class ScoreComparer : IComparer<RecommendationResult>
{
public:
    virtual int Compare(RecommendationResult a, RecommendationResult b)
    {
        return b.Score.CompareTo(a.Score);
    }
};

RecommendationEngine::RecommendationEngine()
{
}

RecommendationEngine::~RecommendationEngine()
{
}

List<RecommendationResult>^ RecommendationEngine::CalculateRecommendations(
    int userId,
    List<ActivityData>^ activities,
    List<UserPreference>^ userPreferences,
    List<int>^ userRegisteredActivities)
{
    auto results = gcnew List<RecommendationResult>();
    
    // 创建用户偏好字典
    auto preferenceDict = gcnew Dictionary<int, double>();
    for each(UserPreference pref in userPreferences)
    {
        if (pref.UserId == userId)
        {
            preferenceDict[pref.CategoryId] = pref.Weight;
        }
    }
    
    // 为每个活动计算推荐分数
    for each(ActivityData activity in activities)
    {
        // 跳过已报名的活动
        if (userRegisteredActivities->Contains(activity.Id))
            continue;
            
        double score = 0.0;
        String^ reason = "";
        
        // 基于用户偏好的分数
        if (preferenceDict->ContainsKey(activity.CategoryId))
        {
            score += preferenceDict[activity.CategoryId] * 0.4;
            reason += "基于用户偏好; ";
        }
        
        // 活动热度分数
        double popularityScore = CalculatePopularityScore(activity);
        score += popularityScore * 0.3;
        reason += "活动热度; ";
        
        // 时间衰减因子
        double timeDecay = CalculateTimeDecayFactor(activity.StartTime);
        score *= timeDecay;
        reason += "时间因素; ";
        
        // 只推荐分数大于阈值的活动
        if (score > 0.1)
        {
            RecommendationResult result;
            result.ActivityId = activity.Id;
            result.Score = score;
            result.Reason = reason;
            results->Add(result);
        }
    }
    
    // 按分数降序排序
    results->Sort(gcnew ScoreComparer());
    
    return results;
}

List<RecommendationResult>^ RecommendationEngine::CollaborativeFiltering(
    int userId,
    List<ActivityData>^ activities,
    Dictionary<int, List<int>^>^ userActivityMatrix)
{
    auto results = gcnew List<RecommendationResult>();
    
    if (!userActivityMatrix->ContainsKey(userId))
        return results;
        
    auto currentUserActivities = userActivityMatrix[userId];
    auto similarities = gcnew Dictionary<int, double>();
    
    // 计算与其他用户的相似度
    for each(auto kvp in userActivityMatrix)
    {
        if (kvp.Key == userId) continue;
        
        double similarity = CalculateCosineSimilarity(currentUserActivities, kvp.Value);
        if (similarity > 0.1)
        {
            similarities[kvp.Key] = similarity;
        }
    }
    
    // 基于相似用户推荐活动
    auto activityScores = gcnew Dictionary<int, double>();
    
    for each(auto simKvp in similarities)
    {
        int similarUserId = simKvp.Key;
        double similarity = simKvp.Value;
        auto similarUserActivities = userActivityMatrix[similarUserId];
        
        for each(int activityId in similarUserActivities)
        {
            if (!currentUserActivities->Contains(activityId))
            {
                if (!activityScores->ContainsKey(activityId))
                    activityScores[activityId] = 0.0;
                    
                activityScores[activityId] += similarity;
            }
        }
    }
    
    // 生成推荐结果
    for each(auto scoreKvp in activityScores)
    {
        RecommendationResult result;
        result.ActivityId = scoreKvp.Key;
        result.Score = scoreKvp.Value;
        result.Reason = "协同过滤推荐";
        results->Add(result);
    }
    
    return results;
}

List<RecommendationResult>^ RecommendationEngine::ContentBasedRecommendation(
    int userId,
    List<ActivityData>^ activities,
    List<UserPreference>^ userPreferences)
{
    auto results = gcnew List<RecommendationResult>();
    
    // 获取用户偏好
    auto userCategoryWeights = gcnew Dictionary<int, double>();
    for each(UserPreference pref in userPreferences)
    {
        if (pref.UserId == userId)
        {
            userCategoryWeights[pref.CategoryId] = pref.Weight;
        }
    }
    
    // 为每个活动计算内容相似度分数
    for each(ActivityData activity in activities)
    {
        double score = 0.0;
        
        if (userCategoryWeights->ContainsKey(activity.CategoryId))
        {
            score = userCategoryWeights[activity.CategoryId];
            
            // 考虑活动的其他特征
            double capacityFactor = 1.0 - (double)activity.CurrentParticipants / activity.MaxParticipants;
            score *= (0.5 + capacityFactor * 0.5);
            
            RecommendationResult result;
            result.ActivityId = activity.Id;
            result.Score = score;
            result.Reason = "基于内容推荐";
            results->Add(result);
        }
    }
    
    return results;
}

double RecommendationEngine::CalculateCosineSimilarity(List<int>^ vector1, List<int>^ vector2)
{
    auto set1 = gcnew System::Collections::Generic::HashSet<int>(vector1);
    auto set2 = gcnew System::Collections::Generic::HashSet<int>(vector2);
    
    // 计算交集
    int intersection = 0;
    for each(int item in set1)
    {
        if (set2->Contains(item))
            intersection++;
    }
    
    // 计算余弦相似度
    double magnitude1 = Math::Sqrt((double)set1->Count);
    double magnitude2 = Math::Sqrt((double)set2->Count);
    
    if (magnitude1 == 0 || magnitude2 == 0)
        return 0.0;
        
    return intersection / (magnitude1 * magnitude2);
}

double RecommendationEngine::CalculatePopularityScore(ActivityData activity)
{
    // 基于报名人数计算热度
    double registrationRatio = (double)activity.CurrentParticipants / activity.MaxParticipants;
    
    // 使用S型曲线，避免过度偏向满员活动
    return 1.0 / (1.0 + Math::Exp(-10.0 * (registrationRatio - 0.5)));
}

double RecommendationEngine::CalculateTimeDecayFactor(DateTime activityTime)
{
    DateTime now = DateTime::Now;
    TimeSpan timeDiff = activityTime - now;
    
    // 活动已过期
    if (timeDiff.TotalDays < 0)
        return 0.0;
    
    // 时间衰减：越近的活动权重越高
    double days = timeDiff.TotalDays;
    
    if (days <= 7)
        return 1.0;
    else if (days <= 30)
        return 0.8;
    else if (days <= 90)
        return 0.6;
    else
        return 0.4;
} 
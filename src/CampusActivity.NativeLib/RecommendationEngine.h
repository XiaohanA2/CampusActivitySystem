#pragma once

using namespace System;
using namespace System::Collections::Generic;

namespace CampusActivity {
    namespace NativeLib {
        
        // 活动数据结构
        public value struct ActivityData
        {
            int Id;
            int CategoryId;
            String^ Title;
            String^ Description;
            DateTime StartTime;
            String^ Location;
            int MaxParticipants;
            int CurrentParticipants;
        };

        // 用户偏好数据结构
        public value struct UserPreference
        {
            int UserId;
            int CategoryId;
            double Weight;
        };

        // 推荐结果
        public value struct RecommendationResult
        {
            int ActivityId;
            double Score;
            String^ Reason;
        };

        // C++/CLI 推荐引擎类
        public ref class RecommendationEngine
        {
        public:
            RecommendationEngine();
            ~RecommendationEngine();

            // 计算活动推荐分数
            List<RecommendationResult>^ CalculateRecommendations(
                int userId,
                List<ActivityData>^ activities,
                List<UserPreference>^ userPreferences,
                List<int>^ userRegisteredActivities
            );

            // 基于协同过滤的推荐
            List<RecommendationResult>^ CollaborativeFiltering(
                int userId,
                List<ActivityData>^ activities,
                Dictionary<int, List<int>^>^ userActivityMatrix
            );

            // 基于内容的推荐
            List<RecommendationResult>^ ContentBasedRecommendation(
                int userId,
                List<ActivityData>^ activities,
                List<UserPreference>^ userPreferences
            );

        private:
            // 计算余弦相似度
            double CalculateCosineSimilarity(List<int>^ vector1, List<int>^ vector2);
            
            // 计算活动热度分数
            double CalculatePopularityScore(ActivityData activity);
            
            // 计算时间衰减因子
            double CalculateTimeDecayFactor(DateTime activityTime);
        };
    }
} 
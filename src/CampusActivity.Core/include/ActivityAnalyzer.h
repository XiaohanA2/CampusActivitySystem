#pragma once

#include <vector>
#include <string>
#include <map>

#ifdef _WINDOWS
    #ifdef CAMPUSACTIVITYCORE_EXPORTS
        #define CAMPUSACTIVITYCORE_API __declspec(dllexport)
    #else
        #define CAMPUSACTIVITYCORE_API __declspec(dllimport)
    #endif
#else
    #define CAMPUSACTIVITYCORE_API
#endif

namespace CampusActivity {
    namespace Core {
        
        struct ActivityInfo {
            int id;
            int categoryId;
            std::string title;
            std::string description;
            std::string location;
            int maxParticipants;
            int currentParticipants;
            long long startTime; // Unix timestamp
        };

        struct AnalysisResult {
            double popularityScore;
            double trendScore;
            std::vector<std::string> keywords;
            std::map<std::string, double> categoryDistribution;
        };

        class CAMPUSACTIVITYCORE_API ActivityAnalyzer {
        public:
            ActivityAnalyzer();
            ~ActivityAnalyzer();

            // 分析活动数据
            AnalysisResult AnalyzeActivities(const std::vector<ActivityInfo>& activities);
            
            // 计算活动热度
            double CalculatePopularity(const ActivityInfo& activity);
            
            // 提取关键词
            std::vector<std::string> ExtractKeywords(const std::string& text, int maxKeywords = 10);
            
            // 分析活动趋势
            double AnalyzeTrend(const std::vector<ActivityInfo>& activities, int categoryId);
            
            // 计算分类分布
            std::map<std::string, double> CalculateCategoryDistribution(const std::vector<ActivityInfo>& activities);

        private:
            // 文本预处理
            std::string PreprocessText(const std::string& text);
            
            // 计算TF-IDF
            std::map<std::string, double> CalculateTFIDF(const std::vector<std::string>& documents);
            
            // 停用词过滤
            bool IsStopWord(const std::string& word);
        };
    }
} 
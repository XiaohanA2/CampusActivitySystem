#pragma once

#ifdef _WINDOWS
    #ifdef CAMPUSACTIVITYCORE_EXPORTS
        #define CAMPUSACTIVITYCORE_API __declspec(dllexport)
    #else
        #define CAMPUSACTIVITYCORE_API __declspec(dllimport)
    #endif
#else
    #define CAMPUSACTIVITYCORE_API
#endif

#include "ActivityAnalyzer.h"
#include "DataProcessor.h"
#include "StringUtils.h" 
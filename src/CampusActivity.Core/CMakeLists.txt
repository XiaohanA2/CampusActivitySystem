cmake_minimum_required(VERSION 3.16)
project(CampusActivityCore)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# 设置输出目录
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)

# 源文件
set(SOURCES
    src/ActivityAnalyzer.cpp
    src/DataProcessor.cpp
    src/StringUtils.cpp
)

# 头文件
set(HEADERS
    include/ActivityAnalyzer.h
    include/DataProcessor.h
    include/StringUtils.h
    include/CampusActivityCore.h
)

# 创建动态库
add_library(CampusActivityCore SHARED ${SOURCES} ${HEADERS})

# 包含目录
target_include_directories(CampusActivityCore PUBLIC include)

# 编译定义
target_compile_definitions(CampusActivityCore PRIVATE CAMPUSACTIVITYCORE_EXPORTS)

# Windows特定设置
if(WIN32)
    target_compile_definitions(CampusActivityCore PRIVATE _WINDOWS)
    set_target_properties(CampusActivityCore PROPERTIES
        WINDOWS_EXPORT_ALL_SYMBOLS ON
    )
endif()

# 安装规则
install(TARGETS CampusActivityCore
    RUNTIME DESTINATION bin
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
)

install(FILES ${HEADERS} DESTINATION include) 
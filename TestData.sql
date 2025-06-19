-- =================================
-- 校园活动管理系统测试数据插入脚本
-- =================================

-- 设置 MySQL 时区和编码
SET time_zone = '+00:00';
SET NAMES utf8mb4;
SET foreign_key_checks = 0;

-- =================================
-- 清理现有测试数据（按依赖关系顺序）
-- =================================
DELETE FROM ActivityTags WHERE ActivityId IN (SELECT Id FROM Activities WHERE CreatedBy <= 16);
DELETE FROM UserActivityPreferences WHERE UserId <= 16;
DELETE FROM ActivityRegistrations WHERE UserId <= 16 OR ActivityId IN (SELECT Id FROM Activities WHERE CreatedBy <= 16);
DELETE FROM Activities WHERE CreatedBy <= 16;
DELETE FROM Users WHERE Id <= 16;
DELETE FROM ActivityCategories WHERE Id <= 8;

-- 重置自增ID
ALTER TABLE ActivityCategories AUTO_INCREMENT = 1;
ALTER TABLE Users AUTO_INCREMENT = 1;
ALTER TABLE Activities AUTO_INCREMENT = 1;
ALTER TABLE ActivityRegistrations AUTO_INCREMENT = 1;
ALTER TABLE ActivityTags AUTO_INCREMENT = 1;
ALTER TABLE UserActivityPreferences AUTO_INCREMENT = 1;

SET foreign_key_checks = 1;

-- =================================
-- 1. 活动分类数据 (ActivityCategories)
-- =================================
INSERT INTO ActivityCategories (Name, Description, IconUrl, IsActive, SortOrder, CreatedAt, UpdatedAt, IsDeleted) VALUES
('学术讲座', '学术研究、专业知识分享、学术交流活动', '/icons/academic.svg', 1, 1, NOW(), NOW(), 0),
('文艺演出', '音乐、舞蹈、戏剧、文艺汇演等文化活动', '/icons/culture.svg', 1, 2, NOW(), NOW(), 0),
('体育竞技', '各类体育比赛、健身活动、运动会等', '/icons/sports.svg', 1, 3, NOW(), NOW(), 0),
('社会实践', '志愿服务、社会调研、公益活动等实践活动', '/icons/volunteer.svg', 1, 4, NOW(), NOW(), 0),
('创新创业', '创业大赛、创新项目、科技竞赛等', '/icons/innovation.svg', 1, 5, NOW(), NOW(), 0),
('交流参观', '企业参观、学术交流、文化访问等', '/icons/visit.svg', 1, 6, NOW(), NOW(), 0),
('社团活动', '各类学生社团组织的活动', '/icons/club.svg', 1, 7, NOW(), NOW(), 0),
('就业招聘', '招聘会、就业指导、职业规划等', '/icons/career.svg', 1, 8, NOW(), NOW(), 0);

-- =================================
-- 2. 用户数据 (Users)
-- =================================

-- 管理员用户
INSERT INTO Users (Username, Email, PasswordHash, FullName, Phone, Role, IsActive, StudentId, Major, Grade, EmployeeId, Department, Title, CreatedAt, UpdatedAt, IsDeleted) VALUES
('admin', 'admin@campus.edu.cn', '$2a$11$N.7QlhBTpJ4.TBsYZHB8E.jGhKmKoRWa5.Eo6/nPKJ1p7qE8N8lhO', '系统管理员', '13800138000', 3, 1, NULL, NULL, NULL, 'EMP001', '信息技术中心', '系统管理员', NOW(), NOW(), 0),
('superadmin', 'superadmin@campus.edu.cn', '$2a$11$N.7QlhBTpJ4.TBsYZHB8E.jGhKmKoRWa5.Eo6/nPKJ1p7qE8N8lhO', '超级管理员', '13800138001', 3, 1, NULL, NULL, NULL, 'EMP002', '校长办公室', '超级管理员', NOW(), NOW(), 0);

-- 教师用户
INSERT INTO Users (Username, Email, PasswordHash, FullName, Phone, Role, IsActive, StudentId, Major, Grade, EmployeeId, Department, Title, CreatedAt, UpdatedAt, IsDeleted) VALUES
('zhangsan', 'zhangsan@campus.edu.cn', '$2a$11$N.7QlhBTpJ4.TBsYZHB8E.jGhKmKoRWa5.Eo6/nPKJ1p7qE8N8lhO', '张三', '13901234567', 2, 1, NULL, NULL, NULL, 'T001', '计算机科学与技术学院', '教授', NOW(), NOW(), 0),
('lisi', 'lisi@campus.edu.cn', '$2a$11$N.7QlhBTpJ4.TBsYZHB8E.jGhKmKoRWa5.Eo6/nPKJ1p7qE8N8lhO', '李四', '13901234568', 2, 1, NULL, NULL, NULL, 'T002', '经济管理学院', '副教授', NOW(), NOW(), 0),
('wangwu', 'wangwu@campus.edu.cn', '$2a$11$N.7QlhBTpJ4.TBsYZHB8E.jGhKmKoRWa5.Eo6/nPKJ1p7qE8N8lhO', '王五', '13901234569', 2, 1, NULL, NULL, NULL, 'T003', '文学与新闻传播学院', '讲师', NOW(), NOW(), 0),
('zhaoliu', 'zhaoliu@campus.edu.cn', '$2a$11$N.7QlhBTpJ4.TBsYZHB8E.jGhKmKoRWa5.Eo6/nPKJ1p7qE8N8lhO', '赵六', '13901234570', 2, 1, NULL, NULL, NULL, 'T004', '体育学院', '副教授', NOW(), NOW(), 0);

-- 学生用户
INSERT INTO Users (Username, Email, PasswordHash, FullName, Phone, Role, IsActive, StudentId, Major, Grade, EmployeeId, Department, Title, CreatedAt, UpdatedAt, IsDeleted) VALUES
('student001', 'student001@stu.campus.edu.cn', '$2a$11$N.7QlhBTpJ4.TBsYZHB8E.jGhKmKoRWa5.Eo6/nPKJ1p7qE8N8lhO', '刘小明', '13812345678', 1, 1, '2021001001', '计算机科学与技术', 2023, NULL, NULL, NULL, NOW(), NOW(), 0),
('student002', 'student002@stu.campus.edu.cn', '$2a$11$N.7QlhBTpJ4.TBsYZHB8E.jGhKmKoRWa5.Eo6/nPKJ1p7qE8N8lhO', '陈小红', '13812345679', 1, 1, '2021001002', '软件工程', 2023, NULL, NULL, NULL, NOW(), NOW(), 0),
('student003', 'student003@stu.campus.edu.cn', '$2a$11$N.7QlhBTpJ4.TBsYZHB8E.jGhKmKoRWa5.Eo6/nPKJ1p7qE8N8lhO', '李小芳', '13812345680', 1, 1, '2022002001', '国际经济与贸易', 2022, NULL, NULL, NULL, NOW(), NOW(), 0),
('student004', 'student004@stu.campus.edu.cn', '$2a$11$N.7QlhBTpJ4.TBsYZHB8E.jGhKmKoRWa5.Eo6/nPKJ1p7qE8N8lhO', '王小强', '13812345681', 1, 1, '2022002002', '工商管理', 2022, NULL, NULL, NULL, NOW(), NOW(), 0),
('student005', 'student005@stu.campus.edu.cn', '$2a$11$N.7QlhBTpJ4.TBsYZHB8E.jGhKmKoRWa5.Eo6/nPKJ1p7qE8N8lhO', '张小华', '13812345682', 1, 1, '2023003001', '汉语言文学', 2021, NULL, NULL, NULL, NOW(), NOW(), 0),
('student006', 'student006@stu.campus.edu.cn', '$2a$11$N.7QlhBTpJ4.TBsYZHB8E.jGhKmKoRWa5.Eo6/nPKJ1p7qE8N8lhO', '赵小丽', '13812345683', 1, 1, '2023003002', '新闻学', 2021, NULL, NULL, NULL, NOW(), NOW(), 0),
('student007', 'student007@stu.campus.edu.cn', '$2a$11$N.7QlhBTpJ4.TBsYZHB8E.jGhKmKoRWa5.Eo6/nPKJ1p7qE8N8lhO', '孙小龙', '13812345684', 1, 1, '2021004001', '体育教育', 2023, NULL, NULL, NULL, NOW(), NOW(), 0),
('student008', 'student008@stu.campus.edu.cn', '$2a$11$N.7QlhBTpJ4.TBsYZHB8E.jGhKmKoRWa5.Eo6/nPKJ1p7qE8N8lhO', '周小燕', '13812345685', 1, 1, '2021004002', '运动训练', 2023, NULL, NULL, NULL, NOW(), NOW(), 0),
('student009', 'student009@stu.campus.edu.cn', '$2a$11$N.7QlhBTpJ4.TBsYZHB8E.jGhKmKoRWa5.Eo6/nPKJ1p7qE8N8lhO', '吴小斌', '13812345686', 1, 1, '2022005001', '电子信息工程', 2022, NULL, NULL, NULL, NOW(), NOW(), 0),
('student010', 'student010@stu.campus.edu.cn', '$2a$11$N.7QlhBTpJ4.TBsYZHB8E.jGhKmKoRWa5.Eo6/nPKJ1p7qE8N8lhO', '郑小雯', '13812345687', 1, 1, '2022005002', '通信工程', 2022, NULL, NULL, NULL, NOW(), NOW(), 0);

-- =================================
-- 3. 活动数据 (Activities)
-- =================================

-- 学术讲座类活动
INSERT INTO Activities (Title, Description, Location, StartTime, EndTime, RegistrationDeadline, MaxParticipants, CurrentParticipants, ImageUrl, Status, CategoryId, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt, IsDeleted) VALUES
('人工智能前沿技术讲座', '邀请业界专家分享最新AI技术发展趋势，包括大模型、机器学习、深度学习等前沿技术。', '学术报告厅A101', '2024-01-15 14:00:00', '2024-01-15 16:00:00', '2024-01-14 18:00:00', 200, 0, '/images/ai-lecture.jpg', 1, 1, 3, 3, NOW(), NOW(), 0),
('区块链技术及应用研讨会', '深入探讨区块链技术原理、应用场景以及未来发展方向，邀请技术专家现场答疑。', '计算机学院会议室', '2024-01-18 09:00:00', '2024-01-18 11:30:00', '2024-01-17 12:00:00', 80, 0, '/images/blockchain.jpg', 1, 1, 3, 3, NOW(), NOW(), 0),
('数字经济时代的商业模式创新', '分析数字化转型背景下企业商业模式的变革与创新策略。', '经管学院阶梯教室', '2024-01-20 15:30:00', '2024-01-20 17:30:00', '2024-01-19 20:00:00', 150, 0, '/images/digital-economy.jpg', 1, 1, 4, 4, NOW(), NOW(), 0);

-- 文艺演出类活动
INSERT INTO Activities (Title, Description, Location, StartTime, EndTime, RegistrationDeadline, MaxParticipants, CurrentParticipants, ImageUrl, Status, CategoryId, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt, IsDeleted) VALUES
('校园新年音乐会', '由校合唱团、器乐队联合演出，演唱经典中外歌曲，演奏优美乐曲，欢庆新年。', '大学生活动中心', '2024-01-25 19:30:00', '2024-01-25 21:30:00', '2024-01-23 18:00:00', 500, 0, '/images/concert.jpg', 1, 2, 5, 5, NOW(), NOW(), 0),
('话剧《青春无悔》演出', '原创校园话剧，讲述当代大学生奋斗成长的青春故事，传递正能量。', '艺术楼小剧场', '2024-01-28 19:00:00', '2024-01-28 21:00:00', '2024-01-26 12:00:00', 120, 0, '/images/drama.jpg', 1, 2, 5, 5, NOW(), NOW(), 0),
('传统文化诗词朗诵会', '师生共同参与，朗诵经典古诗词，感受中华传统文化魅力。', '图书馆报告厅', '2024-02-02 14:30:00', '2024-02-02 16:30:00', '2024-02-01 18:00:00', 80, 0, '/images/poetry.jpg', 1, 2, 5, 5, NOW(), NOW(), 0);

-- 体育竞技类活动
INSERT INTO Activities (Title, Description, Location, StartTime, EndTime, RegistrationDeadline, MaxParticipants, CurrentParticipants, ImageUrl, Status, CategoryId, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt, IsDeleted) VALUES
('校园马拉松比赛', '全校师生共同参与的健康跑活动，设置5公里、10公里两个组别。', '校园环形跑道', '2024-02-05 08:00:00', '2024-02-05 11:00:00', '2024-02-03 18:00:00', 300, 0, '/images/marathon.jpg', 1, 3, 6, 6, NOW(), NOW(), 0),
('篮球友谊赛', '各学院篮球队友谊赛，促进学院间交流，展现青春活力。', '体育馆篮球场', '2024-02-08 16:00:00', '2024-02-08 18:30:00', '2024-02-06 20:00:00', 100, 0, '/images/basketball.jpg', 1, 3, 6, 6, NOW(), NOW(), 0),
('羽毛球大赛', '校内羽毛球爱好者比赛，设置男单、女单、混双等项目。', '体育馆羽毛球场', '2024-02-12 09:00:00', '2024-02-12 17:00:00', '2024-02-10 18:00:00', 64, 0, '/images/badminton.jpg', 1, 3, 6, 6, NOW(), NOW(), 0);

-- 社会实践类活动
INSERT INTO Activities (Title, Description, Location, StartTime, EndTime, RegistrationDeadline, MaxParticipants, CurrentParticipants, ImageUrl, Status, CategoryId, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt, IsDeleted) VALUES
('社区养老院志愿服务', '组织学生到附近养老院开展志愿服务活动，陪伴老人，传递温暖。', '阳光养老院', '2024-02-15 09:00:00', '2024-02-15 16:00:00', '2024-02-13 18:00:00', 30, 0, '/images/volunteer.jpg', 1, 4, 4, 4, NOW(), NOW(), 0),
('环保宣传实践活动', '到社区开展环保知识宣传，提高居民环保意识，共建美好家园。', '春华社区', '2024-02-18 14:00:00', '2024-02-18 17:00:00', '2024-02-16 20:00:00', 50, 0, '/images/environment.jpg', 1, 4, 4, 4, NOW(), NOW(), 0);

-- 创新创业类活动
INSERT INTO Activities (Title, Description, Location, StartTime, EndTime, RegistrationDeadline, MaxParticipants, CurrentParticipants, ImageUrl, Status, CategoryId, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt, IsDeleted) VALUES
('大学生创业计划大赛', '鼓励学生展示创新创业项目，提供项目路演和专家指导机会。', '创新创业园', '2024-02-22 09:00:00', '2024-02-22 17:00:00', '2024-02-20 18:00:00', 100, 0, '/images/startup.jpg', 1, 5, 3, 3, NOW(), NOW(), 0),
('科技创新作品展示', '展示学生科技创新成果，促进学术交流与合作。', '科技楼展厅', '2024-02-25 10:00:00', '2024-02-25 16:00:00', '2024-02-23 18:00:00', 200, 0, '/images/innovation.jpg', 1, 5, 3, 3, NOW(), NOW(), 0);

-- 交流参观类活动
INSERT INTO Activities (Title, Description, Location, StartTime, EndTime, RegistrationDeadline, MaxParticipants, CurrentParticipants, ImageUrl, Status, CategoryId, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt, IsDeleted) VALUES
('知名科技企业参观', '组织学生参观当地知名科技企业，了解行业发展和就业前景。', '华为技术有限公司', '2024-03-01 09:00:00', '2024-03-01 16:00:00', '2024-02-28 18:00:00', 40, 0, '/images/company-visit.jpg', 1, 6, 3, 3, NOW(), NOW(), 0),
('博物馆文化之旅', '参观市博物馆，感受历史文化底蕴，增长见识。', '市历史博物馆', '2024-03-05 14:00:00', '2024-03-05 17:00:00', '2024-03-03 18:00:00', 60, 0, '/images/museum.jpg', 1, 6, 5, 5, NOW(), NOW(), 0);

-- =================================
-- 4. 活动标签数据 (ActivityTags)
-- =================================
INSERT INTO ActivityTags (ActivityId, TagName, CreatedAt, UpdatedAt, IsDeleted) VALUES
-- AI讲座标签
(1, '人工智能', NOW(), NOW(), 0),
(1, '技术前沿', NOW(), NOW(), 0),
(1, '学术报告', NOW(), NOW(), 0),
-- 区块链研讨会标签
(2, '区块链', NOW(), NOW(), 0),
(2, '技术研讨', NOW(), NOW(), 0),
(2, '专家答疑', NOW(), NOW(), 0),
-- 数字经济讲座标签
(3, '数字经济', NOW(), NOW(), 0),
(3, '商业模式', NOW(), NOW(), 0),
(3, '创新思维', NOW(), NOW(), 0),
-- 音乐会标签
(4, '音乐', NOW(), NOW(), 0),
(4, '新年庆典', NOW(), NOW(), 0),
(4, '文艺演出', NOW(), NOW(), 0),
-- 话剧标签
(5, '话剧', NOW(), NOW(), 0),
(5, '原创作品', NOW(), NOW(), 0),
(5, '青春励志', NOW(), NOW(), 0),
-- 马拉松标签
(8, '马拉松', NOW(), NOW(), 0),
(8, '健康运动', NOW(), NOW(), 0),
(8, '全民参与', NOW(), NOW(), 0),
-- 志愿服务标签
(11, '志愿服务', NOW(), NOW(), 0),
(11, '敬老爱老', NOW(), NOW(), 0),
(11, '社会责任', NOW(), NOW(), 0);

-- =================================
-- 5. 活动报名数据 (ActivityRegistrations)
-- =================================

-- 为AI讲座添加报名记录
INSERT INTO ActivityRegistrations (ActivityId, UserId, Status, Note, RegistrationTime, CreatedAt, UpdatedAt, IsDeleted) VALUES
(1, 7, 1, '对AI技术很感兴趣', '2024-01-10 14:30:00', NOW(), NOW(), 0),
(1, 8, 1, '希望了解最新技术趋势', '2024-01-10 15:45:00', NOW(), NOW(), 0),
(1, 9, 1, NULL, '2024-01-11 09:20:00', NOW(), NOW(), 0),
(1, 10, 1, '为毕业设计收集资料', '2024-01-11 16:30:00', NOW(), NOW(), 0),
(1, 11, 1, NULL, '2024-01-12 10:15:00', NOW(), NOW(), 0);

-- 为音乐会添加报名记录
INSERT INTO ActivityRegistrations (ActivityId, UserId, Status, Note, RegistrationTime, CreatedAt, UpdatedAt, IsDeleted) VALUES
(4, 7, 1, '喜欢音乐，期待演出', '2024-01-20 19:30:00', NOW(), NOW(), 0),
(4, 8, 1, NULL, '2024-01-21 08:45:00', NOW(), NOW(), 0),
(4, 12, 1, '和朋友一起参加', '2024-01-21 14:20:00', NOW(), NOW(), 0),
(4, 13, 1, NULL, '2024-01-22 11:30:00', NOW(), NOW(), 0),
(4, 14, 1, '支持校园文化活动', '2024-01-22 16:45:00', NOW(), NOW(), 0),
(4, 15, 1, NULL, '2024-01-23 09:15:00', NOW(), NOW(), 0),
(4, 16, 1, NULL, '2024-01-23 13:50:00', NOW(), NOW(), 0);

-- 为马拉松比赛添加报名记录
INSERT INTO ActivityRegistrations (ActivityId, UserId, Status, Note, RegistrationTime, CreatedAt, UpdatedAt, IsDeleted) VALUES
(8, 13, 1, '报名5公里组', '2024-01-30 10:30:00', NOW(), NOW(), 0),
(8, 14, 1, '报名10公里组', '2024-01-30 14:20:00', NOW(), NOW(), 0),
(8, 15, 1, '报名5公里组', '2024-01-31 09:45:00', NOW(), NOW(), 0),
(8, 16, 1, '报名5公里组，第一次参加', '2024-01-31 16:30:00', NOW(), NOW(), 0),
(8, 7, 1, '报名10公里组', '2024-02-01 11:15:00', NOW(), NOW(), 0);

-- =================================
-- 6. 用户活动偏好数据 (UserActivityPreferences)
-- =================================
INSERT INTO UserActivityPreferences (UserId, CategoryId, Weight, CreatedAt, UpdatedAt, IsDeleted) VALUES
-- 学生001的偏好 (计算机专业学生)
(7, 1, 0.9, NOW(), NOW(), 0),  -- 学术讲座 高权重
(7, 5, 0.8, NOW(), NOW(), 0),  -- 创新创业 高权重
(7, 3, 0.6, NOW(), NOW(), 0),  -- 体育竞技 中等权重
(7, 2, 0.4, NOW(), NOW(), 0),  -- 文艺演出 低权重

-- 学生002的偏好 (软件工程学生)
(8, 1, 0.85, NOW(), NOW(), 0), -- 学术讲座
(8, 5, 0.9, NOW(), NOW(), 0),  -- 创新创业
(8, 6, 0.7, NOW(), NOW(), 0),  -- 交流参观
(8, 3, 0.5, NOW(), NOW(), 0),  -- 体育竞技

-- 学生003的偏好 (经济学学生)
(9, 1, 0.7, NOW(), NOW(), 0),  -- 学术讲座
(9, 5, 0.8, NOW(), NOW(), 0),  -- 创新创业
(9, 6, 0.9, NOW(), NOW(), 0),  -- 交流参观
(9, 2, 0.6, NOW(), NOW(), 0),  -- 文艺演出

-- 学生004的偏好 (管理学学生)
(10, 1, 0.6, NOW(), NOW(), 0), -- 学术讲座
(10, 5, 0.9, NOW(), NOW(), 0), -- 创新创业
(10, 6, 0.8, NOW(), NOW(), 0), -- 交流参观
(10, 4, 0.7, NOW(), NOW(), 0), -- 社会实践

-- 学生005的偏好 (文学学生)
(11, 2, 0.9, NOW(), NOW(), 0), -- 文艺演出
(11, 1, 0.7, NOW(), NOW(), 0), -- 学术讲座
(11, 4, 0.8, NOW(), NOW(), 0), -- 社会实践
(11, 6, 0.6, NOW(), NOW(), 0), -- 交流参观

-- 学生006的偏好 (新闻学学生)
(12, 2, 0.8, NOW(), NOW(), 0), -- 文艺演出
(12, 4, 0.9, NOW(), NOW(), 0), -- 社会实践
(12, 6, 0.7, NOW(), NOW(), 0), -- 交流参观
(12, 1, 0.6, NOW(), NOW(), 0), -- 学术讲座

-- 学生007的偏好 (体育学生)
(13, 3, 0.95, NOW(), NOW(), 0), -- 体育竞技
(13, 4, 0.8, NOW(), NOW(), 0),  -- 社会实践
(13, 2, 0.5, NOW(), NOW(), 0),  -- 文艺演出
(13, 1, 0.4, NOW(), NOW(), 0),  -- 学术讲座

-- 学生008的偏好 (体育学生)
(14, 3, 0.9, NOW(), NOW(), 0),  -- 体育竞技
(14, 4, 0.7, NOW(), NOW(), 0),  -- 社会实践
(14, 2, 0.6, NOW(), NOW(), 0),  -- 文艺演出
(14, 5, 0.5, NOW(), NOW(), 0),  -- 创新创业

-- 学生009的偏好 (工程学生)
(15, 1, 0.8, NOW(), NOW(), 0),  -- 学术讲座
(15, 5, 0.85, NOW(), NOW(), 0), -- 创新创业
(15, 6, 0.7, NOW(), NOW(), 0),  -- 交流参观
(15, 3, 0.6, NOW(), NOW(), 0),  -- 体育竞技

-- 学生010的偏好 (工程学生)
(16, 1, 0.9, NOW(), NOW(), 0),  -- 学术讲座
(16, 5, 0.8, NOW(), NOW(), 0),  -- 创新创业
(16, 3, 0.7, NOW(), NOW(), 0),  -- 体育竞技
(16, 6, 0.6, NOW(), NOW(), 0);  -- 交流参观

-- =================================
-- 7. 更新活动当前参与人数
-- =================================
UPDATE Activities SET CurrentParticipants = (
    SELECT COUNT(*) FROM ActivityRegistrations 
    WHERE ActivityRegistrations.ActivityId = Activities.Id 
    AND ActivityRegistrations.Status = 1
    AND ActivityRegistrations.IsDeleted = 0
);

-- =================================
-- 8. 添加一些历史活动数据 (已完成的活动)
-- =================================
INSERT INTO Activities (Title, Description, Location, StartTime, EndTime, RegistrationDeadline, MaxParticipants, CurrentParticipants, ImageUrl, Status, CategoryId, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt, IsDeleted) VALUES
('2023年度科技创新成果展', '展示本年度师生科技创新成果，促进学术交流。', '科技楼展厅', '2023-12-15 09:00:00', '2023-12-15 17:00:00', '2023-12-13 18:00:00', 200, 180, '/images/tech-expo-2023.jpg', 3, 5, 3, 3, '2023-12-01 10:00:00', '2023-12-16 10:00:00', 0),
('迎新年文艺汇演', '各学院学生精彩演出，共庆新年。', '大礼堂', '2023-12-28 19:00:00', '2023-12-28 21:30:00', '2023-12-26 18:00:00', 800, 750, '/images/new-year-show.jpg', 3, 2, 5, 5, '2023-12-10 14:00:00', '2023-12-29 10:00:00', 0),
('校园环保行动日', '全校师生参与的环保宣传和实践活动。', '全校区', '2023-11-20 08:00:00', '2023-11-20 17:00:00', '2023-11-18 20:00:00', 500, 420, '/images/eco-action.jpg', 3, 4, 4, 4, '2023-11-05 09:00:00', '2023-11-21 10:00:00', 0);

-- =================================
-- 脚本执行完成提示
-- =================================
SELECT '测试数据插入完成！' as Message;

-- 查看插入的数据统计
SELECT 
    '活动分类' as DataType, COUNT(*) as Count FROM ActivityCategories WHERE IsDeleted = 0
UNION ALL
SELECT 
    '用户数据' as DataType, COUNT(*) as Count FROM Users WHERE IsDeleted = 0
UNION ALL
SELECT 
    '活动数据' as DataType, COUNT(*) as Count FROM Activities WHERE IsDeleted = 0
UNION ALL
SELECT 
    '报名记录' as DataType, COUNT(*) as Count FROM ActivityRegistrations WHERE IsDeleted = 0
UNION ALL
SELECT 
    '用户偏好' as DataType, COUNT(*) as Count FROM UserActivityPreferences WHERE IsDeleted = 0
UNION ALL
SELECT 
    '活动标签' as DataType, COUNT(*) as Count FROM ActivityTags WHERE IsDeleted = 0; 
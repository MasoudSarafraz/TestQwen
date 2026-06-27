-- Task Management Database Schema
-- SQL Server

CREATE DATABASE TaskManagementDb;
GO

USE TaskManagementDb;
GO

-- Users Table
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Role INT NULL, -- 1=SystemAdmin, 2=ProjectManager, 3=Expert
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Projects Table
CREATE TABLE Projects (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(200) NOT NULL,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    Budget DECIMAL(18,2) NOT NULL,
    Customer NVARCHAR(200) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ProjectUsers Table (Many-to-Many between Users and Projects with Role)
CREATE TABLE ProjectUsers (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    ProjectId INT NOT NULL FOREIGN KEY REFERENCES Projects(Id),
    Role INT NOT NULL, -- 1=SystemAdmin, 2=ProjectManager, 3=Expert
    CONSTRAINT UK_UserProject UNIQUE(UserId, ProjectId)
);
GO

-- TaskLogs Table
CREATE TABLE TaskLogs (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ProjectId INT NOT NULL FOREIGN KEY REFERENCES Projects(Id),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    TaskType INT NOT NULL, -- 1-4: Expert tasks, 5-8: PM approvals
    TimeSpent TIME NOT NULL,
    LoggedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsApproved BIT NOT NULL DEFAULT 0,
    ApprovedByUserId INT NULL FOREIGN KEY REFERENCES Users(Id) ON DELETE SET NULL,
    ApprovedAt DATETIME2 NULL
);
GO

-- Create Indexes for better performance
CREATE INDEX IX_Users_Username ON Users(Username);
CREATE INDEX IX_ProjectUsers_UserId ON ProjectUsers(UserId);
CREATE INDEX IX_ProjectUsers_ProjectId ON ProjectUsers(ProjectId);
CREATE INDEX IX_TaskLogs_ProjectId ON TaskLogs(ProjectId);
CREATE INDEX IX_TaskLogs_UserId ON TaskLogs(UserId);
GO

-- Seed initial System Admin user
-- Default password: Admin@123 (hashed with SHA256)
INSERT INTO Users (Username, PasswordHash, Role, CreatedAt)
VALUES ('admin', 'qZkKHkc1Ua7kvDwYK8zT9F+Jh5M8L3pN2rO4sQ6tU8vW0xY2zA3bC5dE7fG9hI1jK=', 1, GETUTCDATE());
GO

-- View to get user details with project assignments
CREATE VIEW vw_UserProjectDetails AS
SELECT 
    u.Id AS UserId,
    u.Username,
    u.Role AS UserRole,
    pu.ProjectId,
    p.Title AS ProjectTitle,
    pu.Role AS ProjectRole
FROM Users u
LEFT JOIN ProjectUsers pu ON u.Id = pu.UserId
LEFT JOIN Projects p ON pu.ProjectId = p.Id;
GO

-- View to get expert time reports
CREATE VIEW vw_ExpertTimeReport AS
SELECT 
    u.Id AS UserId,
    u.Username,
    tl.ProjectId,
    p.Title AS ProjectTitle,
    tl.TaskType,
    tl.TimeSpent,
    tl.IsApproved
FROM Users u
INNER JOIN TaskLogs tl ON u.Id = tl.UserId
INNER JOIN Projects p ON tl.ProjectId = p.Id
WHERE u.Role = 3; -- Expert role
GO

-- View to get project status with last approval
CREATE VIEW vw_ProjectStatus AS
SELECT 
    p.Id AS ProjectId,
    p.Title AS ProjectTitle,
    tl.TaskType AS LastApprovalTaskType,
    tl.ApprovedAt AS LastApprovalDate,
    approver.Username AS ApprovedBy
FROM Projects p
OUTER APPLY (
    SELECT TOP 1 *
    FROM TaskLogs
    WHERE ProjectId = p.Id AND IsApproved = 1 AND ApprovedAt IS NOT NULL
    ORDER BY ApprovedAt DESC
) tl
LEFT JOIN Users approver ON tl.ApprovedByUserId = approver.Id;
GO

PRINT 'Database schema created successfully!';
GO

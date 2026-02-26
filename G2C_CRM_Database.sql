-- ============================================
-- G2C CRM Portal — Database Creation Script
-- SQL Server | Database First Approach
-- ============================================

CREATE DATABASE G2CCRMPortal;
GO

USE G2CCRMPortal;
GO

-- ============================================
-- 1. WARD (no FK dependencies)
-- ============================================
CREATE TABLE Ward
(
    Id               INT            PRIMARY KEY,
    Name             VARCHAR(100)   NOT NULL,
    Latitude         DECIMAL(9, 6)  NOT NULL,
    Longitude        DECIMAL(9, 6)  NOT NULL,
    DepartmentEmail  VARCHAR(100)   NOT NULL
);
GO

-- ============================================
-- 2. USERS (depends on Ward)
-- ============================================
CREATE TABLE Users
(
    Id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    MobileNumber  VARCHAR(15)      NULL,
    Email         VARCHAR(100)     NULL,
    PasswordHash  VARCHAR(255)     NULL,
    Role          VARCHAR(20)      NOT NULL CHECK (Role IN ('Citizen', 'Admin', 'Officer')),
    Name          VARCHAR(100)     NOT NULL,
    WardId        INT              NULL,
    CreatedAt     DATETIME         NOT NULL DEFAULT GETUTCDATE(),
    IsActive      BIT              NOT NULL DEFAULT 1,

    CONSTRAINT FK_Users_Ward FOREIGN KEY (WardId) REFERENCES Ward(Id)
);
GO

-- ============================================
-- 3. ARTIFACT (no FK dependencies)
-- ============================================
CREATE TABLE Artifact
(
    Id              INT           PRIMARY KEY IDENTITY(1, 1),
    Name            VARCHAR(100)  NOT NULL,
    Category        VARCHAR(50)   NOT NULL,
    DefaultSLADays  INT           NOT NULL,
    IsActive        BIT           NOT NULL DEFAULT 1
);
GO

-- ============================================
-- 4. ISSUEREQUEST (depends on Artifact, Users, Ward)
-- ============================================
CREATE TABLE IssueRequest
(
    Id               UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ArtifactId       INT              NOT NULL,
    CitizenId        UNIQUEIDENTIFIER NOT NULL,
    AssignedToId     UNIQUEIDENTIFIER NULL,
    Description      TEXT             NOT NULL,
    Latitude         DECIMAL(9, 6)    NOT NULL,
    Longitude        DECIMAL(9, 6)    NOT NULL,
    LocationText     VARCHAR(255)     NULL,
    ImageUrl         VARCHAR(500)     NULL,
    Status           VARCHAR(30)      NOT NULL DEFAULT 'Submitted'
                     CHECK (Status IN ('Submitted', 'Assigned', 'InProgress', 'Resolved', 'Closed')),
    Priority         VARCHAR(10)      NOT NULL DEFAULT 'Medium'
                     CHECK (Priority IN ('Low', 'Medium', 'High')),
    SlaDeadline      DATETIME         NULL,
    IsBreached       BIT              NOT NULL DEFAULT 0,
    EscalationSentAt DATETIME         NULL,
    CreatedAt        DATETIME         NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt        DATETIME         NOT NULL DEFAULT GETUTCDATE(),
    WardId           INT              NOT NULL,

    CONSTRAINT FK_IssueRequest_Artifact   FOREIGN KEY (ArtifactId)   REFERENCES Artifact(Id),
    CONSTRAINT FK_IssueRequest_Citizen    FOREIGN KEY (CitizenId)    REFERENCES Users(Id),
    CONSTRAINT FK_IssueRequest_Officer    FOREIGN KEY (AssignedToId) REFERENCES Users(Id),
    CONSTRAINT FK_IssueRequest_Ward       FOREIGN KEY (WardId)       REFERENCES Ward(Id)
);
GO

-- ============================================
-- 5. TRACKREQUEST (depends on IssueRequest, Users)
-- ============================================
CREATE TABLE TrackRequest
(
    Id                INT              PRIMARY KEY IDENTITY(1, 1),
    IssueRequestId    UNIQUEIDENTIFIER NOT NULL,
    ChangedByUserId   UNIQUEIDENTIFIER NOT NULL,
    OldStatus         VARCHAR(30)      NOT NULL,
    NewStatus         VARCHAR(30)      NOT NULL,
    Remarks           TEXT             NULL,
    ChangedAt         DATETIME         NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_TrackRequest_Issue FOREIGN KEY (IssueRequestId)  REFERENCES IssueRequest(Id),
    CONSTRAINT FK_TrackRequest_User  FOREIGN KEY (ChangedByUserId) REFERENCES Users(Id)
);
GO

-- ============================================
-- 6. CITIZENFEEDBACK (depends on IssueRequest)
-- ============================================
CREATE TABLE CitizenFeedback
(
    Id              INT              PRIMARY KEY IDENTITY(1, 1),
    IssueRequestId  UNIQUEIDENTIFIER NOT NULL,
    Rating          INT              NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment         TEXT             NULL,
    SubmittedAt     DATETIME         NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_CitizenFeedback_Issue FOREIGN KEY (IssueRequestId) REFERENCES IssueRequest(Id)
);
GO

-- ============================================
-- INDEXES — optimise common query patterns
-- ============================================

-- Fast lookup of issues by citizen
CREATE NONCLUSTERED INDEX IX_IssueRequest_CitizenId
    ON IssueRequest(CitizenId);

-- Dashboard filtering by status and ward
CREATE NONCLUSTERED INDEX IX_IssueRequest_Status_WardId
    ON IssueRequest(Status, WardId);

-- Hangfire SLA breach job: find unbreached issues past deadline
CREATE NONCLUSTERED INDEX IX_IssueRequest_SlaDeadline
    ON IssueRequest(SlaDeadline)
    WHERE IsBreached = 0 AND Status <> 'Resolved' AND Status <> 'Closed';

-- Officer assignment lookup
CREATE NONCLUSTERED INDEX IX_IssueRequest_AssignedToId
    ON IssueRequest(AssignedToId)
    WHERE AssignedToId IS NOT NULL;

-- Audit trail per issue
CREATE NONCLUSTERED INDEX IX_TrackRequest_IssueRequestId
    ON TrackRequest(IssueRequestId);

-- One feedback per resolved issue
CREATE UNIQUE NONCLUSTERED INDEX UX_CitizenFeedback_IssueRequestId
    ON CitizenFeedback(IssueRequestId);

GO
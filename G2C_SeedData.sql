-- ============================================
-- Seed Data — Chennai Wards, Artifacts & Users
-- All user passwords: Password@123
-- ============================================

USE G2CCRMPortal;
GO

-- -----------------------------------------------
-- Step 1: Generate BCrypt hash via quick C# check
-- The hash below is for plain text: Password@123
-- If it doesn't work, re-generate using the
-- helper endpoint or the C# snippet at the bottom.
-- -----------------------------------------------

-- ============================================
-- WARDS — Chennai corporation wards
-- ============================================
INSERT INTO Ward (Id, Name, Latitude, Longitude, DepartmentEmail) VALUES
(1,  'Tiruvottiyur',   13.159230, 80.303040, 'ward1@chennaicorp.gov.in'),
(2,  'Manali',         13.166410, 80.258680, 'ward2@chennaicorp.gov.in'),
(3,  'Madhavaram',     13.148560, 80.230760, 'ward3@chennaicorp.gov.in'),
(4,  'Tondiarpet',     13.113640, 80.279350, 'ward4@chennaicorp.gov.in'),
(5,  'Royapuram',      13.107300, 80.294150, 'ward5@chennaicorp.gov.in'),
(6,  'Thiru Vi Ka Nagar', 13.119530, 80.245300, 'ward6@chennaicorp.gov.in'),
(7,  'Ambattur',       13.114250, 80.161800, 'ward7@chennaicorp.gov.in'),
(8,  'Anna Nagar',     13.085110, 80.209930, 'ward8@chennaicorp.gov.in'),
(9,  'Teynampet',      13.047260, 80.253480, 'ward9@chennaicorp.gov.in'),
(10, 'Adyar',          13.006290, 80.256090, 'ward10@chennaicorp.gov.in');
GO

-- ============================================
-- ARTIFACTS — common civic complaint types
-- ============================================
INSERT INTO Artifact (Name, Category, DefaultSLADays, IsActive) VALUES
('Pothole',              'Roads',           3, 1),
('Garbage Not Cleared',  'Sanitation',      2, 1),
('Streetlight Out',      'Electrical',      5, 1),
('Water Leakage',        'Water Supply',    2, 1),
('Sewage Overflow',      'Drainage',        1, 1),
('Illegal Encroachment', 'Encroachment',    7, 1),
('Stray Animals',        'Animal Control',  3, 1),
('Tree Fallen',          'Infrastructure',  1, 1);
GO

-- ============================================
-- USERS — dummy users (password = Password@123)
-- ============================================

-- >> STEP A: Get the hash by running this ONE TIME
--    in your app (see C# snippet below), then paste
--    the result into @hash variable.

DECLARE @hash VARCHAR(255);
-- Replace with the output of the C# snippet below:
SET @hash = '$2a$11$placeholder_replace_me_after_running_csharp';

-- Admin
INSERT INTO Users (Id, MobileNumber, Email, PasswordHash, Role, Name, WardId, CreatedAt, IsActive) VALUES
(NEWID(), '9876543210', 'admin@chennaicorp.gov.in', @hash, 'Admin', 'Priya Sharma', NULL, GETUTCDATE(), 1);

-- Officers (one per ward 1–5)
INSERT INTO Users (Id, MobileNumber, Email, PasswordHash, Role, Name, WardId, CreatedAt, IsActive) VALUES
(NEWID(), '9876543211', 'officer1@chennaicorp.gov.in', @hash, 'Officer', 'Rajesh Kumar',    1, GETUTCDATE(), 1),
(NEWID(), '9876543212', 'officer2@chennaicorp.gov.in', @hash, 'Officer', 'Meena Devi',      2, GETUTCDATE(), 1),
(NEWID(), '9876543213', 'officer3@chennaicorp.gov.in', @hash, 'Officer', 'Karthik Rajan',   3, GETUTCDATE(), 1),
(NEWID(), '9876543214', 'officer4@chennaicorp.gov.in', @hash, 'Officer', 'Lakshmi Narayanan', 4, GETUTCDATE(), 1),
(NEWID(), '9876543215', 'officer5@chennaicorp.gov.in', @hash, 'Officer', 'Suresh Babu',     5, GETUTCDATE(), 1);

-- Citizens
INSERT INTO Users (Id, MobileNumber, Email, PasswordHash, Role, Name, WardId, CreatedAt, IsActive) VALUES
(NEWID(), '9000000001', 'arjun@gmail.com',     @hash, 'Citizen', 'Arjun Venkat',   NULL, GETUTCDATE(), 1),
(NEWID(), '9000000002', 'divya@gmail.com',     @hash, 'Citizen', 'Divya Krishnan', NULL, GETUTCDATE(), 1),
(NEWID(), '9000000003', 'murugan@gmail.com',   @hash, 'Citizen', 'Murugan Selvam', NULL, GETUTCDATE(), 1);
GO

-- ============================================
-- ISSUE REQUESTS — 5 issues covering all cases
-- ============================================

DECLARE @citizen1 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Users WHERE Email = 'arjun@gmail.com');
DECLARE @citizen2 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Users WHERE Email = 'divya@gmail.com');
DECLARE @citizen3 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Users WHERE Email = 'murugan@gmail.com');

DECLARE @officer1 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Users WHERE Email = 'officer1@chennaicorp.gov.in');
DECLARE @officer2 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Users WHERE Email = 'officer2@chennaicorp.gov.in');
DECLARE @officer3 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Users WHERE Email = 'officer3@chennaicorp.gov.in');

DECLARE @pothole     INT = (SELECT TOP 1 Id FROM Artifact WHERE Name = 'Pothole');
DECLARE @garbage     INT = (SELECT TOP 1 Id FROM Artifact WHERE Name = 'Garbage Not Cleared');
DECLARE @streetlight INT = (SELECT TOP 1 Id FROM Artifact WHERE Name = 'Streetlight Out');
DECLARE @waterLeak   INT = (SELECT TOP 1 Id FROM Artifact WHERE Name = 'Water Leakage');
DECLARE @sewage      INT = (SELECT TOP 1 Id FROM Artifact WHERE Name = 'Sewage Overflow');

INSERT INTO IssueRequest
    (Id, ArtifactId, CitizenId, AssignedToId, Description, Latitude, Longitude,
     LocationText, ImageUrl, Status, Priority, SlaDeadline, IsBreached,
     EscalationSentAt, CreatedAt, UpdatedAt, WardId)
VALUES
-- 1) SUBMITTED — freshly filed, no officer
(NEWID(), @pothole, @citizen1, NULL,
 'Large pothole on Main Road near bus stop causing accidents.',
 13.159230, 80.303040, 'Main Road, Tiruvottiyur', NULL,
 'Submitted', 'High',
 DATEADD(DAY, 3, GETUTCDATE()), 0, NULL,
 GETUTCDATE(), GETUTCDATE(), 1),

-- 2) ASSIGNED — officer assigned, not yet started
(NEWID(), @garbage, @citizen2, @officer2,
 'Garbage not cleared for 3 days near Manali market entrance.',
 13.166410, 80.258680, 'Market Road, Manali', NULL,
 'Assigned', 'Medium',
 DATEADD(DAY, 2, GETUTCDATE()), 0, NULL,
 DATEADD(DAY, -1, GETUTCDATE()), GETUTCDATE(), 2),

-- 3) IN PROGRESS — officer actively working
(NEWID(), @streetlight, @citizen3, @officer3,
 'Streetlight not working for a week. Very dark at night.',
 13.148560, 80.230760, '2nd Avenue, Madhavaram', '/images/issue3.jpg',
 'InProgress', 'Medium',
 DATEADD(DAY, 2, GETUTCDATE()), 0, NULL,
 DATEADD(DAY, -3, GETUTCDATE()), GETUTCDATE(), 3),

-- 4) RESOLVED — completed, SLA met
(NEWID(), @waterLeak, @citizen1, @officer1,
 'Underground water pipe burst causing road flooding.',
 13.113640, 80.279350, '4th Street, Tondiarpet', '/images/issue4.jpg',
 'Resolved', 'High',
 DATEADD(DAY, -1, GETUTCDATE()), 0, NULL,
 DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()), 4),

-- 5) ESCALATED — SLA breached, escalation sent
(NEWID(), @sewage, @citizen2, @officer1,
 'Sewage overflowing onto road near school. Health hazard.',
 13.107300, 80.294150, 'School Road, Royapuram', '/images/issue5.jpg',
 'Escalated', 'Critical',
 DATEADD(DAY, -2, GETUTCDATE()), 1, DATEADD(DAY, -1, GETUTCDATE()),
 DATEADD(DAY, -4, GETUTCDATE()), GETUTCDATE(), 5);
GO

-- ============================================
-- Remove Duplicate Seed Data (With FK Reassignment)
-- ============================================

-- 1. Delete TrackRequest duplicates first
WITH DuplicateTracking AS (
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY IssueRequestId, ChangedByUserId, CAST(ChangedAt AS DATE) ORDER BY ChangedAt ASC) AS rn
    FROM TrackRequest
)
DELETE FROM DuplicateTracking
WHERE rn > 1;

-- 2. Delete CitizenFeedback duplicates
WITH DuplicateFeedback AS (
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY IssueRequestId, Id ORDER BY SubmittedAt ASC) AS rn
    FROM CitizenFeedback
)
DELETE FROM DuplicateFeedback
WHERE rn > 1;

-- 3. Delete IssueRequest duplicates
WITH DuplicateIssues AS (
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY CitizenId, ArtifactId, CAST(CreatedAt AS DATE) ORDER BY CreatedAt ASC) AS rn
    FROM IssueRequest
)
DELETE FROM DuplicateIssues
WHERE rn > 1;

-- 4. Reassign TrackRequest records from duplicate users to the kept user
WITH UserDuplicates AS (
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY Email ORDER BY CreatedAt ASC) AS rn,
           FIRST_VALUE(Id) OVER (PARTITION BY Email ORDER BY CreatedAt ASC) AS KeptUserId
    FROM Users
)
UPDATE TrackRequest
SET ChangedByUserId = ud.KeptUserId
FROM TrackRequest tr
INNER JOIN UserDuplicates ud ON tr.ChangedByUserId = ud.Id
WHERE ud.rn > 1;

-- 5. Reassign CitizenFeedback records from duplicate users
WITH UserDuplicates AS (
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY Email ORDER BY CreatedAt ASC) AS rn,
           FIRST_VALUE(Id) OVER (PARTITION BY Email ORDER BY CreatedAt ASC) AS KeptUserId
    FROM Users
)
UPDATE CitizenFeedback
SET UserId = ud.KeptUserId
FROM CitizenFeedback cf
INNER JOIN UserDuplicates ud ON cf.Id = ud.Id
WHERE ud.rn > 1;

-- 6. Now delete duplicate USER entries (by email, keeping the first)
WITH DuplicateUsers AS (
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY Email ORDER BY CreatedAt ASC) AS rn
    FROM Users
)
DELETE FROM DuplicateUsers
WHERE rn > 1;

-- 7. Delete duplicate ARTIFACT entries
WITH DuplicateArtifacts AS (
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY [Name] ORDER BY Id) AS rn
    FROM Artifact
)
DELETE FROM DuplicateArtifacts
WHERE rn > 1;

-- 8. Delete duplicate WARD entries
WITH DuplicateWards AS (
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY [Name] ORDER BY Id) AS rn
    FROM Ward
)
DELETE FROM DuplicateWards
WHERE rn > 1;

-- Verify results
PRINT '========== FINAL RECORD COUNTS ==========';
SELECT 'Wards' AS TableName, COUNT(*) AS Count FROM Ward
UNION ALL
SELECT 'Artifacts', COUNT(*) FROM Artifact
UNION ALL
SELECT 'Users', COUNT(*) FROM Users
UNION ALL
SELECT 'IssueRequests', COUNT(*) FROM IssueRequest
UNION ALL
SELECT 'TrackRequests', COUNT(*) FROM TrackRequest
UNION ALL
SELECT 'CitizenFeedback', COUNT(*) FROM CitizenFeedback
ORDER BY TableName;

PRINT 'Seed data inserted successfully.';
GO

SELECT * FROM IssueRequest;
SELECT * FROM Users;
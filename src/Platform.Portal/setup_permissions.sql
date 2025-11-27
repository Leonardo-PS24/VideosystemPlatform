USE VideosystemPortal;
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ApplicationPermissions')
BEGIN
    CREATE TABLE [dbo].[ApplicationPermissions](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] [nvarchar](450) NOT NULL,
        [ApplicationName] [nvarchar](100) NOT NULL,
        [CanView] [bit] NOT NULL DEFAULT 0,
        [CanCreate] [bit] NOT NULL DEFAULT 0,
        [CanEdit] [bit] NOT NULL DEFAULT 0,
        [CanDelete] [bit] NOT NULL DEFAULT 0,
        [GrantedBy] [nvarchar](450) NOT NULL,
        [GrantedAt] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [FK_ApplicationPermissions_AspNetUsers_UserId] 
            FOREIGN KEY([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ApplicationPermissions_AspNetUsers_GrantedBy] 
            FOREIGN KEY([GrantedBy]) REFERENCES [AspNetUsers] ([Id])
    );
    CREATE INDEX [IX_ApplicationPermissions_UserId] ON [ApplicationPermissions]([UserId]);
    CREATE INDEX [IX_ApplicationPermissions_ApplicationName] ON [ApplicationPermissions]([ApplicationName]);
    CREATE UNIQUE INDEX [IX_ApplicationPermissions_UserId_ApplicationName] ON [ApplicationPermissions]([UserId], [ApplicationName]);
    CREATE INDEX [IX_ApplicationPermissions_GrantedBy] ON [ApplicationPermissions]([GrantedBy]);
    PRINT 'Table created!'
END
GO

DECLARE @AdminUserId NVARCHAR(450);
SELECT TOP 1 @AdminUserId = u.Id FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.Name = 'Admin';

IF @AdminUserId IS NOT NULL
BEGIN
    DELETE FROM ApplicationPermissions WHERE UserId = @AdminUserId;
    INSERT INTO ApplicationPermissions (UserId, ApplicationName, CanView, CanCreate, CanEdit, CanDelete, GrantedBy)
    VALUES 
        (@AdminUserId, 'KioskRegistration', 1, 1, 1, 1, @AdminUserId),
        (@AdminUserId, 'BugTracking', 1, 1, 1, 1, @AdminUserId),
        (@AdminUserId, 'FeatureRequest', 1, 1, 1, 1, @AdminUserId),
        (@AdminUserId, 'DeveloperDashboard', 1, 1, 1, 1, @AdminUserId);
    PRINT 'Permissions added!'
END
GO

SELECT COUNT(*) AS Total FROM ApplicationPermissions;
SELECT u.UserName, ap.* FROM ApplicationPermissions ap
INNER JOIN AspNetUsers u ON ap.UserId = u.Id;

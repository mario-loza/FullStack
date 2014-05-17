CREATE TABLE [dbo].[Versions]
(
	[VersionsId] INT NOT NULL CONSTRAINT "pk_versions" PRIMARY KEY IDENTITY, 
    [CurrentVersion] INT NULL,
	[CreateDate] DATETIME NOT NULL CONSTRAINT "df_versions_createdate" DEFAULT (GETUTCDATE()),
	[ModifyDate] DATETIME NULL
)
GO
EXEC sys.sp_addextendedproperty @name=N'SingularName', @value=N'Version' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Versions'
GO

CREATE TABLE [dbo].[Users]
(
	[UserId] INT NOT NULL CONSTRAINT "pk_users" PRIMARY KEY IDENTITY, 
    [Alias] NVARCHAR(25) NOT NULL,
	[Email] NVARCHAR(80) NOT NULL,
	[Username] NVARCHAR(35) NOT NULL,
	[PasswordHash] VARBINARY(MAX) NULL,
	[Salt] VARCHAR(256) NULL,
	[IsAdmin] BIT NOT NULL,
	[CreateByUserId] INT NOT NULL,
	[CreateDate] DATETIME NOT NULL CONSTRAINT "df_users_createdate" DEFAULT (GETUTCDATE()),
	[ModifyByUserId] INT NULL,
	[ModifyDate] DATETIME NULL
)
GO
EXEC sys.sp_addextendedproperty @name=N'SingularName', @value=N'User' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Users'
GO

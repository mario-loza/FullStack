--RESEEDING DATABASE, CLEAR IT OUT
DELETE FROM [dbo].[Users];
GO

--CREATE SUPERUSER PASSWORD
DECLARE @PasswordSalt VARCHAR(256);
DECLARE @Password VARCHAR(40);
Declare @Hash VARBINARY(max);
Declare @CreateUserId INT;

-- CREATE A SHA1 of the password: 'password'
SET @Password = 'pasSword1';
SET @PasswordSalt = NEWID();
SET @Hash = Hashbytes('SHA1', CAST(@PasswordSalt AS VARBINARY(256)) + Cast('|' As binary(1)) + Cast(@Password As varbinary(100)));
SET @CreateUserId = 1; --Set the create user as the new super user

SET IDENTITY_INSERT [dbo].[Users] ON

-- CREATE A SYSTEM USER FOR SYSTEM ACTIONS WITH A KNOWN USERID
INSERT INTO [dbo].[Users] 
	([UserId], [Alias], [Email], [Username], [PasswordHash], [Salt], [IsAdmin], [CreateByUserId])
	VALUES
	(
		1,
		'System',
		'myname@test.com',
		'System',
		NULL,
		NULL,
		1,
		1
	)

SET @PasswordSalt = NEWID();
SET @Hash = Hashbytes('SHA1', CAST(@PasswordSalt AS VARBINARY(256)) + Cast('|' As binary(1)) + Cast(@Password As varbinary(100)));
-- CREATE THE SUPER USER WITH A KNOWN USERID
INSERT INTO [dbo].[Users] 
	([UserId], [Alias], [Email], [Username], [PasswordHash], [Salt], [IsAdmin], [CreateByUserId])
	VALUES
	(
		2,
		'Super User',
		'myname@test.com',
		'SuperUser',
		@Hash,
		@PasswordSalt,
		1,
		1
	)
GO
SET IDENTITY_INSERT [dbo].[Users] OFF

-- CREATE THE SYSTEM OBJECT TYPES

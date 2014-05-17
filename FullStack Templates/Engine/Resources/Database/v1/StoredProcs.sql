CREATE PROCEDURE sp_LoginUsernamePassword 
	 @username nvarchar(35) 
	,@password varchar(40)
AS
BEGIN
	DECLARE @passwordSalt VARCHAR(256);
	Declare @hash VARBINARY(max);
	Declare @userId INT;

	-- CREATE A SHA1 of the password: 'password'
	SELECT @passwordSalt = Salt FROM [dbo].[Users] WHERE lower(Username)=lower(@username);
	
	IF (@passwordSalt IS NOT NULL) 
	BEGIN
		SET @Hash = Hashbytes('SHA1', CAST(@passwordSalt AS VARBINARY(256)) + Cast('|' As binary(1)) + Cast(@password As varbinary(100)));

		SELECT  @userId = UserId FROM Users WHERE lower(Username)=lower(@username) AND PasswordHash=@Hash;

		IF (@userId IS NOT NULL) 
		BEGIN
			-- GOOD LOGIN, LET'S CREATE A SESSION KEY
			DECLARE @sessionKey uniqueidentifier, @sessionKeyId BIGINT;
			SET @sessionKey = NEWID();

			INSERT INTO [dbo].[SessionKeys] ([SessionKey],[UserId]) VALUES (@sessionKey, @userId);

			SET @sessionKeyId = (SELECT SCOPE_IDENTITY());
		
			SELECT Alias, Email, IsAdmin, Username, UserId, 1 AS Success, @sessionKey AS SessionKey, @sessionKeyId AS SessionKeyId FROM Users WHERE  UserId = @UserId
		END
		ELSE
		BEGIN
		    -- USER BUT BAD PASSWORD
			SELECT NULL AS Alias, NULL AS Email, NULL AS IsAdmin, Username, 0 AS Success FROM [dbo].[Users] WHERE lower(Username)=lower(@username);
		END		
	END
	ELSE 
	BEGIN
	    -- NO USER BY THE USERNAME EXISTS, RETURN AN EMPTY RECORD
		SELECT NULL AS Alias, NULL AS Email, NULL AS IsAdmin, NULL AS Username, 0 AS Success
	END
END
GO

CREATE PROCEDURE sp_CreateUser
	@alias nvarchar(25),
	@username nvarchar(35),
	@password VARCHAR(40),
	@email nvarchar(80),
	@isAdmin bit,
	@currentUserId int,
	@result int OUT
AS
DECLARE @PasswordSalt VARCHAR(256);
DECLARE @Hash VARBINARY(max);

-- CREATE A SHA1 of the password: 'password'
SET @PasswordSalt = NEWID();
SET @Hash = Hashbytes('SHA1', CAST(@PasswordSalt AS VARBINARY(256)) + Cast('|' As binary(1)) + Cast(@password As varbinary(100)));

IF ((SELECT TOP 1 UserId FROM [dbo].[Users] WHERE [Alias] LIKE @alias OR [Username] LIKE @username) IS NULL)
BEGIN
INSERT INTO [dbo].[Users] 
	([Alias], [Email], [Username], [PasswordHash], [Salt], [IsAdmin], [CreateByUserId])
	VALUES
	(
		@alias,
		@email,
		@username,
		@Hash,
		@PasswordSalt,
		@isAdmin,
		@currentUserId
	)

	SELECT @result = @@identity;
END
ELSE
BEGIN
	SELECT @result = 0;
END

GO

CREATE PROCEDURE sp_ChangePassword
	@userId int,
	@password VARCHAR(40),
	@modifyByUserId int,
	@result bit OUT
AS
DECLARE @PasswordSalt VARCHAR(256);
DECLARE @Hash VARBINARY(max);

-- CREATE A SHA1 of the password: 'password'
SET @PasswordSalt = NEWID();
SET @Hash = Hashbytes('SHA1', CAST(@PasswordSalt AS VARBINARY(256)) + Cast('|' As binary(1)) + Cast(@password As varbinary(100)));

UPDATE [dbo].[Users] 
	SET
	 [PasswordHash]=@Hash
	,[Salt]=@PasswordSalt
	,[ModifyByUserId]=@modifyByUserId
	,[ModifyDate]=GETUTCDATE()
WHERE [UserId]=@userId;

SELECT @result = CAST('TRUE' as bit);

GO
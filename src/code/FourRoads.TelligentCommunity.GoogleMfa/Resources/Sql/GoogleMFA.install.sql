/****** Object:  Table [dbo].[fr_Forum_LastRead]    Script Date: 27/06/2014 12:58:29 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MfaSession]') AND type in (N'U'))
BEGIN

CREATE TABLE [dbo].[fr_MfaSession](
	[UserId] [int] NOT NULL,
	[SessionId] [nvarchar](10) NOT NULL,
	[Valid] [bit]
 CONSTRAINT [PK_fr_MfaSession] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

END


IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MfaSession_Get]'))
	DROP PROCEDURE [dbo].[fr_MfaSession_Get]
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MfaSession_Update]'))
	DROP PROCEDURE [dbo].[fr_MfaSession_Update]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[fr_MfaSession_Get]
	@userId int
AS
BEGIN
	SELECT 	[UserId], [SessionId], [Valid] FROM [dbo].[fr_MfaSession] where UserId = @UserId
END
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[fr_MfaSession_Update]
	@userId int,
	@SessionId nvarchar(10),
	@Valid bit
AS
BEGIN
	IF EXISTS (SELECT 1 FROM [dbo].[fr_MfaSession] where UserId = @UserId)
		UPDATE  [dbo].[fr_MfaSession] SET SessionId = @SessionId , Valid = @Valid where  UserId = @UserId
	ELSE
		INSERT INTO [dbo].[fr_MfaSession]  (UserId, SessionID, Valid) VALUES (@UserId, @SessionId , @Valid)
END
GO


IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MfaSession_Update]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[fr_MfaSession_Update]
GO

CREATE PROCEDURE [dbo].[fr_MfaSession_Update]
	@userId int,
	@SessionId nvarchar(10),
	@Valid bit
AS
BEGIN
	IF EXISTS (SELECT 1 FROM [dbo].[fr_MfaSession] where UserId = @UserId)
		UPDATE  [dbo].[fr_MfaSession] SET SessionId = @SessionId , Valid = @Valid where  UserId = @UserId
	ELSE
		INSERT INTO [dbo].[fr_MfaSession]  (UserId, SessionID, Valid) VALUES (@UserId, @SessionId , @Valid)
END
GO

/** USER KEY TABLE**/
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MfaKeys]') AND type in (N'U'))
BEGIN
	CREATE TABLE [dbo].[fr_MfaKeys](
		[UserId] [int] NOT NULL,
		[UserKey] [uniqueidentifier] NOT NULL,
	 CONSTRAINT [PK_fr_MfaKeys] PRIMARY KEY CLUSTERED 
	(
		[UserId] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MfaKeys_Update]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[fr_MfaKeys_Update]
GO

CREATE PROCEDURE [dbo].[fr_MfaKeys_Update]
	@userId int,
	@userKey uniqueidentifier
AS
BEGIN
	IF EXISTS (SELECT 1 FROM [dbo].[fr_MfaKeys] where UserId = @UserId)
		UPDATE  [dbo].[fr_MfaKeys] SET UserKey = @userKey where  UserId = @userId
	ELSE
		INSERT INTO [dbo].[fr_MfaKeys]  ( UserId, UserKey ) VALUES ( @userId, @userKey )
END
GO


IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MfaKeys_Get]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[fr_MfaKeys_Get]
GO

CREATE PROCEDURE [dbo].[fr_MfaKeys_Get]
	@userId int
AS
BEGIN
	SELECT UserKey FROM [dbo].[fr_MfaKeys] where UserId = @UserId
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MfaKeys_Clear]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[fr_MfaKeys_Clear]
GO

CREATE PROCEDURE [dbo].[fr_MfaKeys_Clear]
	@userId int
AS
BEGIN
	DELETE FROM [dbo].[fr_MfaKeys] where UserId = @UserId
END
GO

/**ONE TIME CODES **/


IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MfaOTCodes]') AND type in (N'U'))
BEGIN

	CREATE TABLE [dbo].[fr_MfaOTCodes](
		[Id] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
		[UserId] [int] NOT NULL,
		[Code] [char](50) NOT NULL,
		[GeneratedOnUtc] [datetime] NOT NULL,
		[RedeemedOnUtc] [datetime] NULL,
	 CONSTRAINT [PK_fr_MfaOTCodes] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[fr_MfaOTCodes] ADD  CONSTRAINT [DF_fr_MfaOTCodes_RedeemedOnUtc]  DEFAULT (NULL) FOR [RedeemedOnUtc]

END
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MfaOTCodes_RemoveAll]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[fr_MfaOTCodes_RemoveAll]
GO

CREATE PROCEDURE [dbo].[fr_MfaOTCodes_RemoveAll]
	@userId int
AS
BEGIN
	DELETE FROM [dbo].[fr_MfaOTCodes] WHERE UserId = @userId
END
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MfaOTCodes_Create]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[fr_MfaOTCodes_Create]
GO

CREATE PROCEDURE [dbo].[fr_MfaOTCodes_Create]
	@id uniqueidentifier,
	@userId int,
	@code char(50),
	@generatedOnUtc datetime
AS
BEGIN
	SET NOCOUNT ON;  
	INSERT INTO [dbo].[fr_MfaOTCodes] (Id, UserId, Code, GeneratedOnUtc ) VALUES ( @id, @userId, @code, @generatedOnUtc )
END
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MfaOTCodes_VerifyUnused]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[fr_MfaOTCodes_VerifyUnused]
GO

CREATE PROCEDURE [dbo].[fr_MfaOTCodes_VerifyUnused]
	@userId int,
	@code char(50)
AS
BEGIN
	SELECT Id FROM [dbo].[fr_MfaOTCodes] WHERE UserId = @userId AND Code = @code AND RedeemedOnUtc IS NULL
END
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MfaOTCodes_Redeem]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[fr_MfaOTCodes_Redeem]
GO

CREATE PROCEDURE [dbo].[fr_MfaOTCodes_Redeem]
	@codeId uniqueidentifier,
	@redeemedAtUtc datetime
AS
BEGIN
	UPDATE [dbo].[fr_MfaOTCodes] SET RedeemedOnUtc = @redeemedAtUtc WHERE Id = @codeId 
END
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MfaOTCodes_CountUsableCodes]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[fr_MfaOTCodes_CountUsableCodes]
GO

CREATE PROCEDURE [dbo].[fr_MfaOTCodes_CountUsableCodes]
	@userId int
AS
BEGIN
	SELECT COUNT(*) FROM [dbo].[fr_MfaOTCodes] WHERE UserId = @userId AND RedeemedOnUtc IS NULL
END
GO

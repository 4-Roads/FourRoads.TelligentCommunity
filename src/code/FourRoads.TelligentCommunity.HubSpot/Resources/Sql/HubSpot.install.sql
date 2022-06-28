SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/** USER KEY TABLE**/
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_HubSpotAuth]') AND type in (N'U'))
BEGIN
	CREATE TABLE [dbo].[fr_HubSpotAuth](
		[ClientId] [uniqueidentifier] NOT NULL,
		[AccessToken] [nvarchar](255) NOT NULL,
		[RefreshToken] [nvarchar](255) NOT NULL,
		[ExpiryUtc] [datetime] NOT NULL
	 CONSTRAINT [PK_fr_HubSpotAuth] PRIMARY KEY CLUSTERED 
	(
		[ClientId] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_HubSpotAuth_Update]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[fr_HubSpotAuth_Update]
GO

CREATE PROCEDURE [dbo].[fr_HubSpotAuth_Update]
	@ClientId [uniqueidentifier],
    @AccessToken [nvarchar](255),
	@RefreshToken [nvarchar](255),
	@ExpiryUtc [datetime]
AS
BEGIN
	IF EXISTS (SELECT 1 FROM [dbo].[fr_HubSpotAuth] where ClientId = @ClientId)
		UPDATE  [dbo].[fr_HubSpotAuth] SET AccessToken = @AccessToken, RefreshToken = @RefreshToken, ExpiryUtc = @ExpiryUtc where  ClientId = @ClientId
	ELSE
		INSERT INTO [dbo].[fr_HubSpotAuth]  (ClientId, AccessToken, RefreshToken, ExpiryUtc ) VALUES ( @ClientId, @AccessToken, @RefreshToken, @ExpiryUtc )
END
GO


IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_HubSpotAuth_Get]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[fr_HubSpotAuth_Get]
GO

CREATE PROCEDURE [dbo].[fr_HubSpotAuth_Get]
    @ClientId [uniqueidentifier]
AS
BEGIN
	SELECT AccessToken, RefreshToken, ExpiryUtc  FROM [dbo].[fr_HubSpotAuth] where ClientId = @ClientId
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_HubSpotAuth_Clear]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[fr_HubSpotAuth_Clear]
GO

CREATE PROCEDURE [dbo].[fr_HubSpotAuth_Clear]
	@ClientId [uniqueidentifier]
AS
BEGIN
	DELETE FROM [dbo].[fr_HubSpotAuth] where ClientId = @ClientId
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

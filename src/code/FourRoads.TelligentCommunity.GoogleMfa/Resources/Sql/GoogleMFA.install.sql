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


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_PwaTokens]') AND type in (N'U'))
BEGIN

CREATE TABLE [dbo].[fr_PwaTokens](
	[id] [int] PRIMARY KEY IDENTITY(1,1),
	[UserId] [int] NOT NULL,
	[Token] [nvarchar](4000) NOT NULL
) ON [PRIMARY]

	CREATE CLUSTERED INDEX IX_fr_PwaToken ON dbo.fr_PwaTokens
	(
	[Token]
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

	CREATE NONCLUSTERED INDEX IX_fr_PwaUserId ON dbo.fr_PwaTokens
	(
	[UserId]
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

END


IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_PwaSession_StoreToken]'))
	DROP PROCEDURE [dbo].[fr_PwaSession_StoreToken]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[fr_PwaSession_StoreToken]
	@userId int,
	@token nvarchar(4000)
AS
BEGIN
	 DELETE FROM fr_PwaTokens WHERE Token = @token

	 INSERT INTO fr_PwaTokens (UserId , Token) VALUES (@userId , @token)
END
GO


IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_PwaSession_RevokeToken]'))
	DROP PROCEDURE [dbo].[fr_PwaSession_RevokeToken]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[fr_PwaSession_RevokeToken]
	@userId int,
	@token nvarchar(4000)
AS
BEGIN
	 DELETE FROM fr_PwaTokens WHERE Token = @token AND UserID = @userId
END
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_PwaSession_ListTokens]'))
	DROP PROCEDURE [dbo].[fr_PwaSession_ListTokens]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[fr_PwaSession_ListTokens]
	@userId int
AS
BEGIN
	 SELECT Token FROM fr_PwaTokens WHERE UserID = @userId
END
GO



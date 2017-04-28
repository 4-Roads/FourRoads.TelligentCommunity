IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_Forum_ThreadView_Create]'))
	DROP PROCEDURE [dbo].[fr_Forum_ThreadView_Create]
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_Forum_ThreadView_NewList]'))
	DROP PROCEDURE [dbo].[fr_Forum_ThreadView_NewList]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE fr_Forum_ThreadView_Create
	@ApplicationId uniqueidentifier,
	@ContentId uniqueidentifier,
	@UserId int,
	@ViewDate datetime,
	@Status int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

		INSERT INTO dbo.[fr_Forum_ThreadView] (	[ApplicationId],[ContentId],[UserId],[ViewDate],[CreatedDate],[Status]) 
			VALUES (@ApplicationId , @ContentId , @UserId , @ViewDate, GETUTCDATE() ,@Status)

END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE fr_Forum_ThreadView_NewList
	@Threshold int 
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [ApplicationId], [ContentId] FROM dbo.[fr_Forum_ThreadView] WHERE Status = 1 GROUP BY [ApplicationId],[ContentId]

	UPDATE dbo.[fr_Forum_ThreadView] SET Status = 2 WHERE Status = 1 AND [CreatedDate] < DATEADD("MINUTE",(0-@Threshold), GETUTCDATE());

	DELETE FROM dbo.[fr_Forum_ThreadView] WHERE Status = 2 AND [CreatedDate] < DATEADD("DAY",-7, GETUTCDATE());

END
GO




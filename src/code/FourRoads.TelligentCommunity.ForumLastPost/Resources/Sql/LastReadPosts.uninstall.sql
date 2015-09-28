
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_LastReadPost_LastReadPost]'))
	DROP PROCEDURE [dbo].[fr_LastReadPost_LastReadPost]
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_LastReadPost_Update]'))
	DROP PROCEDURE [dbo].[fr_LastReadPost_Update]
GO
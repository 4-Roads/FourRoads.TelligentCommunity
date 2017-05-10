IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_Forum_ThreadView_Create]'))
	DROP PROCEDURE [dbo].[fr_Forum_ThreadView_Create]
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_Forum_ThreadView_NewList]'))
	DROP PROCEDURE [dbo].[fr_Forum_ThreadView_NewList]
GO

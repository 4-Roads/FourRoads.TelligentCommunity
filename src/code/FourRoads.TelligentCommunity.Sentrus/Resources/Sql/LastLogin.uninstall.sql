
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_LastLogin_CreateUpdate]'))
	DROP PROCEDURE [dbo].[fr_LastLogin_CreateUpdate]
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_LastLogin_Get]'))
	DROP PROCEDURE [dbo].[fr_LastLogin_Get]
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_LastLogin_List]'))
	DROP PROCEDURE [dbo].[fr_LastLogin_List]
GO


IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_LastLogin_CreateUpdate]'))
	DROP PROCEDURE [dbo].[fr_LastLogin_CreateUpdate]
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_LastLogin_Get]'))
	DROP PROCEDURE [dbo].[fr_LastLogin_Get]
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_LastLogin_List]'))
	DROP PROCEDURE [dbo].[fr_LastLogin_List]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[fr_LastLogin_CreateUpdate]
	@MembershipId uniqueidentifier,
	@LastLogonDate datetime,
	@EmailCountSent int,
	@FirstEmailSentAt datetime,
	@ignoredUser bit
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	IF EXISTS (SELECT 1 FROM fr_User_LastLogin WHERE  @MembershipId = MembershipId)
		UPDATE dbo.fr_User_LastLogin SET [LastLogonDate] = @LastLogonDate ,  EmailCountSent = @EmailCountSent , FirstEmailSentAt = @FirstEmailSentAt, IgnoredUser = @ignoredUser WHERE  @MembershipId = MembershipId
	ELSE
		INSERT INTO dbo.fr_User_LastLogin (	MembershipId,[LastLogonDate],EmailCountSent,FirstEmailSentAt,IgnoredUser) VALUES (@MembershipId , @LastLogonDate , @EmailCountSent, @FirstEmailSentAt , @ignoredUser)

END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[fr_LastLogin_Get]
	@MembershipId uniqueidentifier
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT MembershipId,[LastLogonDate],EmailCountSent,FirstEmailSentAt,IgnoredUser  FROM dbo.fr_User_LastLogin WHERE @MembershipId = MembershipId

END
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[fr_LastLogin_List]
	@LastLogonDate datetime,
	@excludeIgnored bit
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	IF @excludeIgnored is null OR @excludeIgnored = 0
		SELECT MembershipId  FROM dbo.fr_User_LastLogin WHERE @LastLogonDate > LastLogonDate ORDER BY LastLogonDate
	ELSE
		SELECT MembershipId  FROM dbo.fr_User_LastLogin WHERE @LastLogonDate > LastLogonDate AND IgnoredUser = 0 ORDER BY LastLogonDate
END
GO


GRANT EXECUTE ON  [dbo].[fr_LastLogin_List] TO [public]
GO
GRANT EXECUTE ON  [dbo].[fr_LastLogin_Get] TO [public]
GO
GRANT EXECUTE ON  [dbo].[fr_LastLogin_CreateUpdate] TO [public]
GO

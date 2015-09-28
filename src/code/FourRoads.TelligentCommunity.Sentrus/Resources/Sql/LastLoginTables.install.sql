/****** Object:  Table [dbo].[fr_Forum_LastRead]    Script Date: 27/06/2014 12:58:29 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_User_LastLogin]') AND type in (N'U'))
BEGIN

CREATE TABLE [dbo].[fr_User_LastLogin](
	[MembershipId] [uniqueidentifier] NOT NULL,
	[LastLogonDate] [datetime] NOT NULL,
	[EmailCountSent] [int],
	[FirstEmailSentAt] [datetime],
	[IgnoredUser] bit
 CONSTRAINT [PK_fr_User_LastLogin] PRIMARY KEY CLUSTERED 
(
	[MembershipId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

INSERT INTO fr_User_LastLogin ([MembershipId] , [LastLogonDate] , [EmailCountSent] , [FirstEmailSentAt] , [IgnoredUser]) 
	SELECT [UserId] , [LastLoginDate] , 0 , null , 0 FROM aspnet_Membership

END

#Database Sanitisation Utility

##Introduction
This is an console application which main purpose is to rename user's names and email addresses. The utility also disables e-mail and wipes e-mail configuration values, so no third party SMTP server credentials is leaked.

Every user gets a new name in form of user_XXXX and E-mail of the user becomes "user_XXXX@localhost.local", where XXXX is UserID value. User profile fields are reset to empty/default values. Passwords remain unchanged. 

Please note, the following user Ids are not sanitised:

	- 2100
	- 2101
	- 2102

##How to use

1) Edit the connectionStrings.config file so that it points to the **copy of the database** you want to sanitise. 
	
	It's always a good idea to make sure you are sanitising a copy 
	of the database and not the original one.

2) Run the utility's exe file.

3) Review log.txt file for any errors or informational messages.

**Important:** If you are going to share the backup of sanitised database with someone else, create a backup using 'copy-only' option! Otherwise the backup may include backups taken earlier, therefore containing unsanitised data.

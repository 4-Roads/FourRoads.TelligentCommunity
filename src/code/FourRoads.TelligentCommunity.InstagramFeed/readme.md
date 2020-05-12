## Initial version: 
* Uses facebook client SDK to connect to facebook page.
* User access token must be exchanged for Permanent Access Token: https://developers.facebook.com/docs/marketing-api/access#get-access-tokens
* Widget currently only supports search by single hashtag.

# How to set up and use 

## [Pre-requisites](https://developers.facebook.com/docs/instagram-api/getting-started):
 1. An Instagram Business Account or Instagram Creator Account 
 2. A Facebook Page connected to that account 
 3. A Facebook Developer account that can perform Tasks on that Page 
 4. A registered Facebook App with Basic settings configured

## Facebook Developer:
1.	Create a new Facebook app and Implement Facebook Login, or using an existing app..
2.	Make sure the following permissions are set: `instagram_basic` and `pages_show_list` permission
3.	Add your website’s URL in Valid OAuth Redirect URLs, **make sure it is using https**.

## Telligent
1.	Once installed go to Admin Panel > Extensions, look for `4 Roads – Instagram Feed` and enable it.
3.	On the same page, check the Settings tab and populate `App Id` with your Facebook App Id to start using the Instagram Connect widget. 
4.	Make sure to install the widgets if it's not done automatically.
5.	Navigate to `{homeurl}/instagram-feed-setup`, if the App Id’s set up correctly, you should now see a Facebook login button.
6.	Log on to your Facebook account, and follow the wizard. Make sure the account you are logging on is connected to your Instagram account:
7.	If successful, the login button will now be replaced with the page id and page name you linked to.
 
## Graph API Explorer
1.	Go to [Facebook Graph API Explorer](https://developers.facebook.com/tools/explorer/). On the right panel, select your Facebook App and Page. 
2.	Under Access Token, click on `i` icon and then `Open in Access Token Tool` button
 

## Access Token Debugger
1.	Watch out for your token expiry, it should be non-expiring. If not, follow the process [here](https://developers.facebook.com/docs/marketing-api/access#get-access-tokens) to get a permanent token.
2.	Copy the page id and Permanent Access Token into `4 Roads - Instagram Hashtag Search` widget in Telligent. 
 
After setting up page id, access token, and specifying a hashtag, all the public media should now load.
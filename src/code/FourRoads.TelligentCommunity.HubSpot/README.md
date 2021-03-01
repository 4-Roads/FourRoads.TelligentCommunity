    Auth Process
    
    Requirements
    ============
    -Hubspot developers account:
        visit https://developers.hubspot.com/ and create an account
    -An application in hubspot with the "Read from and write to my Contacts" and "Basic OAuth functionality" scopes defined
    -The client id and secret id from the above application
    -Telligent application hosted with a https secured endpoint
    
    Setup 
    =====
    If in dev create an entry in your system32/drivers/etc/hosts file for your 'test' domain
        127.0.0.1 www.devurl.com
        127.0.0.1 devurl.com

    Configure iis to host your telligent application and add https bindings for your domain so for example www.devurl.com and devurl.com
    Edit your telligent application connectionStrings.config make sure SiteUrl is set to a https url (e.g. https://www.devurl.com)
    You may also need to configure your database access depending on the user you configure in iis

    Run the site and via the administration page enable the following: 
		"4 Roads - Hubspot Core" plugin
		"Hubspot - Authorize Property Template"
		"Hubspot - Trigger Action Property Template"
    In the plugin configuration, enter the hubspot client id and secret id and save the configuration
    
    oAuth Overview
    ==============
    
    Conigure the Auth settings for the hubspot application
        -Make sure the "Read from and write to my Contacts" and "Basic OAuth functionality" scopes are defined
        -Enter the telligent site url as the redirect url: e.g. https://www.devurl.com/
        
    You can then visit the install url in a browser, this takes the format below:
        https://app.hubspot.com/oauth/authorize?client_id=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx&redirect_uri=https://www.devurl.com/&scope=contacts%20oauth
    This will prompt the user to allow access to your application and upon successfull auth will call back to the redirect_uri passing a code.
    You must ensure this redirect_uri exactly matches your hosting and is secured using https.
  
    You will receive a call back to your url with a code as below (use this in the plugin config panel)
    https://www.devurl.com/?code=yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy

    In the configuration panel enter this code and click on the auth button.

    The stored values for AccessToken and RefreshToken are hidden in the plugin configuration.
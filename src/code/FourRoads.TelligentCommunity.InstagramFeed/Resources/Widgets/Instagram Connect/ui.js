(function ($, global) {
	if (typeof $.fourroads === 'undefined')
		$.fourroads = {};

	if (typeof $.fourroads.widgets === 'undefined')
		$.fourroads.widgets = {};

	var statusSelector;
	var loginSelector;
	var pagesSelector;
	var pleaseLogInMessage;
	var loggedInMessage;
	
	function statusChangeCallback(response) {
		console.log(response);
		if (response.status === 'connected') {
			testAPI();
			$(loginSelector).hide();
		} else {
			$(loginSelector).show();
			$(pagesSelector).hide();
			$(statusSelector).html(pleaseLogInMessage);
		}
	}

	function testAPI() { // Testing Graph API after login.  See statusChangeCallback() for when this call is made.
		console.log('Welcome!  Fetching your information.... ');
		FB.api('/me', function (response) {
			console.log('Successful login for: ' + response.name);
			$(statusSelector).html(loggedInMessage + response.name + '!');

			FB.api('/me/accounts', function (response) {
				var pages = response.data;

				if (pages.length > 0) {
					$(pagesSelector).show();
					$.each(pages, function (i, v) {
						var row = "<tr><td>" + v.id + "</td><td>" + v.name + "</td>/tr>";
						$(pagesSelector).find("table > tbody").append(row);
					});
				}
			});
		});
	}
	$.fourroads.widgets.instagramFeed = {
		register: function (context) {

			statusSelector = context.selectors.status;
			loginSelector = context.selectors.loginDisplay;
			pagesSelector = context.selectors.pages;
			pleaseLogInMessage = context.resources.pleaseLogIn;
			loggedInMessage = context.resources.loggedIn;
			
			if (!context.appId) {
				$(statusSelector).html(context.resources.missingAppId);
				$(context.selectors.loginDisplay).hide();
			}
			else {
				window.fbAsyncInit = function () {
					FB.init({
						appId: context.appId,
						cookie: true, // Enable cookies to allow the server to access the session.
						xfbml: true, // Parse social plugins on this webpage.
						version: 'v7.0' // Use this Graph API version for this call.
					});

					FB.getLoginStatus(function (response) {
						statusChangeCallback(response);
					});
				};
			}
		},
		checkLoginState: function () {              // Called when a person is finished with the Login Button.
			FB.getLoginStatus(function (response) {   // See the onlogin handler
				statusChangeCallback(response);
			});
		}
	};
})(jQuery, window);

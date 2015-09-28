(function ($) {
    if (typeof $.fourroads == 'undefined') {
        $.fourroads = {};
    }
    if (typeof $.fourroads.widgets == 'undefined') {
        $.fourroads.widgets = {};
    }

    var scrapeElements = function (context) {
        $.each([context.selectors], function (i, set) {
            $.each(set, function (key, value) {
                set[key] = $(value);
            });
        });
    };

    $.fourroads.widgets.subscriptionDefaults =
	{
	    register: function (context) {
	        scrapeElements(context);

	        context.selectors.defaultSettingsRadio.change(function (e) {
	            e.preventDefault();
	            var setting = this.value;
	            var forumId = context.forumId;

	            $.telligent.evolution.post({
	                url: context.urls.updateSettings,
	                data: {
	                    setting: setting,
	                    forumId: forumId
	                },
	                success: function (response) {
	                    $.telligent.evolution.notifications.show(context.resources.updatedMessage, { type: 'success' });
	                }
	            });
	        });

	        context.selectors.resetSiteButton.click(function (e) {
	            e.preventDefault();
	            var forumId = context.forumId;

	            $.telligent.evolution.post({
	                url: context.urls.resetSettings,
	                data: {
	                    forumId: forumId
	                },
	                success: function (response) {
	                    $.telligent.evolution.notifications.show(context.resources.updatedMessage, { type: 'success' });
	                }
	            });
	        });
	    }
	}

})(jQuery);
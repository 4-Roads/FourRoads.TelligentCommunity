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
	            var blogId = context.blogId;

	            $.telligent.evolution.post({
	                url: context.urls.updateSettings,
	                data: {
	                    setting: setting,
	                    blogId: blogId
	                },
	                success: function (response) {
	                    $.telligent.evolution.notifications.show(context.resources.updatedMessage, { type: 'success' });
	                }
	            });
	        });

	        context.selectors.resetSiteButton.on('click', function (e) {
	            e.preventDefault();
	            var blogId = context.blogId;
	            var groupId = context.groupId;

	            $.telligent.evolution.post({
	                url: context.urls.resetSettings,
	                data: {
	                    blogId: blogId,
	                    groupId: groupId
	                },
	                success: function (response) {
	                    $.telligent.evolution.notifications.show(context.resources.updatedMessage, { type: 'success' });
	                }
	            });
	        });
	    }
	}

})(jQuery);
(function($) {

    if (typeof $.fourroads == 'undefined') { $.fourroads = {}; }
    if (typeof $.fourroads.widgets == 'undefined') { $.fourroads.widgets = {}; }

    var scrapeElements = function (context) {
        $.each([context.elements], function (i, set) {
            $.each(set, function (key, value) {
                set[key] = context.wrapper.find(value);
            });
        });
    };

    $.fourroads.widgets.userNotifications = {
        register: function (context) {
            context.wrapper = $(context.wrapper);
            scrapeElements(context);

            context.elements.applyToAllusers.on('click', function (e) {
                e.preventDefault();
                var target = $(e.target);
                if (!target.hasClass('disabled')) {
                    target.addClass('disabled');
                    target.closest('td').find('.in-progress').show();
                    var notificationType = target.data('notificationtype');
                    var checked = $("input[data-notificationType='" + notificationType + '');
                    var distributionTypes = [];
                    $.each(checked, function (index, element) {
                        var box = $(element);
                        if (box.is(':checked')) {
                            distributionTypes.push($(element).data('distributiontype'));
                        }
                    });
                    var data = {
                        notificationType: notificationType,
                        distributionTypes: distributionTypes.toString()
                    }
                    $.telligent.evolution.post({
                        url: context.urls.reset,
                        data: data,
                        success: function (response) {
                            target.removeClass('disabled');
                            target.closest('td').find('.in-progress').hide();
                            $.telligent.evolution.notifications.show(context.resources.userUpdateQueued, { type: 'success' });
                        }
                    })
                }
            });

            context.elements.setAsDefault.on('click', function (e) {
                e.preventDefault();
                if (!context.elements.setAsDefault.hasClass('disabled')) {
                    context.elements.setAsDefault.addClass('disabled');
                    $('.working').show();
                    var checkboxes = context.wrapper.find('[type=checkbox]');
                    $.each(checkboxes, function (index, element) {
                        var box = $(element);
                        var checked = box.is(':checked');
                        var wasEnabled = box.data('enabled')
                        if ((checked && wasEnabled == 'False') || (!checked && wasEnabled == 'True')) {
                            var data = {
                                notificationType: box.data('notificationtype'),
                                distributionType: box.data('distributiontype'),
                                enable: checked
                            }
                            $.telligent.evolution.post({
                                async: false,
                                url: context.urls.updateDefaults,
                                data: data,
                                success: function (response) {
                                }
                            });
                        }
                    });
                    $('.working').hide();
                    $.telligent.evolution.notifications.show(context.resources.defaultsUpdated, { type: 'success' });
                    context.elements.setAsDefault.removeClass('disabled');
                }
            })

        }
    };


})(jQuery);
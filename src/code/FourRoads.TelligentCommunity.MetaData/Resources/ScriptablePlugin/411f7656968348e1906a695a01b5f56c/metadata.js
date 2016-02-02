(function($, global)
{
    if (typeof $.fourroads === 'undefined')
        $.fourroads = {};

    if (typeof $.fourroads.widgets === 'undefined')
        $.fourroads.widgets = {};

    var attachUpdateHandlers = function(context) {
        context.selectors.save.click(function (e) {
            e.preventDefault();

            var data = context.getData();
      
            $.telligent.evolution.post({
                url: context.urls.saveDataCallback,
                data: data,
                success: function(d) {
                    if (d.warnings && d.warnings.length > 0) {
                        window.parent.jQuery.glowModal.opener(window).jQuery.telligent.evolution.notifications.show(d.warnings[0], {type: 'warning', duration: 5000});
                    } else if (d.message)  {
                        window.parent.jQuery.glowModal.opener(window).jQuery.telligent.evolution.notifications.show(d.message, {type: 'success', duration: 5000});
                    }
                    window.parent.jQuery.glowModal.close(window);
                }	
             });
        });

    },
    scrapeElements = function (context) {
        $.each([context.selectors], function(i, set) {
            $.each(set, function(key, value) {
                set[key] = $(value);
            });
        });
    };

    $.fourroads.widgets.metaDataUpdate = {
        register: function (context) {
            scrapeElements(context);

            attachUpdateHandlers(context);
        }
    };

})(jQuery, window);

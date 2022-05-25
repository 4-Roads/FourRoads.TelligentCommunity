(function ($, global) {
    if (typeof $.fourroads === 'undefined')
        $.fourroads = {};

    if (typeof $.fourroads.widgets === 'undefined')
        $.fourroads.widgets = {};

        var attachHandlers = function (context) {
                context.selectors.editContent.on('click', function(e) {

                    e.preventDefault();

                    var modal = $.glowModal(context.urls.editContent + '&inlineContentName=' + context.inlineContentName,
                        {width:600});  

                   

                });
            },
        scrapeElements = function (context) {
            $.each([context.selectors], function (i, set) {
                $.each(set, function (key, value) {
                    set[key] = $(value);
                });
            });
        }

        $.fourroads.widgets.inlineContent = {
        register: function (context) {
            scrapeElements(context);
            attachHandlers(context);
        }
    };
})(jQuery, window);

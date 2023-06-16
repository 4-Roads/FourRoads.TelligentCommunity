(function ($, global, undef) {

    var api = {
        register: function (context) {
            var $wrapper = $(context.wrapperSelector);


            $.telligent.evolution.messaging.subscribe('paywall.displayPopup', function (data) {

                window.setTimeout(function () {
                    $wrapper.show();
                }, context.displayDelay * 1000);

            });

            $.telligent.evolution.messaging.publish('paywall.ready', {});

        }
    };

    $.fourroads = $.fourroads || {};
    $.fourroads.widgets = $.fourroads.widgets || {};
    $.fourroads.widgets.paywall = api;

})(jQuery, window);
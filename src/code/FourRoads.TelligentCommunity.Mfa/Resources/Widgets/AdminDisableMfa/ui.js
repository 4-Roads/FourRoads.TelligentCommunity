(function($, global, undef){

    if (!$.ns) { $.ns = {}; }
    if (!$.ns.widgets) { $.ns.widgets = {}; }
    var ev = $.telligent.evolution;
    
    var parent = $('.admin-disable-mfa');
    
    $.fn.showLoader = function (visible, restore) {
        if(visible) {
            this.data('loader-content', this.html()).html('<span class="ui-loading"></span>');
            ev.ui.components.loading.add($('.ui-loading', this), {
                width: 45,
                height: 10
            });
        } else {
            this.html(restore || this.data('loader-content'));
        }
        return this.prop('disabled', visible);
    };
    
    $.ns.widgets.disableMFA = {
        register: function (context) {
            // The add list button
            $('.action .disable-mfa', parent).on('click', function (e) {
                e.preventDefault();
                var button = $(this);
                button.showLoader(true);
                ev.post({
                    url: context.disableMfaUrl,
                    data: { userId: context.userId },
                    success: function (r) {
                        button.showLoader(false);
                        button.hide();
                        button.parent('.navigation-list-item').hide();
                        ev.notifications.show(context.disableMfaSuccessMsg, { type: 'information' });
                    },
                    error: function (r) {
                        button.showLoader(false);
                        ev.notifications.show(r.message, { type: 'error' });
                    }
                });
            });

            $('.action .validate-email', parent).on('click', function (e) {
                e.preventDefault();
                var button = $(this);
                button.showLoader(true);
                ev.post({
                    url: context.validateEmaolUrl,
                    data: { userId: context.userId },
                    success: function (r) {
                        button.showLoader(false);
                        button.hide();
                        button.parent('.navigation-list-item').hide();
                        ev.notifications.show(context.validateEmailSuccessMsg, { type: 'information' });
                    },
                    error: function (r) {
                        button.showLoader(false);
                        ev.notifications.show(r.message, { type: 'error' });
                    }
                });
            });
        }
    };
    
})($, window);
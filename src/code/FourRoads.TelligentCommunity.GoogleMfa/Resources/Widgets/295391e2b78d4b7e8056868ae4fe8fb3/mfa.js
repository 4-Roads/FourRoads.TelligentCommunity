(function($, global)
{
    if (typeof $.fourroads === 'undefined')
        $.fourroads = {};

    if (typeof $.fourroads.widgets === 'undefined')
        $.fourroads.widgets = {};

    var attachHandlers = function (context) {
            context.selectors.submit.click(function(){save(context, context.selectors.validateInput.val());});
        },
        scrapeElements = function (context) {
            $.each([context.selectors], function(i, set) {
                $.each(set, function(key, value) {
                    set[key] = $(value);
                });
            });
        },
        save = function(context , code) {
            var data = {
                validationCode: code
            };

            context.selectors.submit.closest('.field-item').find('.field-item-validation').hide();

            return $.telligent.evolution.post({
                url: context.urls.validate,
                data: data,
                dataType: 'json',
                success: function(response) {
                    if (response.success === "true") {
                        window.location = context.returnUrl;
                    } else {
                        //Show error message
                        context.selectors.submit.closest('.field-item').find('.field-item-validation').show();
                    }

                }
            });
        };

    $.fourroads.widgets.mfa = {
        register: function(context) {
            scrapeElements(context);

            attachHandlers(context);
        }
    };
})(jQuery, window);

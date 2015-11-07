(function($, global)
{
    if (typeof $.fourroads === 'undefined')
        $.fourroads = {};

    if (typeof $.fourroads.widgets === 'undefined')
        $.fourroads.widgets = {};

    var attachHandlers = function (context) {

            context.selectors.termsLink.click(function(e) {
                context.selectors.termsLink.parent().find('.terms-container').slideDown();
            });

            context.selectors.email.blur(function (e) {
                //Test to see if this is a valid beta user
                $.telligent.evolution.post({
                    url: context.urls.testAccess,
                    data: { email: context.selectors.email.val() },
                    dataType: 'json',
                    success: function(response) {
                        if (response.result == "true") {
                            context.selectors.accessCode.closest('li').show();
                        } else {
                            context.selectors.accessCode.closest('li').hide();
                        }
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
    },
    addValidation = function(context) {
        var saveButton = context.selectors.submit;

        saveButton.evolutionValidation({
            onValidated: function(isValid, buttonClicked, c) {
                    if (isValid)
                        saveButton.removeClass('disabled');
                    else {
                        saveButton.addClass('disabled');
                    }
                },
                onSuccessfulClick: function(e) {
                    $('.processing', saveButton.parent()).css("visibility", "visible");
                    saveButton.addClass('disabled');
                    save(context);
                }
            });

            saveButton.evolutionValidation('addField',
                context.selectors.email,
                {
                    required: true,
                    email: true,
                    messages: {
                        required: context.resources.requiredField,
                        email: context.resources.validEmail
                    }
                },
                context.selectors.email.closest('.field-item').find('.field-item-validation'), null)
            .evolutionValidation('addField',
                context.selectors.displayName,
                {
                    required: true,
                    messages: {
                        required: context.resources.requiredField
                    }
                },
                context.selectors.displayName.closest('.field-item').find('.field-item-validation'), null)
            .evolutionValidation('addField',
                context.selectors.terms,
                {
                    required: true,
                    messages: {
                        required: context.resources.requiredField
                    }
                },
                context.selectors.terms.closest('.field-item').find('.field-item-validation'), null);

        },
        save = function(context) {
            var data = {
                email: context.selectors.email.val(),
                displayName: context.selectors.displayName.val(),
                accessCode: context.selectors.accessCode.val()
            };

            return $.telligent.evolution.post({
                url: context.urls.submitForm,
                data: data,
                dataType: 'json',
                success: function(response) {
                    if (response.result == "true") {
                        context.selectors.submit.closest('.form').replaceWith('<div>' + context.resources.thankYou + '</div>');
                    }
                }
            });
        };

    $.fourroads.widgets.splash = {
        register: function(context) {
            scrapeElements(context);

            addValidation(context);
            attachHandlers(context);
        }
    };
})(jQuery, window);

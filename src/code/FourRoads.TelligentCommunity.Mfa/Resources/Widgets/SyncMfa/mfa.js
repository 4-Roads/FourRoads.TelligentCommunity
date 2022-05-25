(function ($, global) {
    if (typeof $.fourroads === 'undefined')
    $.fourroads = {};
    
    if (typeof $.fourroads.widgets === 'undefined')
    $.fourroads.widgets = {};
    var renderCodes = function(context, data) {
        //update count and date generated
        var stats = $('.stats ', context.selectors.codesWrapper);
        $('.count', stats).text('10');
        $('.generated-on', stats).text(context.resources.today);
        //display warning
        var html = '<h4>'+context.resources.showCodesWarn+'</h4>';
        html += '<div class="message warning">'+ context.resources.showCodesInfo +'</div>';
        //show new codes
        html += '<ul class="left-col">';
        for(var i=0; i < data.length;) {
            html += '<li>'+data[i++]+'</li>';
            if(i % 5 === 0) {
                html += '</ul><ul class="right-col">'
            }
        }
        html += '</ul><div style="clear:both;"></div>';
        var container = $('.codes-list', context.selectors.codesWrapper);
        container.html(html);
        container.fadeIn("slow");
    };
    var attachHandlers = function (context) {
        context.selectors.submit.on('click', function () { save(context, context.selectors.validateInput.val()); });
        context.selectors.disable.on('click', function () { save(context, '~~disable~~'); });
        context.selectors.toggle.on('click', function () { context.selectors.activate.hide(); context.selectors.configure.show(); });
        context.selectors.generateCodes.on('click', function (e) {
            e.preventDefault();
            $.telligent.evolution.post({
                url: context.urls.generateCodes,
                data: { },
                success: function (response) {
                    renderCodes(context, response);
                },
                error: function(resp, msg, err) {
                    $.telligent.evolution.notifications.show(msg, { type: 'error' } );
                }
            });
        });
        
        // enter key on code entry
        context.selectors.validateInput.bind('keypress', function (e) {
            // if enter was pressed, trigger a click on the submit button
            if (e.keyCode === 13) {
                e.preventDefault();
                context.selectors.submit.trigger('click');
            }
        });
    },
    scrapeElements = function (context) {
        $.each([context.selectors], function (i, set) {
            $.each(set, function (key, value) {
                set[key] = $(value);
            });
        });
    },
    save = function (context, code) {
        var data = {
            validationCode: code
        };
        console.log("save");
        context.selectors.validateInput.closest('.field-item').find('.field-item-validation').hide();
        
        return $.telligent.evolution.post({
            url: context.urls.validate,
            data: data,
            dataType: 'json',
            success: function (response) {
                if (response.result === 'true' || response.result === 'disabled') {
                    window.location = window.location;
                } else {
                    //Show error message
                    context.selectors.validateInput.closest('.field-item').find('.field-item-validation').show();
                }
            }
        });
    };
    
    $.fourroads.widgets.mfa = {
        register: function (context) {
            scrapeElements(context);
            attachHandlers(context);
        }
    };
})(jQuery, window);

(function($, global)
{
    if (typeof $.fourroads === 'undefined')
        $.fourroads = {};

    if (typeof $.fourroads.widgets === 'undefined')
        $.fourroads.widgets = {};

    var headerList = null;


    var attachUpdateHandlers = function(context) {
        headerList = $('<ul class="field-list"></ul>')
            .append(
                $('<li class="field-item"></li>')
                .append(
                    $('<span class="field-item-input"></span>')
                    .append(
                        $('<a href="#"></a>')
                        .addClass('button save')
                        .text(context.resources.save)
                    )
                )
            );

        $.telligent.evolution.administration.header($('<fieldset></fieldset>').append(headerList));

        var button = $('a.save', headerList);

        button.on('click', function(e) {
            e.preventDefault();

            var data = context.getData();

            $.telligent.evolution.post({
                url: context.urls.saveDataCallback,
                data: data,
                success: function(d) {
                    $.telligent.evolution.notifications.show(context.resources.updated);
                }
            });
        });

    };

    $.fourroads.widgets.metaDataUpdate = {
        register: function (context) {

            attachUpdateHandlers(context);
        }
    };

})(jQuery, window);

(function ($) {
    // Make sure JQuery is loaded
    if (typeof ($) === 'undefined')
        return;

    var registered = false,
    loadConsole = function (context) {
        //TODO: Get HTML from plugin property
        $(document.body).prepend(context.markup);
    },
    modalReset = function (success) {
        if (success) {
            reset({
                data: {
                    action: 'cache'
                }
            });
        }
    },
    openModal = function(e) {
        Telligent_Modal.Open(e.data.url, 550, 400, modalReset);

        return false;
    },
    refresh = function (shouldRefresh) {
        if (shouldRefresh) {
            window.location = window.location;
        }
    },
    reset = function (e) {
        $.telligent.evolution.post({
            url: $.telligent.evolution.site.getBaseUrl() + 'api.ashx/v2/themeutility/reset.json?action={action}',
            data: {
                action: e.data.action
            },
            cache: false,
            dataType: 'json',
            success: function (response) {
                refresh(true);
            }
        });

        return false;
    },
    scrapeElements = function (context) {
        $.each([context.elements], function (i, set) {
            $.each(set, function (key, value) {
                set[key] = context.wrapper.find(value);
            });
        });
    },
    wireEvents = function (context) {
        context.wrapper.hover(function() {
            $(this).fadeTo("fast", 1.0);
        }, function() {
            $(this).fadeTo("fast", 0.5);
        });
        context.elements.fullRevert.click({ action: 'theme' }, reset);
        context.elements.reset.click({ action: 'cache' }, reset);
        context.elements.selectiveRevert.click({url: context.urls.modal}, openModal);
    },
    wireUi = function (context) {
    };

    var api = {
        register: function (options) {
            // Ensure this function only gets called once
            if (!registered) {
                registered = true;

                var context = $.extend({}, api.defaults, options || {});
                // shallow-copy elements to avoid shared state if other widgets are rendered
                context.elements = $.extend({}, context.elements);
                // shallow-copy resources to avoid shared state if other widgets are rendered
                context.resources = $.extend({}, context.resources);
                // inject the cache console markup
                loadConsole(context);
                // ensure certain elements are already jquery selections
                context.wrapper = $(context.wrapper);
                // pre-grab all the pieces of the ui so minimize selections
                scrapeElements(context);
                // wire up the widget's ui to respond to its functionality
                wireUi(context);
                // wire up the widget's client functionality
                wireEvents(context);
            }
        }
    };
    $.extend(api, {
        defaults: {
            wrapper: '#theme-console', // wrapper selector
            // these should be overriden with actual resource values when registered
            resources: {
                cookieName: 'FourRoads.Telligent.Tools.ClearCache'
            },
            // selectors relative to the wrapper - rarely need to be overriden
            elements: {
                reset: '#resetCache',
                fullRevert: '#fullRevert',
                selectiveRevert: '#selectiveRevert'
            },
            markup: '<div class="theme-console" id="theme-console">' +
            '	<ul class="navigation-list">' +
            '		<li class="navigation-item">' +
            '		    <a id="fullRevert" href="javascript:(0)">Full revert</a>' +
            '	    </li>' +
            '		<li class="navigation-item">' +
            '		    <a id="selectiveRevert" href="javascript:(0)">Selective revert</a>' +
            '		</li>' +
            '	    <li class="navigation-item">' +
            '		    <a id="resetCache" href="javascript:(0)">Clear cache</a>' +
            '	    </li>' +
            '	</ul>' +
            '</div>',
            urls: {
                modal: '/ControlPanel/Utility/RevertTheme.aspx?ThemeTypeID=0c647246-6735-42f9-875d-c8b991fe739b&ThemeContextID=00000000-0000-0000-0000-000000000000&ThemeName=c297dba84c7a40af9371c409abe65c26'
            }
        }
    });

    // expose api in a public namespace
    if (typeof $.fourroads === 'undefined') {
        $.fourroads = {};
    }
    if (typeof $.fourroads.plugins === 'undefined') {
        $.fourroads.plugins = {};
    }
    $.fourroads.plugins.themeConsole = api;
})(jQuery);

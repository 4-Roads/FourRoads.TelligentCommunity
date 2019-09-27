(function ($) {
    if (typeof $.fourroads == 'undefined') {
        $.fourroads = {};
    }
    if (typeof $.fourroads.widgets == 'undefined') {
        $.fourroads.widgets = {};
    }

    var scrapeElements = function (context) {
        $.each([context.selectors], function (i, set) {
            $.each(set, function (key, value) {
                set[key] = $(value);
            });
        });
    },
    getSelectionText = function () {
        var text = "";
        if (window.getSelection) {
            text = window.getSelection().toString();
        } else if (document.selection && document.selection.type != "Control") {
            text = document.selection.createRange().text;
        }
        return text;
    };

    $.fourroads.widgets.contentDiscuss =
	{
	    register: function (context) {
	        scrapeElements(context)

	        context.selectors.setForum.on('click', function (e) {
	            e.preventDefault();
	            var target = $(e.target).closest('.set-forum');
	            var forumId = context.selectors.selectForum.val();
	            $.telligent.evolution.post({
	                url: context.urls.setForum,
	                data: {
	                    blogId: target.data('blogid'),
	                    wikiId: target.data('wikiid'),
	                    forumId: forumId
	                },
	                success: function (response) {
	                    if (response.success == 'true') {
	                        var span = $('<br /><span>Forum updated</span>');
	                        span.insertAfter(target);
	                        window.setTimeout(function () {
	                            span.hide(200);
	                        }, 5000);
	                    }
	                    else {
	                        var span = $('<br /><span>Forum update failed</span>');
	                        span.insertAfter(target);
	                        window.setTimeout(function () {
	                            span.hide(200);
	                        }, 5000);
	                    }
	                }
	            })
	        });


	        var textArea = context.selectors.textAreaSelector;
	        if (textArea.length > 0) {
	            textArea.on('mouseup', function (e) {
	                e.preventDefault();
	                var selected = $.telligent.glow.utility.getSelectedHtmlInElement(textArea.get(0), false, false, null);
	                if (selected != null && selected != '') {
	                    var menu = context.selectors.textAreaMenu;

	                    var xPos = e.pageX - textArea.offset().left;
	                    var yPos = e.pageY - textArea.offset().top;

	                    var o = {
	                        left: e.pageX - 5,
	                        top: e.pageY - 5
	                    };
	                    menu.show(200).offset(o);
	                }
	            });
	            $(document).on('click', function (e) {
	                var menu = context.selectors.textAreaMenu;
	                if (menu.is(':visible')) {
	                    setTimeout(function () {
	                        var selected = $.telligent.glow.utility.getSelectedHtmlInElement(textArea.get(0), false, false, null);
	                        if (!menu.is(':hover') && (selected == null || selected == '')) {
	                            menu.hide(200);
	                        }
	                    }, 200);
	                }
	            });
	            $('.start-discussion').live('click', function (e) {
	                e.preventDefault();
	                var target = $(e.target).closest('.start-discussion');
	                var selected = $.telligent.glow.utility.getSelectedHtmlInElement(textArea.get(0), false, false, null);
	                if (selected != null) {
	                    var data = {
	                        url: context.url,
	                        body: selected,
	                        title: target.data('title'),
	                        authorName: target.data('author'),
	                        forumId: target.data('forumid'),
	                        contentId: target.data('contentid'),
	                        contentTypeId: target.data('contenttypeid')
	                    };
	                    $.telligent.evolution.post({
	                        url: context.urls.storeTempDataUrl,
	                        data: data,
	                        success: function (response) {
	                            window.location.href = response.threadUrl;
	                        }
	                    })
	                }
	            })
	        }
	    }
	}

})(jQuery);

if (!RedactorPlugins) var RedactorPlugins = {};

//Tags in TINYMce are in the format [Tag:<tagname>] which is great but not all wysiwyg editors can handle the formating, so convert to a span
function fixupTags(editor) {
    var text = editor.code.get();

    editor.code.set(text.replace(/\[Tag\:([^\]]+)]/gi, '<span class="redactor-tag">$1</span>'));
}

RedactorPlugins.hashtags = function () {

    var isLookupMode = false;
    var delayMs = 700;
    var delayTimer;
    var tagName = '';
    var template;
    var hashKeyCode = "#".charCodeAt(0);
    var spaceKeyCode = " ".charCodeAt(0);
    var returnKeyCode = "\r".charCodeAt(0);
    var currentNode = null;

    var delayedLookup = function (editor) {
        clearTimeout(delayTimer);
        delayTimer = setTimeout(function () { displayModal(editor) }, delayMs);
        return true;
    }

    var displayModal = function (editor) {
        var current = editor.selection.getCurrent();

        ///Want to change this to telligent popup menu
        if (current != undefined && current != false) {
            var currentNode = $(current).closest('.redactor-tag');
            var menu = $('.hashtag-menu');
            if (menu.length == 0) {
                menu = $('<div class="hashtag-menu"></div>');
                $('body').append(menu);

                menu.glowPopUpMenu({
                    groupCssClass: 'editor-popup-menu',
                    itemCssClass: 'menu-item',
                    itemSelectedCssClass: 'menu-item',
                    itemExpandedCssClass: 'menu-item expanded',
                    position: 'down',
                    zIndex: 99999,
                    closeOnMouseOut: false,
                    menuItems: []
                }).bind({
                    glowPopUpMenuOpened: function (e) {
                        var menu = $(this);
                        editor.code.sync();
                        var tag = $(editor.selection.getCurrent()).closest('.redactor-tag');
                        var text = tag.text();
                        if (text.indexOf('#') == 0) {
                            text = text.slice(1);
                        }
                        $.telligent.evolution.get({
                            url: $.telligent.evolution.site.getBaseUrl() + 'api.ashx/v2/hashtags.json?QueryText=' + text,
                            success: function (response) {
                                var items = eval(response.HashTags);
                                menu.glowPopUpMenu('clear');
                                for (var i = 0; i < items.length; i++) {
                                    var menuItem = menu.glowPopUpMenu('createMenuItem', {
                                        id: items[i].Name,
                                        text: items[i].Name,
                                        onClick: function () {
                                            editor.code.sync();
                                            if (tag.length > 0) {
                                                tag.text('#' + this.id);
                                                editor.insert.html('<span></span>'); // this seems to force redactor to realise there's been a change
                                                editor.code.sync();
                                                editor.caret.setAfter(tag[0]);
                                            }
                                        }
                                    });
                                    menu.glowPopUpMenu('add', menuItem);
                                }
                                menu.glowPopUpMenu('refresh');
                            }
                        });
                    }
                }).bind({
                    glowPopUpMenuClosed: function () {
                    }
                });
            }
            if (currentNode.hasClass('redactor-tag')) {
                menu.glowPopUpMenu('open', currentNode);
            }
        }
    };

    return {
        init: function () {
            fixupTags(this);

            this.$editor.on('keypress', $.proxy(function (e) {
                var key = e.keyCode || e.which;
                var ctrl = e.ctrlKey || e.metaKey;
                var shift = e.shiftKey;

                var current = this.selection.getCurrent();

                if (current != undefined && current != false) {
                    currentNode = $(current).closest('.redactor-tag');
                    if (key == hashKeyCode && currentNode.length == 0) {
                        e.preventDefault();
                        var node = $('<span class="redactor-tag" />').html('#');
                        var inserted = this.insert.node(node);
                        this.code.sync();
                        this.caret.setEnd(inserted);
                        return false;
                    }
                    else if (currentNode.length > 0) {
                        if (key == spaceKeyCode || key == returnKeyCode || key == hashKeyCode) {

                            this.caret.setAfter(currentNode[0]);
                            $('.hashtag-menu').glowPopUpMenu('close');
                        }
                        else {
                            delayedLookup(this);
                        }
                    }
                }
            }, this));

        },
        clickCallback: function (editor, e) {
            if (editor.selection != undefined) {
                var currentNode = $(editor.selection.getCurrent()).closest('.redactor-tag');
                if (currentNode.length > 0 && currentNode.text.length > 2) {
                    delayedLookup(editor);
                }
                else {
                    $('.hashtag-menu').glowPopUpMenu('close');
                }
            }
        }
    }
};

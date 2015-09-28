
if (!RedactorPlugins) var RedactorPlugins = {};

//Tags in TINYMce are in the format [Tag:<tagname>] which is great but not all wysiwyg editors can handle the formating, so convert to a span
function fixupTags(editor) {
    var text = editor.code.get();

    editor.code.set(text.replace(/\[Tag\:([^\]]+)]/gi, '<span class="redactor-mention">$1</span>'));
}

RedactorPlugins.mentions = function () {

    var isLookupMode = false;
    var delayMs = 700;
    var delayTimer;
    var tagName = '';
    var template;
    var atKeyCode = "@".charCodeAt(0);
    var spaceKeyCode = " ".charCodeAt(0);
    var returnKeyCode = "\r".charCodeAt(0);
    var currentNode = null;

    var delayedLookup = function (editor) {
        clearTimeout(delayTimer);
        delayTimer = setTimeout(function () { displayModal(editor) }, delayMs);
        return true;
    }

    var displayModal = function (editor) {
        var currentNode = $(editor.selection.getCurrent());
        ///Want to change this to telligent popup menu
        if (currentNode != undefined && currentNode && (currentNode.hasClass('redactor-mention') || currentNode.parent().hasClass('redactor-mention'))) {
            var menu = $('.mention-menu');
            if (menu.length == 0) {
                menu = $('<div class="mention-menu"></div>');
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
                        var mention = $(editor.selection.getCurrent()).closest('.redactor-mention');
                        var text = mention.text();
                        if (text.indexOf('@') == 0) {
                            text = text.slice(1);
                        }
                        $.telligent.evolution.get({
                            url: $.telligent.evolution.site.getBaseUrl() + 'api.ashx/v2/mentionables.json?QueryText=' + text,
                            success: function (response) {
                                var items = eval(response.Mentionables);
                                menu.glowPopUpMenu('clear');
                                for (var i = 0; i < items.length; i++) {
                                    var menuItem = menu.glowPopUpMenu('createMenuItem', {
                                        id: items[i].PreviewHtml,
                                        text: items[i].PreviewHtml,
                                        onClick: function () {
                                            if (mention.length > 0) {
                                                mention.text('@' + this.id);
                                                mention.attr('id', $(this).data('contentid').replace(/-/g, '') + ':' + $(this).data('contenttypeid').replace(/-/g, ''));
                                                editor.insert.html('<span class="empty"></span>'); // this seems to force redactor to realise there's been a change
                                                editor.code.sync();
                                                editor.caret.setAfter(mention[0]);
                                            }
                                        }
                                    });
                                    menu.glowPopUpMenu('add', menuItem);
                                    var m = $(menuItem);
                                    m.data('contentid', items[i].ContentId);
                                    m.data('contenttypeid', items[i].ContentTypeId);
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
            if (currentNode.hasClass('redactor-mention')) {
                menu.glowPopUpMenu('open', currentNode);
            }
            else {
                menu.glowPopUpMenu('open', currentNode.parent());
            }
        }
    };

    return {
        init: function () {
            fixupTags(this);

            this.$editor.on('keypress', $.proxy(function (e) {
                var key = e.which;
                var ctrl = e.ctrlKey || e.metaKey;
                var shift = e.shiftKey;

                var current = this.selection.getCurrent();
                if (current != undefined && current != false) {
                    var currentNode = $(current).closest('.redactor-mention');
                    if (key == spaceKeyCode || key == returnKeyCode || key == atKeyCode) {
                        if (currentNode.length > 0) {
                            this.caret.setAfter(currentNode[0]);
                            $('.mention-menu').glowPopUpMenu('close');
                        }
                        else if (key == atKeyCode) {
                            e.preventDefault();
                            var node = $('<span class="redactor-mention" />').html('@');
                            var inserted = this.insert.node(node);
                            this.code.sync();
                            this.caret.setEnd(inserted);
                            return false;
                        }
                    }
                    else if (currentNode.length > 0 && currentNode.text().length > 2) {
                        delayedLookup(this);
                    }
                }

            }, this));

            // keypress won't capture the backspace key so use the keydown event for this
            this.$editor.on('keydown', $.proxy(function (e) {
                if (this.selection != undefined) {
                    var currentNode = $(this.selection.getCurrent()).closest('.redactor-mention');
                    if (currentNode.length > 0 && e.which == 8) {
                        currentNode.remove();
                        this.code.sync();
                        $('.mention-menu').glowPopUpMenu('close');
                    }
                }
            }, this));
        }
    }
};

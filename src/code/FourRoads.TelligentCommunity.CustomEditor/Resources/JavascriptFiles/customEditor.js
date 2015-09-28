

(function ($) {

    if (typeof $.fourroads == 'undefined') {
        $.fourroads = {};
    }
    if (typeof $.fourroads.customEditor == 'undefined') {
        $.fourroads.customEditor = {};
    }

    $.fourroads.customEditor.getCookie = function (name) {
        var value = "; " + document.cookie;
        var parts = value.split("; " + name + "=");
        if (parts.length == 2) return parts.pop().split(";").shift();
    }

    var callbackArray = [];

    function CallbackInterceptor(editorId) {
        this.delegateCallBack = null;
        this.editorId = editorId;
    }

    CallbackInterceptor.prototype.setDelegateCallback = function (func) {
        this.delegateCallBack = func;
    };

    CallbackInterceptor.prototype.onClickEvent = function (e) {
        RedactorPlugins.hashtags().clickCallback(this, e);
    }

    var getCodeFixed = function (editorId) {
        var code = $('<span></span>').append($($.fourroads.customEditor.GetEditor(editorId).redactor('code.get')));

        $('.redactor-tag', code).each(function (i, j) {
            var tag = $(j);
            var text = tag.text().trim();
            if (text.indexOf('#') == 0) {
                text = text.slice(1);
            }
            if (text.length > 0) {
                tag.replaceWith('[tag:' + text + ']');
            }
            else {
                tag.remove();
            }
        });

        $('.redactor-mention', code).each(function (i, j) {
            $(j).replaceWith('[mention:' + $(j).attr('id') + ']');
        });

        $('.redactor-video', code).each(function (i, j) {
            $(j).replaceWith('[View:' + $(j).data('url') + ']');
        });

        return code.html();
    };

    /* Attach the editor to an element */
    $.fourroads.customEditor.Attach = function (editorId, imageUploadUrl, imageDeleteUrl, supportImages, sourceButton) {

        var callback = new CallbackInterceptor(editorId);

        var options = {
            plugins: ['video', 'table', 'hashtags', 'mentions', 'bufferbuttons'],
            focus: true,
            buttonSource: false,
            clickCallback: callback.onClickEvent,
            convertImageLinks: true,
            convertLinks: true,
            convertVideoLinks: true
        };

        if (sourceButton) {
            options.buttonSource = true;
        }

        if (supportImages) {
            $.extend(options, {
                imageUpload: imageUploadUrl,
                fileUpload: imageUploadUrl,
                imageDeleteCallback: function (url, image) {
                    var src = $(image).attr('src');
                    $.telligent.evolution.post({
                        url: imageDeleteUrl,
                        data: { image: src }
                    });
                },
                imageUploadCallback: function (image, json) {
                    var jImage = $(image);

                    var wrapper = $('<a href="' + jImage.attr('src') + '" target="_blank">' + json.resizedMarkup + '</a>');

                    $(image).replaceWith(wrapper);
                }
            });
        }

        $.fourroads.customEditor.GetEditor(editorId).redactor(options);

        callbackArray.push({ 'editorId': editorId, 'callback': callback });
    }

    /* InsertContent should insert html at the cursor position */
    $.fourroads.customEditor.GetContent = function (editorId) {
        return getCodeFixed(editorId);
    }

    /* InsertContent should insert html at the cursor position */
    $.fourroads.customEditor.GetEditor = function (editorId) {
        return $('#' + editorId);
    }

    $.fourroads.customEditor.UpdateBookmark = function (editorId) {

    }

    $.fourroads.customEditor.AttachOnChangeHandler = function (editorId, func) {
        for (var i = 0; i < callbackArray.length; i++) {
            if (callbackArray[i].editorId == editorId) {
                callbackArray[i].callback.setDelegateCallback(func);
            }
        }
    }

    /* InsertContent should insert html at the cursor position */
    $.fourroads.customEditor.InsertContent = function (editorId, html) {
        $.fourroads.customEditor.GetEditor(editorId).redactor('insert.html', html);
    }

    /* UpdateContent replaces whatever is in the editor with the html supplied */
    $.fourroads.customEditor.UpdateContent = function (editorId, html) {
        $.fourroads.customEditor.GetEditor(editorId).redactor('insert.set', html);
    }

    /* Set the focus on the editor */
    $.fourroads.customEditor.SetFocus = function (editorId) {
        $.fourroads.customEditor.GetEditor(editorId).redactor('focus.setStart');
    }
})(jQuery);


function getHostName(url) {
    var match = url.match(/:\/\/(www[0-9]?\.)?(.[^/:]+)/i);
    if (match != null && match.length > 2 &&
        typeof match[2] === 'string' && match[2].length > 0) {
        return match[2];
    }
    else {
        return null;
    }
}

!function (send) {
    XMLHttpRequest.prototype.send = function (data) {

        if (this.headers === undefined || this.headers['Authorization-Code'] == null && this.addAuthorizationHeader == true) {
            this.setRequestHeader('Authorization-Code', $.fourroads.customEditor.getCookie('AuthorizationCookie'));
        }

        send.call(this, data);
    }
}(XMLHttpRequest.prototype.send);

!function (open) {
    XMLHttpRequest.prototype.open = function (action, url, flag) {

        if (getHostName(url) == window.location.hostname) {
            this.addAuthorizationHeader = true;
        }

        open.call(this, action, url, flag);
    }
}(XMLHttpRequest.prototype.open);

!function (setRequestHeader) {
    XMLHttpRequest.prototype.setRequestHeader = function (header, value) {

        if (!this.headers) {
            this.headers = {};
        }
        if (!this.headers[header]) {
            this.headers[header] = [];
        }
        this.headers[header].push(value);

        setRequestHeader.call(this, header, value);
    }
}(XMLHttpRequest.prototype.setRequestHeader);
if (!RedactorPlugins) var RedactorPlugins = {};

RedactorPlugins.video = function () {
    return {
        reUrlYoutube: /https?:\/\/(?:[0-9A-Z-]+\.)?(?:youtu\.be\/|youtube\.com\S*[^\w\-\s])([\w\-]{11})(?=[^\w\-]|$)(?![?=&+%\w.-]*(?:['"][^<>]*>|<\/a>))[?=&+%\w.-]*/ig,
        reUrlVimeo: /(videos|video|channels|\.com)\/([\d]+)/,
        getTemplate: function () {
            return String()
            + '<section id="redactor-modal-video-insert">'
                + '<label>' + this.lang.get('video_html_code') + '</label>'
                + '<textarea id="redactor-insert-video-area" style="height: 160px;"></textarea>'
            + '</section>';
        },
        init: function () {
            var button = this.button.addAfter('image', 'video', this.lang.get('video'));
            this.button.addCallback(button, this.video.show);
        },
        show: function () {
            this.modal.addTemplate('video', this.video.getTemplate());

            this.modal.load('video', this.lang.get('video'), 700);
            this.modal.createCancelButton();

            var button = this.modal.createActionButton(this.lang.get('insert'));
            button.on('click', this.video.insert);

            this.selection.save();
            this.modal.show();

            $('#redactor-insert-video-area').focus();

        },
        insert: function () {
            var data = $('#redactor-insert-video-area').val();

            if (!data.match(/<iframe|<video/gi)) {
                data = this.clean.stripTags(data);

                if (data.match(/vimeo/)) {
                    var url = "https://vimeo.com/api/oembed.json?url=" + encodeURIComponent(data);
                    $.ajax({
                        method: 'GET',
                        async: false,
                        url: url,
                        dataType: 'json',
                        global: false,
                        success: function (response) {
                            data = '<iframe class="redactor-video" style="width: 500px; height: 281px;" data-url="https://player.vimeo.com/video/' + response.video_id + '" src="https://player.vimeo.com/video/' + response.video_id + '" frameborder="0" allowfullscreen></iframe>';
                        }
                    });
                }
                else if (data.match(/youtu/)) {
                    var iframeStart = '<iframe class="redactor-video" style="width: 500px; height: 281px;" src="',
                    iframeEnd = ' frameborder="0" allowfullscreen></iframe>';

                    data = data.replace(this.video.reUrlYoutube, iframeStart + '//www.youtube.com/embed/$1" data-url="' + data + '"' + iframeEnd);
                }
            }
            else {
                var wrapper = $('<div/>').append($(data));
                var frame = wrapper.find('iframe');
                frame.addClass('redactor-video');
                frame.attr('data-url', frame.attr('src'));
                data = wrapper.html();
            }

            this.selection.restore();
            this.modal.close();

            var current = this.selection.getBlock() || this.selection.getCurrent();

            if (current) $(current).after(data);
            else {
                this.insert.html(data);
            }

            this.code.sync();
        }

    };
};

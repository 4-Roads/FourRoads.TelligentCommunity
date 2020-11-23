(function (j, global) {
    var spinner = '<span class="ui-loading" width="48" height="48"></span>',
        lastState,
        api = {
            register: function(options) {
                var processingTemplate = j.telligent.evolution.template.compile(options.processingTemplate);
                var startTemplate = j.telligent.evolution.template.compile(options.startTemplate);

                function pad(number) {
                    return ('0' + number).slice(-2);
                }

                function getClock(ms) {
                    hours = Math.floor(ms / 3600000), // 1 Hour = 3600000 Milliseconds
                        minutes = Math.floor((ms % 3600000) / 60000), // 1 Minutes = 60000 Milliseconds
                        seconds = Math.floor(((ms % 360000) % 60000) / 1000); // 1 Second = 1000 Milliseconds

                    return {
                        hours: hours,
                        minutes: minutes,
                        seconds: seconds,
                        clock: pad(hours) + ":" + pad(minutes) + ":" + pad(seconds)
                    };
                }

                function getDate(sqlDate) {
                    //sqlDate in SQL DATETIME format ("yyyy-mm-ddThh:mm:ss")
                    var sqlDateArr1 = sqlDate.split("-");
                    //format of sqlDateArr1[] = ['yyyy','mm','dd hh:mm:ss']
                    var sYear = sqlDateArr1[0];
                    var sMonth = (Number(sqlDateArr1[1]) - 1).toString();
                    var sqlDateArr2 = sqlDateArr1[2].split("T");
                    //format of sqlDateArr2[] = ['dd', 'hh:mm:ss']
                    var sDay = sqlDateArr2[0];
                    var sqlDateArr3 = sqlDateArr2[1].split(":");
                    //format of sqlDateArr3[] = ['hh','mm','ss']
                    var sHour = sqlDateArr3[0];
                    var sMinute = sqlDateArr3[1];
                    var sSecond = sqlDateArr3[2];

                    return new Date(sYear, sMonth, sDay, sHour, sMinute, sSecond);
                }

                function updateStatus() {

                    j.telligent.evolution.post({
                        url: options.statusUrl,
                        data: null,
                        success: function(response) {
                            j('#' + options.titleId).text(response.State);
                            switch (response.State) {
                            case 'Running':
                            case 'Finished':
                                {
                                    var rowsProcessingTimeAvg = parseFloat(response.RowsProcessingTimeAvg);
                                        if (!isNaN(rowsProcessingTimeAvg)) {
                                        response.RowsProcessingTimeAvg =
                                            (1 / (rowsProcessingTimeAvg / 10000000) * 60).toFixed(3);
                                        response.ElapsedTime = getClock(Math.abs(getDate(response.LastUpdated) - getDate(response.Started))).clock;

                                        response["Action"] = response.State == 'Running' ? 'Cancel' : 'Reset';

                                        j('#' + options.processingArea)
                                            .html(processingTemplate({ response: response }));

                                        j('#' + options.actionLink).click(function(e) {
                                            e.preventDefault();
                                            j(this).hide();

                                            var cancelUrl = options.cancelCurrentJobUrl;
                                            var resetUrl = options.resetUrl;

                                            j('#' + options.processingArea).html(j(spinner));

                                            j.telligent.evolution.post({
                                                url: response.State == 'Running' ? cancelUrl : resetUrl,
                                                data: null
                                            });
                                        });


                                        j('#' + options.downloadRewriteMap).click(function (e) {
                                            e.preventDefault();
                                            
                                               fetch(options.downloadRewriteMapUrl)
                                                    .then(resp => resp.blob())
                                                    .then(blob => {
                                                        const url = window.URL.createObjectURL(blob);
                                                        const a = document.createElement('a');
                                                        a.style.display = 'none';
                                                        a.href = url;
                                                        a.download = 'legacy_rewrite_map.config';
                                                        document.body.appendChild(a);
                                                        a.click();
                                                        window.URL.revokeObjectURL(url);
                                                    }); 
                                        });

                                        //This forces a refresh of the error list
                                        j.telligent.evolution.url.hashData({ stamp: Date.now()}, {});
                                    }

                                }
                                break;

                            case 'Ready':{

                                    //If the checkbox selectors are being displayed then don't do anything
                                    if ($('input:checkbox.object-handlers').length > 0) {
                                        break;
                                    }

                                    j('#' + options.processingArea)
                                        .html(startTemplate({ response: { Action: 'Start' } }));

                                    j('#' + options.actionLink).click(function(e) {
                                        e.preventDefault();

                                        var data = [];
                                        $('input:checkbox.object-handlers').each(function() {
                                            data.push((this.checked ? $(this).val() : ""));
                                        });

                                        j(this).hide();
                                        j('#' + options.processingArea).html(j(spinner));

                                        j.telligent.evolution.post({
                                            url: options.startUrl,
                                            data: { objectHandlers: data }
                                        });
                                    });

                                }
                                break;

                            default:
                                {
                                    j('#' + options.processingArea).html(j(spinner));
                                }
                                break;
                            }
                          
                        }
                    });
                }

                setInterval(updateStatus, 10000);

                updateStatus();
            }
        };

    $.fourroads = $.fourroads || {};
    $.fourroads.migration = api;

})(jQuery, window);
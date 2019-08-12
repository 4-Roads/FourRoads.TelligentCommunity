/**
 * 
   ## How to use it:

      1) if not yet installed, install node.js and npm
      2) if not installed, install gulp
            > npm install -g gulp-cli
      3) open shell in root of the trunk folder (this file should 
         be in the same folder) and type:

            > npm install

      4) type:

            > gulp watch-widgets
      
         Or in visual stuido -> view -> other windows -> task runner explorer
         run task 'watch-widgets'

// Create a file 'mycoonfig.json' and put it in the same folder as this file
// the contents of this file should be the customized version of what you see below:
```
{
   "clearcache": {
       "auth": "ZmluNm41bnRwcTRxeWh1bnJrYWg4d244dDgydjA6YWRtaW4=",
       "url": "http://nordic.lvh.me/api.ashx/v2/4roads-dev/expireallui.json"
   },
   "widgets": {
       "mapping": {
           "SyncMfa": "6d9264a6f6c4434c9d9954b87a865e57",
           "ValidateMfa": "295391e2b78d4b7e8056868ae4fe8fb3"
       },
       "src": "C:\\dev\\FourRoads.TelligentCommunity\\src\\code\\FourRoads.TelligentCommunity.GoogleMfa\\Resources\\Widgets\\",
       "dest": "C:\\dev\\NS\\trunk\\development\\source\\code\\NordicSemi.Web\\filestorage\\defaultwidgets\\3bd55b782e624b77b6e730f6bce17de2"
   }
}
```
note, that the 'auth' value above is BASE64("{user's apikey}:{username}")
*/

//nothing to edit below this line!
var gulp = require('gulp'),
    config = require('.\\myconfig')
    watch = require('gulp-watch'),
    debug = require('gulp-debug'),
    filesize = require('gulp-filesize'),
    rename = require('gulp-rename'),
    fn = require('gulp-fn'),
    req = require("request");

function clearCaches() {
    return req.post(config.clearcache.url,
        {
            headers: {
                'Rest-User-Token': config.clearcache.auth
            },
            callback: function(err, resp, body) {
                console.log(body);
            }
        },
        config.clearcache.callback);
}

function watchEmAll() {
    return watch(config.widgets.src + '**/*', function(v){
        var widgetName = v.path.replace(v.base+'\\', '').replace('\\'+v.basename,'');
        if(v.basename.match(/\.xml/)) {
            var newName =config.widgets.mapping[widgetName] + '.xml';
        } else {
            var newName = config.widgets.mapping[widgetName]+'\\'+v.basename;
        }
        return gulp.src(v.path)
            .pipe(rename( '.\\'+ newName))
            .pipe(debug({title: newName}))
            .pipe(gulp.dest(config.widgets.dest))
            .pipe(filesize())
            //comment out this line if do not need to clear caches 
            .pipe(fn(function(f,e){clearCaches();}))
            ;
    });
} 
gulp.task("watch-widgets", watchEmAll);
gulp.task("clear-caches", clearCaches);
gulp.task('index', gulp.series(watchEmAll, clearCaches));

// for IE
document.currentScript = document.currentScript || (function() {
  var scripts = document.getElementsByTagName('script');
  return scripts[scripts.length - 1];
})();

var websocketurl = document.currentScript.getAttribute('data-websocketurl');
var websockettoken = document.currentScript.getAttribute('data-websockettoken');

function ready(fn) {
    if (document.readyState != 'loading'){
        fn();
    } else if (document.addEventListener) {
        document.addEventListener('DOMContentLoaded', fn);
    } else {
        document.attachEvent('onreadystatechange', function() {
        if (document.readyState != 'loading')
            fn();
        });
    }
}

ready(function() {
    ws.start("#wsResult", websocketurl, websockettoken);
});

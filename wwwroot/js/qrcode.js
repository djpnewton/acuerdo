// for IE
document.currentScript = document.currentScript || (function() {
  var scripts = document.getElementsByTagName('script');
  return scripts[scripts.length - 1];
})();

var id = document.currentScript.getAttribute('data-id');
var text = document.currentScript.getAttribute('data-text');
var width = document.currentScript.getAttribute('data-width');
var height = document.currentScript.getAttribute('data-height');

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
    new QRCode(document.getElementById(id),
    {
        text: text,
        width: width,
        height: height
    });
});

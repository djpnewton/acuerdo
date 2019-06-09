// for IE
document.currentScript = document.currentScript || (function() {
  var scripts = document.getElementsByTagName('script');
  return scripts[scripts.length - 1];
})();

var market = document.currentScript.getAttribute('data-market');

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

function loadChart(intervalCode) {
    var multiplier = parseInt(intervalCode[0]);
    var period = intervalCode[1];
    var intervalSeconds = 3600;
    switch (period) {
        case "H":
            intervalSeconds = multiplier * 3600;
            break;
        case "D":
            intervalSeconds = multiplier * 3600 * 24;
            break;
        case "W":
            intervalSeconds = multiplier * 3600 * 24 * 7;
            break;
        case "M":
            intervalSeconds = multiplier * 3600 * 24 * 7 * 30;
            break;
    }
    var unixTimestamp = Math.floor(+new Date()/1000);
    var start = unixTimestamp - intervalSeconds * 30;
    var url = "/Market/klines?market=" + market + "&start=" + start + "&end=" + unixTimestamp + "&interval=" + intervalSeconds;
    $.getJSON(url, function(data) {
        viachart.load(data);
    });
}

ready(function() {
    viachart.create("#chart");
    loadChart("1H");
    $(document).on('click', '.loadchart', function() {
        loadChart($(this).attr('data-period'));
    });
});

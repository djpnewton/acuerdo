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

ready(function () {
    var items = document.getElementsByClassName('set-email');
    for (var i = 0; i < items.length; i++) {
        items[i].addEventListener('click', function () {
            var email = this.textContent.trim();
            document.getElementById('Email').value = email;
            document.getElementById('Offset').value = 0;
            document.getElementById('search-form').submit();
        });
    }
});

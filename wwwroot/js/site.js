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
    // submit the form when element changes
    $(".onchange-submit").each(function(index) {
        $(this).change(function() {
            this.form.submit();
        });
    });
});

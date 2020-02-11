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

    // submit the form when enter pressed
    $(".onenter-submit").each(function(index) {
        $(this).keypress(function(e) {
            if (e.which == 13) {
                this.form.submit();
            }
        });
    });

    // init datepicker elements
    $(".datepicker").datepicker({ format: 'yyyy/mm/dd', autoclose: true, todayBtn: 'linked' });
});

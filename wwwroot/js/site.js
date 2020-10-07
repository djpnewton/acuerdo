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
        $(this).change(function () {
            var offsetInput = this.form.querySelector("#Offset");
            if (offsetInput)
                offsetInput.value = 0;
            this.form.submit();
        });
    });

    // submit the form when enter pressed
    $(".onenter-submit").each(function(index) {
        $(this).keypress(function(e) {
            if (e.which == 13) {
                var offsetInput = this.form.querySelector("#Offset");
                if (offsetInput)
                    offsetInput.value = 0;
                this.form.submit();
            }
        });
    });

    // init datepicker elements
    $(".datepicker").datepicker({ format: 'yyyy/mm/dd', autoclose: true, todayBtn: 'linked' });
});

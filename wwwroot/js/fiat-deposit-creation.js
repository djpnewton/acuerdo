$(function() {
    $('.form-deposit-button').click(function() {
        var form = $(this).closest('form');
        form.validate();
        if (!form.valid())
            return;

        var amount = $('#Amount');
        var text = 'Amount: ' + amount.val() + ' ' + form.attr('x-unit');

        $('#modal-deposit-create-p').text(text);
        $('#confirm-deposit-submit').modal('show')
    });

    $('#confirm-deposit').click(function() {
        $('#deposit-form').submit();
    });
});

$(function() {
    $('.form-withdrawal-button').click(function() {
        var form = $(this).closest('form');
        form.validate();
        if (!form.valid())
            return;

        var account = $('#WithdrawalAccount');
        var amount = $('#Amount');
        var text = 'Withdrawal account: ' + account.val() + ', Amount: ' + amount.val() + ' ' + form.attr('x-unit');

        $('#modal-withdrawal-create-p').text(text);
        $('#confirm-withdrawal-submit').modal('show')
    });

    $('#confirm-withdrawal').click(function() {
        $('#withdraw-form').submit();
    });
});

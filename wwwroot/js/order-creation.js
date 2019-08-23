$(function() {
    function showOrderModal(form) {
        form.validate();
        if (!form.valid())
            return;

        var formId = form.prop('id');
        $('#modal-table-header').prop('formId', formId);

        var text = '';
        if (formId == 'limit-order-form')
        {
            var amount = $('#form-limit-amount');
            text = 'Type: Limit, Amount: ' + amount.val() + ' ' + amount.attr('x-unit') + ', ';
            var price = $('#form-limit-price');
            text += 'Price: ' + price.val() + ' ' + price.attr('x-unit');
        }
        else
        {
            var amount = $('#form-market-amount');
            text = 'Type: Market, Amount: ' + amount.val() + ' ' + amount.attr('x-unit');
        }

        $('#modal-order-create-p').text(text);
        $('#confirm-order-submit').modal('show')
    };

    $('.form-order-input').on('keypress', function(e) {
        if (e.which == 13) {
            var form = $(this).closest('form');
            showOrderModal(form);
            return false; // dont submit form
        }
    });

    $('.form-create-button').click(function() {
        var form = $(this).closest('form');
        showOrderModal(form);
        form.validate();
        if (!form.valid())
            return;
    });

    $('#confirm-creation').click(function() {
        var formId = $('#modal-table-header').prop('formId');
        $('#' + formId).submit();
    });
});

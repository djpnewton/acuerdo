$(function() {
    $('.form-cancel-button').click(function() {
        var formId = $(this).closest('form').prop('id');
        $('#modal-table-header').prop('formId', formId);

        var header = $(this).closest('table').find('tr').first();
        $('#modal-table-header').html(header.html());

        var cells = $(this).closest('tr').children().slice(0, -1);
        var html = '';
        cells.each(function() { html += $(this).prop('outerHTML'); });
        $('#modal-table-row').html(html);
    });

    $('#confirm-cancellation').click(function() {
        var formId = $('#modal-table-header').prop('formId');
        $('#' + formId).submit();
    });
});

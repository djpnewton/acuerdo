$(function() {
    $('.form-delete-button').click(function() {
        var formId = $(this).closest('form').prop('id');
        $('#modal-device-delete-p').prop('formId', formId);

        var deviceName = $(this).closest('form').attr('x-device-name');
        $('#modal-device-delete-p').text(deviceName);
    });

    $('#confirm-deletion').click(function() {
        var formId = $('#modal-device-delete-p').prop('formId');
        $('#' + formId).submit();
    });
});

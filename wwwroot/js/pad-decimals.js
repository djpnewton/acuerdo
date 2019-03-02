$(function() {
    function getDecimalData(cell) {
        var span = $(cell).children('.pad-decimals-data');
        return span;
    }
    function getDigitParts(cell) {
        var parts = getDecimalData(cell).text().split('.');
        var intDigits = parts[0].trim();
        var fracDigits = '';
        if (parts.length > 1)
            fracDigits = parts[1].trim();
        return {intDigits: intDigits, intDigitsCount: intDigits.length, fracDigits: fracDigits, fracDigitsCount: fracDigits.length};
    }
    $('.pad-decimals').each(function() {
        var idx = $(this).index();
        var cells = $(this).closest('table').find('td:nth-child(' + (idx + 1) + ')').toArray();
        var mostIntDigits = 0;
        for (var i = 0; i < cells.length; i++) {
            var parts = getDigitParts(cells[i]);
            if (parts.intDigitsCount > mostIntDigits)
                mostIntDigits = parts.intDigitsCount;
        }
        cells.forEach(function(cell) {
            var parts = getDigitParts(cell);
            var span = getDecimalData(cell);
            var html = span.html().trim();
            var hiddenSpan = '';
            if (parts.intDigitsCount < mostIntDigits)
                hiddenSpan = '<span class="hidden-digits">' + '0'.repeat(mostIntDigits - parts.intDigitsCount) + '</span>';
            var fracSigDigits = '';
            for (var i = parts.fracDigitsCount - 1; i >= 0; i--) {
                if (parts.fracDigits[i] != '0') {
                    fracSigDigits = parts.fracDigits.substring(0, i + 1);
                    break;
                }
            }
            if (parts.fracDigits.length == 0) {
                var html = hiddenSpan + parts.intDigits;
                span.html(html);
            } else if (fracSigDigits.length == 0) {
                var greyedSpan = '<span class="greyed-digits">.' + '0'.repeat(parts.fracDigitsCount - fracSigDigits.length) + '</span>';
                var html = hiddenSpan + parts.intDigits + greyedSpan;
                span.html(html);
            } else if (fracSigDigits.length < parts.fracDigitsCount) {
                var greyedSpan = '<span class="greyed-digits">' + '0'.repeat(parts.fracDigitsCount - fracSigDigits.length) + '</span>';
                var html = hiddenSpan + parts.intDigits + '.' + fracSigDigits + greyedSpan;
                span.html(html);
            } else {
                var html = hiddenSpan + parts.intDigits + '.' + fracSigDigits;
                span.html(html);
            }
        });
    });
});

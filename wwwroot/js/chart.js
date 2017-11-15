var viachart = (function () {

    var margin = {top: 20, right: 50, bottom: 30, left: 50},
            width = 800 - margin.left - margin.right,
            height = 500 - margin.top - margin.bottom;

    var parseDate = d3.timeParse("%s"); // unix timestamp

    var x = techan.scale.financetime()
            .range([0, width]);

    var y = d3.scaleLinear()
            .range([height, 0]);

    var yVolume = d3.scaleLinear()
            .range([y(0), y(0.2)]);

    var candlestick = techan.plot.candlestick()
            .xScale(x)
            .yScale(y);

    var volume = techan.plot.volume()
            .accessor(techan.accessor.ohlc())   // For volume bar highlighting
            .xScale(x)
            .yScale(yVolume);

    var xAxis = d3.axisBottom()
            .scale(x);

    var yAxisPrice = d3.axisRight()
            .scale(y);

    var yAxisVolume = d3.axisLeft(yVolume)
            .ticks(5)
            .tickFormat(d3.format(",.3s"));

    var ohlcAnnotation = techan.plot.axisannotation()
            .axis(yAxisPrice)
            .orient('right')
            .format(d3.format(',.2f'))
            .translate([width, 0]);

    var timeAnnotation = techan.plot.axisannotation()
            .axis(xAxis)
            .orient('bottom')
            .format(d3.timeFormat('%Y-%m-%d'))
            .width(65)
            .translate([0, height]);

    var closeAnnotation = techan.plot.axisannotation()
                .axis(yAxisPrice)
                .orient('right')
                .accessor(candlestick.accessor())
                .format(d3.format(',.2f'))
                .translate([width, 0]);

    var crosshair = techan.plot.crosshair()
            .xScale(x)
            .yScale(y)
            .xAnnotation([timeAnnotation])
            .yAnnotation([ohlcAnnotation])
            .on("enter", enter)
            .on("out", out)
            .on("move", move);

    var svg = null;
    var coordsText = null;

    function enter() {
        coordsText.style("display", "inline");
    }

    function out() {
        coordsText.style("display", "none");
    }

    function move(coords) {
        coordsText.text(
            timeAnnotation.format()(coords.x) + ", " + ohlcAnnotation.format()(coords.y)
        );
    }

    function draw(data) {
        x.domain(data.map(candlestick.accessor().d));
        y.domain(techan.scale.plot.ohlc(data, candlestick.accessor()).domain());
        yVolume.domain(techan.scale.plot.volume(data).domain());

        svg.selectAll("g.candlestick").datum(data).call(candlestick);
        svg.selectAll("g.volume").datum(data).call(volume);
        svg.selectAll("g.x.axis").call(xAxis);
        svg.selectAll("g.y.axis.price").call(yAxisPrice);
        svg.selectAll("g.y.axis.volume").call(yAxisVolume);
        svg.select("g.close.annotation").datum([data[data.length-1]]).call(closeAnnotation);
    }

    return {

        create: function(selector) {
            svg = d3.select(selector).append("svg")
                    .attr("width", width + margin.left + margin.right)
                    .attr("height", height + margin.top + margin.bottom)
                    .append("g")
                    .attr("transform", "translate(" + margin.left + "," + margin.top + ")");

            coordsText = svg.append('text')
                    .style("text-anchor", "end")
                    .attr("class", "coords")
                    .attr("x", width - 25)
                    .attr("y", 15);

            svg.append("g")
                    .attr("class", "candlestick");

            svg.append("g")
                    .attr("class", "volume");

            svg.append("g")
                    .attr("class", "x axis")
                    .attr("transform", "translate(0," + height + ")");

            svg.append("g")
                    .attr("class", "y axis price")
                    .attr("transform", "translate(" + width + ",0)")
                    .append("text")
                    .attr("transform", "rotate(-90)")
                    .attr("y", -12)
                    .attr("dy", ".71em")
                    .style("text-anchor", "end")
                    .text("Price ($)");

            svg.append("g")
                    .attr("class", "y axis volume")
                    .append("text")
                    .attr("transform", "rotate(-90)")
                    .attr("y", 6)
                    .attr("dy", ".71em")
                    .style("text-anchor", "end")
                    ;//.text("Volume");

            svg.append("g")
                    .attr("class", "close annotation up");

            svg.append('g')
                    .attr("class", "crosshair")
                    .datum({ x: x.domain()[80], y: 67.5 })
                    .call(crosshair)
                    .each(function(d) { move(d); }); // Display the current data
        },

        load: function(data) {
            var accessor = candlestick.accessor();

            data = data.map(function(d) {
                return {
                    date: parseDate(d[0]),
                    open: +d[1],
                    high: +d[3],
                    low: +d[4],
                    close: +d[2],
                    volume: +d[5]
                };
            }).sort(function(a, b) { return d3.ascending(accessor.d(a), accessor.d(b)); });

            draw(data);
        },
    };
})();

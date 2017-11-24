var ws = (function () {

    var sock = null;
    var output = null;
    var callId = 0;

    function log(msg) {
        console.log(msg);
        output.text(output.text() + "\n" + msg);
    }

    function sendMsg(method, paramsList) {
        log("calling: " + method + "(id: " + callId + ") ");
        if (paramsList === undefined)
            paramsList = [];
        else
            log(paramsList)
        var msg = JSON.stringify({id: callId++, method: method, params: paramsList});
        sock.send(msg);
    }

    return {

        start: function(selector, url, token) {
            sock = new WebSocket(url);
            output = $(selector);
            var ping_id = null;
            var ping = function() {
                sendMsg("server.ping");
            }
            sock.onopen = function (event) {
                log("opened ws to " + url);
                sendMsg("server.time");
                if (token !== undefined)
                    sendMsg("server.auth", [token, "viafront"]);
                ping_id = setTimeout(ping, 20000); 
            };
            sock.onmessage = function(event) {
                log(event.data);
                var msg = JSON.parse(event.data);
                if (msg.result == "pong") {
                    ping_id = setTimeout(ping, 20000); 
                }
            }
            sock.onerror = function(event) {
                log("ws error (" + event + ")");
            };
            sock.onclose = function(event) {
                log("ws closed (" + event.code + ")");
                if (ping_id != null) {
                    clearTimeout(ping_id);
                }
                setTimeout(function() { start_ws(); }, 30000);
            };
        }
    };
})();

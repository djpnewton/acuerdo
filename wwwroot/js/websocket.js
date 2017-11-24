var ws = (function () {

    var sock = null;
    var output = null;
    var callId = 0;
    var pendingCalls = [];

    function log(msg) {
        console.log(msg);
        output.text(output.text() + "\n" + msg);
    }

    function sendMsg(method, paramsList) {
        log("calling: " + method + "(id: " + callId + ") ");
        if (paramsList === undefined)
            paramsList = [];
        var msg = JSON.stringify({id: callId++, method: method, params: paramsList});
        sock.send(msg);
    }

    function getPendingCall(id) {
        var call = null;
        for (var i = 0; i < pendingCalls.length; i++)
            if (pendingCalls[i].id == id) {
                call = pendingCalls[i];
                pendingCalls.splice(i, 1);
            }
        return call;
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
                sendMsg("deals.subscribe", ["BTCUSD"]);
                if (token !== undefined) {
                    pendingCalls.push({id: callId, method: "server.auth"});
                    sendMsg("server.auth", [token, "viafront"]);
                }
                ping_id = setTimeout(ping, 60000); 
            };
            sock.onmessage = function(event) {
                log(event.data);
                var msg = JSON.parse(event.data);
                if (msg.result == "pong") {
                    ping_id = setTimeout(ping, 60000); 
                }
                var call = getPendingCall(msg.id);
                if (call != null) {
                    if (call.method == "server.auth" && msg.error == null)
                        sendMsg("order.subscribe", ["BTCUSD"]);
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

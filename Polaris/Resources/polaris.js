
alert(3);

function polaris() {
}

polaris.maxCallId = 0;

polaris.prototype.dispatchSync = function (module, method, parameters) {
    window.polaris.maxCallId++;
    window.polaris._msgQueue[polaris.maxCallId] = retFunc;
    window.polaris.external.SendMessage(module, method, parameters, 1);
}

polaris.prototype.dispatch = function (module, method, parameters, retFunc) {
    window.polaris.maxCallId++;
    window.polaris._msgQueue[polaris.maxCallId] = retFunc;
    polarisConn.sendMessage(module, method, parameters, 1);
}

polaris.prototype._msgQueue = {};

polaris.prototype.pollMessages = function () {
    for (var key in window.polaris._msgQueue) {
        alert(key);
    }
}

if (window.addEventListener)
    window.addEventListener('load', load, false)
else if (window.attachEvent)
    window.attachEvent('onload', load)


function load(){    
    
        if (document.readyState === "complete") {
            document.detachEvent("onreadystatechange", arguments.callee);
            window.polaris = new polaris();
            window.polaris.dispatch("file", "isExists", "[\"e:\\\\jing_setup.exe\"]", null);
        }
}




function polaris() {
    this.maxCallId = 0;
}

polaris.maxCallId = 0;

polaris.prototype.dispatchSync = function (module, method, parameters) {
    var jsonString = polarisConn.sendMessageSync(module, method, parameters);
	return JSON.parse (jsonString);
}

polaris.prototype.pollMessages = function (key,result) {
    func = window.polaris._msgQueue[key];
    func(result);                
}

polaris.prototype.dispatch = function (module, method, parameters, retFunc) {
    window.polaris.maxCallId++;
    window.polaris._msgQueue[window.polaris.maxCallId] = retFunc;
    polarisConn.sendMessage(module, method, parameters, window.polaris.maxCallId);
}

polaris.prototype._msgQueue = {};

if (window.addEventListener)
    window.addEventListener('load', load, false)
else if (window.attachEvent)
    window.attachEvent('onload', load)

function load() {
    
    window.polaris = new polaris();
    try{
        window.loadModules();
    } catch (e) {
        alert(e.message);
    }

	if ( document.onPolarisReady !=null){
		document.onPolarisReady();
	}

}




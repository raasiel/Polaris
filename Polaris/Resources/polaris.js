

function polaris() {
    this.maxCallId = 0;
}

polaris.maxCallId = 0;

polaris.prototype.dispatchSync = function (module, method, parameters) {
    polaris.maxCallId++;
    polaris._msgQueue[polaris.maxCallId] = retFunc;
    window.external.SendMessage(module, method, parameters, maxCallId);
}

polaris.prototype.pollMessages = function (key,result) {
    func = window.polaris._msgQueue[key];
    func(result);                
}

polaris.prototype.dispatch = function (module, method, parameters, retFunc) {
    window.polaris.maxCallId++;
    window.polaris._msgQueue[window.polaris.maxCallId] = retFunc;
    polarisConn.sendMessage(module, method, parameters, window.polaris.maxCallId);
    //polarisConn.dox();
}

/*
function file() { };
file.prototype.isExists = function (filename, retFunc) {
    window.polaris.dispatch("file", "isExists",filename, retFunc);
}
polaris.prototype.file = new file ();            
*/

polaris.prototype._msgQueue = {};

if (window.addEventListener)
    window.addEventListener('load', load, false)
else if (window.attachEvent)
    window.attachEvent('onload', load)


function load() {
                             
    window.polaris = new polaris();
    //alert(1);
    try{
        window.loadModules();
    } catch (e) {
        alert(e.message);
    }
    alert(window.polaris.file);
    window.polaris.file.getFiles("f:\\Users\\shafqat\\Downloads\\", "*.ipk", function (result) {
        alert("result is " + result.toString());
    });
    alert("called");
}

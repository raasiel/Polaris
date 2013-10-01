


function polaris() {
}

polaris.maxCallId = 0;


polaris.prototype.dispatch = function (module, method, parameters, retFunc) {
    window.external.SendMessage(module, method, parameters,1);
}




if ( document.attachEvent ) 
{
    document.attachEvent("onreadystatechange", function () {
        if (document.readyState === "complete") {
            document.detachEvent("onreadystatechange", arguments.callee);
            window.polaris = new polaris();
            window.polaris.dispatch("file", "isExists", "[\"e:\\\\jing_setup.exe\"]");
        }
    });
}
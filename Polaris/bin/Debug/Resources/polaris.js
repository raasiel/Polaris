


function polaris() {

}

polaris.prototype.dispatch = function (module, method, parameters) {
    window.external.SendMessage(module, method, parameters);
}



if ( document.attachEvent ) 
{
    document.attachEvent("onreadystatechange", function () {
        if (document.readyState === "complete") {
            document.detachEvent("onreadystatechange", arguments.callee);
            window.polaris = new polaris();
            window.polaris.dispatch("file", "isExists", "message");
        }
    });
}
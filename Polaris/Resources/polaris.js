


function polaris() {

    //prefix = "__pls_3635_wq_";


    // set up the dispacher
    div = document.createElement("div");
    btn = document.createElement("button");
    div.appendChild(btn);
    div.setAttribute("style", "display:none;");
    hidSend = document.createElement("hidden");
    div.appendChild(hidSend);
    document.appendChild(div);

    this.toMsg = hidSend;
    this.send = btn;
}

//polaris.prototype.dispatch = new function (message) {
    //window.polaris.toMsg = message;
    //window.polaris.send.click();
//}



if ( document.attachEvent ) 
{
    document.attachEvent("onreadystatechange", function () {
        if (document.readyState === "complete") {
            document.detachEvent("onreadystatechange", arguments.callee);
            //window.polaris = new polaris();
            //window.external.SendMessage("yo!");
            alert(window.external.Host);
        }
    });
}
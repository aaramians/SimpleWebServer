var ws = null
var i = 0;





function ServerEventSourceTest() {
    if (typeof (EventSource) !== "undefined") {
        var source = new EventSource("TestSSE");
        source.addEventListener('message', function (event) {
            document.getElementById("result").innerHTML += event.data + "<br>";
        });
        source.addEventListener('customevent', function (event) {
            document.getElementById("result").innerHTML += event.data + "<br>";
        });
    } else {
        alert("EventSource NOT supported by your Browser!");
    }
}

function WebSocketTest() {

    // The browser doesn't support WebSocket
    if (typeof (EventSource) !== "undefined") {
        if (ws === null) {
            // Let us open a web socket
            ws = new WebSocket("ws://" + location.host + "/TestWS");

            ws.onopen = function () {
                console.log("Socket is open...");
                setTimeout(() => {
                    // Web Socket is connected, send data using send()
                    ws.send('Testing WebSocket');
                }, 1000);

            };

            ws.onmessage = function (evt) {
                console.log(evt);
                var received_msg = evt.data;
                document.getElementById("result").innerHTML += received_msg + "<br>";
            };

            ws.onclose = function () {
                // websocket is closed.
                console.log("Connection is closed...");

            };

        }
        else {
            ws.send('Testing WebSocket');
        }
    }
    else {
        alert("WebSocket NOT supported by your Browser!");
    }

}
function getBase64(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.readAsDataURL(file);
        reader.onload = () => resolve(reader.result);
        reader.onerror = error => reject(error);
    });
}

function AjaxTest() {
    $.ajax({
        type: "POST",
        url: 'TestAjax',
        data: { param1: "param 1", param2: "param+2%X", param3: "param&3" },
        success: function (received_msg) {
            document.getElementById("result").innerHTML += received_msg + "<br>";
        },
    });
}

function AjaxTestJson(test) {

    var file = document.querySelector('input[type="file"]').files[0];


    var file = document.querySelector('input[type="file"]').files[0];
    getBase64(file).then(
        data =>
            $.ajax({
                type: "POST",
                url: 'TestAjax',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify({ param1: "param 1", param2: "param2", param3: data }),
                success: function (received_msg) {
                    document.getElementById("result").innerHTML += received_msg + "<br>";
                },
            })
    );



    //document.getElementById('div-video').innerHTML = '<video width="400" controls><source src="movie.mp4" type="video/mp4">Your browser does not support HTML5 video.</video>';

    // http://api.jquery.com/jquery.ajax/ see for more details
    // For cross-domain requests, setting the content type to anything other than application/x-www-form-urlencoded, multipart/form-data, or text/plain will trigger the browser to send a preflight OPTIONS request to the server.
    // Ajax requests are sent using the GET HTTP method, post is UTF-8 charset
    // if dictionary; jQuery.param() is used
    // converters (default: {"* text": window.String, "text html": true, "text json": jQuery.parseJSON, "text xml": jQuery.parseXML})
    // dataType (default: Intelligent Guess (xml, json, script, or html))

    // $.ajax({
    //    type: "POST",
    //    url: 'TestAjax',
    //    data: { param1: "param 1", param2: "param+2%X", param3: "param&3" },
    //    success: function (received_msg) {
    //       document.getElementById("result").innerHTML += received_msg + "<br>";
    //    },
    // });


    // var formData = new FormData();

    // formData.append("username", "Groucho");
    // formData.append("accountnum", 123456); // number 123456 is immediately converted to a string "123456"

    // fileInputElement = document.getElementById('filetoupload')
    // // HTML file input, chosen by user
    // formData.append("userfile", fileInputElement.files[0]);

    // // JavaScript file-like object
    // var content = '<a id="a"><b id="b">hey!</b></a>'; // the body of the new file...
    // var blob = new Blob([content], { type: "text/xml" });

    // // formData.append("webmasterfile", blob);

    // var request = new XMLHttpRequest();
    // request.open("POST", "TestPost");
    // request.send(formData);


}

function VideoTest() {
    x = '<video width="100%" controls><source src="content/video.mp4?1=2" type="video/mp4">Your browser does not support HTML5 video.</video>';
    $('#videotest').html(x);
}

$("form").submit(function (e) {
    //e.preventDefault();
    enctype = ($("form [name='enctype']").val())
    method = ($("form [name='method']").val())
    $("form").attr("method", method);
    $("form").attr("enctype", enctype);
    //return false;
});


function uploadFile() {
    var file = document.getElementById("file1").files[0];
    // alert(file.name+" | "+file.size+" | "+file.type);
    var formdata = new FormData();
    formdata.append("file1", file);
    var ajax = new XMLHttpRequest();
    ajax.upload.addEventListener("progress", progressHandler, false);
    ajax.addEventListener("load", completeHandler, false);
    ajax.addEventListener("error", errorHandler, false);
    ajax.addEventListener("abort", abortHandler, false);
    ajax.open("POST", "file_upload_parser.php");
    ajax.send(formdata);
}
function progressHandler(event) {
    document.getElementById("loaded_n_total").innerHTML = "Uploaded " + event.loaded + " bytes of " + event.total;
    var percent = (event.loaded / event.total) * 100;
    document.getElementById("progressBar").value = Math.round(percent);
    document.getElementById("status").innerHTML = Math.round(percent) + "% uploaded... please wait";
}
function completeHandler(event) {
    document.getElementById("status").innerHTML = event.target.responseText;
    document.getElementById("progressBar").value = 0;
}
function errorHandler(event) {
    document.getElementById("status").innerHTML = "Upload Failed";
}
function abortHandler(event) {
    document.getElementById("status").innerHTML = "Upload Aborted";
}

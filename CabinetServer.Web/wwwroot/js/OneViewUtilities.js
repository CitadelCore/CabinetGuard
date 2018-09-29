function validateOneView() {
    var hostname = document.getElementById("hostname");
    var username = document.getElementById("username");
    var password = document.getElementById("password");

    getOneViewSessionID(hostname, username, password);
}

function getOneViewSessionID(hostname, username, password) {
    $.ajax({
        url: "https://" + hostname + "/api/rest/login-sessions",
        dataType: 'jsonp',
        success: function (json) {
            var _result = JSON.parse(json);
            return _result.sessionID;
        },
        error: function () {
            $.notify({ message: 'Could not contact HPE OneView. Please verify your credentials and try again.', icon: 'fa fa-times' }, { type: 'danger', animate: { enter: 'animated fadeInRight', exit: 'animated fadeOutRight' } });
            return null;
        }
    });
}
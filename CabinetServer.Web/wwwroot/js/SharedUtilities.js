function queueCommand(id, name, payload, btn) {
    var options = {};
    options.url = "/api/Commands/ScheduleFor/" + id;
    options.type = "POST";

    var command = {};
    command.name = name;
    command.payload = payload;

    options.data = JSON.stringify(command);
    options.dataType = "html";
    options.contentType = "application/json";

    options.success = function (data, msg, xhr) {
        var _command = JSON.parse(data);

        if (_command.error !== null) {
            if (btn !== null) {
                $(btn).removeClass("disabled");
            }

            $.notify({ message: _command.error, icon: 'fa fa-times' }, { type: 'danger', animate: { enter: 'animated fadeInRight', exit: 'animated fadeOutRight' } });
        } else {
            $.notify({ message: 'Successfully queued the command ' + _command.name, icon: 'fa fa-check' }, { type: 'success', animate: { enter: 'animated fadeInRight', exit: 'animated fadeOutRight' } });
            window.setTimeout(checkCommandCompletion.bind(null, _command.id, btn), 2000);
        }
    };

    options.error = function (xhr, status, error) {
        if (btn !== null) {
            $(btn).removeClass("disabled");
        }

        $.notify({ message: 'An error occurred while sending the command to the API.', icon: 'fa fa-times' }, { type: 'danger', animate: { enter: 'animated fadeInRight', exit: 'animated fadeOutRight' } });
    };

    if (btn !== null) {
        $(btn).addClass("disabled");
    }

    $.ajax(options);
}

function checkCommandCompletion(cmdId, btn) {
    var options = {};
    options.url = "/api/Commands/" + cmdId;
    options.type = "GET";

    options.dataType = "html";
    options.contentType = "application/json";

    options.success = function (data, msg, xhr) {
        var _command = JSON.parse(data);

        if (_command.error !== null) {
            if (btn !== null) {
                $(btn).removeClass("disabled");
            }

            $.notify({ message: _command.error, icon: 'fa fa-times' }, { type: 'danger', animate: { enter: 'animated fadeInRight', exit: 'animated fadeOutRight' } });
        } else if (_command.state == 3 && _command.result == 2) {
            window.setTimeout(forceRefresh, 2000);
            $.notify({ message: 'The command ' + _command.name + ' has completed successfully.', icon: 'fa fa-check' }, { type: 'success', animate: { enter: 'animated fadeInRight', exit: 'animated fadeOutRight' } });
        } else if (_command.state == 2 || _command.state == 1) {
            window.setTimeout(checkCommandCompletion.bind(null, cmdId, btn), 2000);
        } else if (_command.result == 5) {
            if (btn !== null) {
                $(btn).removeClass("disabled");
            }
            $.notify({ message: 'The command ' + _command.name + ' has taken too long and has been cancelled.', icon: 'fa fa-exclamation-triangle' }, { type: 'warning', animate: { enter: 'animated fadeInRight', exit: 'animated fadeOutRight' } });
        } else if (_command.result == 3 || _command.result == 1 || _command.result == 4) {
            if (btn !== null) {
                $(btn).removeClass("disabled");
            }
            $.notify({ message: "An internal execution error occurred.", icon: 'fa fa-times' }, { type: 'danger', animate: { enter: 'animated fadeInRight', exit: 'animated fadeOutRight' } });
        } else {
            if (btn !== null) {
                $(btn).removeClass("disabled");
            }
            $.notify({ message: "An internal execution error occurred.", icon: 'fa fa-times' }, { type: 'danger', animate: { enter: 'animated fadeInRight', exit: 'animated fadeOutRight' } });
        }
    };

    $.ajax(options);
}

function forceRefresh() {
    location.reload();
}
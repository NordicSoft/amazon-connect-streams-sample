(function ($) {
    window.myCPP = window.myCPP || {};


    //https://github.com/aws/amazon-connect-streams/blob/master/Documentation.md - doc for integration
    ///
    //https://blogs.perficient.com/2017/11/06/amazon-connect-javascript-libraries-lily-ccp-streams-and-connect-rtc/ - post about additional js files which loaded with that one amazon-connect.js
    ///initalize iframe with CCP

    var ccpUrl = "https://AmazonConnectInstanceName.awsapps.com/connect/ccp#/" //your cppurl

    //also you have to whitelist your domain https://github.com/aws/amazon-connect-streams/issues/25#issuecomment-362032554
    connect.core.initCCP($("#containerDiv")[0], {
        //ccpUrl: adfsURL,
        ccpUrl: ccpUrl,
        loginPopup: false,
        softphone: {
            allowFramedSoftphone: true
        }
    });


    //actions when contact or agent initialized
    connect.contact(subscribeToContactEvents);
    connect.agent(subscribeToAgentEvents);

    function subscribeToContactEvents(contact) {
        window.myCPP.contact = contact;
        logInfoMsg("Subscribing to events for contact");
        if (contact.getActiveInitialConnection()
            && contact.getActiveInitialConnection().getEndpoint()) {
            logInfoMsg("New contact number: " + contact.getActiveInitialConnection().getEndpoint().phoneNumber);
            var eOutboundApiCall = new CustomEvent('OutboundApiCall', { 'detail': contact.getActiveInitialConnection().getEndpoint().phoneNumber });
            window.dispatchEvent(eOutboundApiCall);
            //TODO: get info about user from DB by number
        } else {
            logInfoMsg("This is an existing contact for this agent");
        }
        logInfoMsg("Contact is from queue " + contact.getQueue().name);
        logInfoMsg("Contact attributes are " + JSON.stringify(contact.getAttributes()));
        contact.onIncoming(handleContactIncoming);
        contact.onAccepted(handleContactAccepted);
        contact.onConnected(handleContactConnected);
        contact.onEnded(handleContactEnded);
    }
    function handleContactIncoming(contact) {
        if (contact) {
            logInfoEvent("[contact.onIncoming] Contact is incoming. Contact state is " + contact.getStatus().type);
        } else {
            logInfoEvent("[contact.onIncoming] Contact is incoming. Null contact passed to event handler");
        }
    }
    function handleContactAccepted(contact) {
        if (contact) {
            logInfoEvent("[contact.onAccepted] Contact accepted by agent. Contact state is " + contact.getStatus().type);
        } else {
            logInfoEvent("[contact.onAccepted] Contact accepted by agent. Null contact passed to event handler");
        }
    }
    function handleContactConnected(contact) {
        if (contact) {
            logInfoEvent("[contact.onConnected] Contact connected to agent. Contact state is " + contact.getStatus().type);
        } else {
            logInfoEvent("[contact.onConnected] Contact connected to agent. Null contact passed to event handler");
        }
    }
    function handleContactEnded(contact) {
        if (contact) {
            logInfoEvent("[contact.onEnded] Contact has ended. Contact state is " + contact.getStatus().type);
        } else {
            logInfoEvent("[contact.onEnded] Contact has ended. Null contact passed to event handler");
        }
    }


    function subscribeToAgentEvents(agent) {
        window.myCPP.agent = agent;
        $(".jumbotron h1").text(agent.getName());
        logInfoMsg("Subscribing to events for agent " + agent.getName());
        logInfoMsg("Agent is currently in status of " + agent.getStatus().name);
        displayAgentStatus(agent.getStatus().name);
        agent.onRefresh(handleAgentRefresh);
        agent.onRoutable(handleAgentRoutable);
        agent.onNotRoutable(handleAgentNotRoutable);
        agent.onOffline(handleAgentOffline);
    }
    function handleAgentRefresh(agent) {
        logInfoEvent("[agent.onRefresh] Agent data refreshed. Agent status is " + agent.getStatus().name);
        displayAgentStatus(agent.getStatus().name);
    }
    function handleAgentRoutable(agent) {
        logInfoEvent("[agent.onRoutable] Agent is routable. Agent status is " + agent.getStatus().name);
        displayAgentStatus(agent.getStatus().name);
    }
    function handleAgentNotRoutable(agent) {
        logInfoEvent("[agent.onNotRoutable] Agent is online, but not routable. Agent status is " + agent.getStatus().name);
        displayAgentStatus(agent.getStatus().name);
    }
    function handleAgentOffline(agent) {
        logInfoEvent("[agent.onOffline] Agent is offline. Agent status is " + agent.getStatus().name);
        displayAgentStatus(agent.getStatus().name);
    }
    function logMsgToScreen(msg) {
        logMsgs.innerHTML = '<div>' + new Date().toLocaleTimeString() + ' ' + msg + '</div>' + logMsgs.innerHTML;
    }
    function logEventToScreen(msg) {
        eventMsgs.innerHTML = '<div>' + new Date().toLocaleTimeString() + ' ' + msg + '</div>' + eventMsgs.innerHTML;
    }
    function logInfoMsg(msg) {
        connect.getLog().info(msg);
        logMsgToScreen(msg);
    }
    function logInfoEvent(eventMsg) {
        connect.getLog().info(eventMsg);
        logEventToScreen(eventMsg);
    }



    function displayAgentStatus(status) {
        ///possile to send this status on the server for monitoring;
        agentStatusDiv.innerHTML = 'Status: <span style="font-weight: bold">' + status + '</span>';
    }

    function addConnection(number) {
        var endpoint = connect.Endpoint.byPhoneNumber(number);
        connect.agent(function (agent) {
            console.log('Making outbound call to: ' + number);
            agent.connect(endpoint, {
                success: function (connectionId) { // connectionId available here as an argument
                    console.log('Make call success!');
                },
                failure: function () {
                    console.log('Make call fail!');
                }
            });
        });
    };

    function acceptContact() {
        window.myCPP.contact.accept({
            success: function () {
                logInfoMsg("Accepted contact via Streams");
            },
            failure: function () {
                logInfoMsg("Failed to accept contact via Streams");
            }
        });
    }

    $("#start-ccp-call").on("click", function () {
        var number = $("input[name=number-ccp]").val();
        addConnection(number);
    });

    $("#stop-call").prop("disabled", true);

    $("#start-call-form").on("submit", function (e) {
        e.preventDefault();
        var data = {
            number: $("input[name=number]").val(),
            idContactFlow: $("#contact-flow-id option:selected").val(),
            sourcePhoneNumber: $("#source-number option:selected").val()
        }
        $.post("/Home/InitOutboundCall", data, function (resp) {
            if (resp.success) {
                window.addEventListener('OutboundApiCall', function acceptOutboundApiCall(e) {
                    if (data.number === e.detail) {
                        acceptContact();
                        window.removeEventListener("OutboundApiCall", load, false);
                    }
                }, false);
                $("#start-call").val("Calling..");
                $("#start-call").prop("disabled", true);
                $("#stop-call").prop("disabled", false);
                $("#stop-call").data("contactId", resp.data);
                setTimeout(function () {
                    $("#start-call").val("Call");
                    $("#start-call").prop("disabled", false);

                }, 60000);
            }
            else {
                console.log(resp.data.Message);
                console.log(resp.data.InnerException.Message);
            }
        });
    });


    $("#stop-call").on("click", function (e) {
        var contactId = $(e.currentTarget).data("contactId");
        if (!contactId) {
            return false;
        }
        var data = { contactId: contactId };
        $.post("/Home/StopOutboundCall", data, function (resp) {
            if (resp.success) {
                $("#start-call").val("Call");
                $("#start-call").prop("disabled", false);

                $("#stop-call").prop("disabled", true);
                $("#stop-call").removeData("contactId");
            } else {
                console.log(resp.data.Message);
                console.log(resp.data.InnerException.Message);
            }
        });
    });


})(jQuery)
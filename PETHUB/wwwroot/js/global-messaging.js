// =========================================================
// GLOBAL CHAT SIGNALR CONNECTION
// =========================================================

window.chatConnection =
    new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect()
        .build();


// =========================================================
// GLOBAL MESSAGE NOTIFICATION
// Updates the main system sidebar badge.
// =========================================================

window.chatConnection.on(
    "MessageNotification",
    function (message) {

        console.log(
            "Global MessageNotification:",
            message
        );


        // If the user is currently viewing
        // the conversation that received the message,
        // don't increase the global unread count.
        const currentConversationIdElement =
            document.getElementById(
                "currentConversationId"
            );


        const currentConversationId =
            currentConversationIdElement
                ? parseInt(
                    currentConversationIdElement.value
                )
                : null;


        if (
            currentConversationId ===
            message.conversationId
        ) {
            return;
        }


        const badge =
            document.getElementById(
                "globalMessagesUnreadBadge"
            );


        if (!badge) {
            return;
        }


        let currentCount =
            parseInt(
                badge.textContent
            ) || 0;


        currentCount++;


        badge.textContent =
            currentCount > 99
                ? "99+"
                : currentCount;


        badge.classList.remove(
            "d-none"
        );
    }
);


// =========================================================
// START CONNECTION
// =========================================================

window.chatConnectionReady =
    window.chatConnection
        .start()
        .then(function () {

            console.log(
                "Global chat SignalR connected."
            );

        })
        .catch(function (error) {

            console.error(
                "Global chat SignalR connection error:",
                error
            );

            throw error;
        });
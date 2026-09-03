document.addEventListener("DOMContentLoaded", function () {

    // =========================================================
    // 1. PAGE ELEMENTS
    // =========================================================

    const searchInput =  document.getElementById("conversationSearch");

    const messageSendForm = document.getElementById("messageSendForm");

    const messageInput = document.querySelector(".chat-message-input");

    const messageImageInput = document.getElementById("messageImageInput");

    const sendButton = document.querySelector(".chat-send-button");

    const conversationIdElement = document.getElementById("currentConversationId");

    const currentUserId = document.getElementById("currentUserId")?.value;

    const chatMessages = document.querySelector(".chat-messages");

    const typingIndicator = document.getElementById("typingIndicator");

    const isArchiveView = document.getElementById("isArchiveView")?.value === "true";

    // Reuse the global SignalR connection.
    const connection = window.chatConnection;


    // =========================================================
    // 2. CONVERSATION SEARCH + FILTERS
    // =========================================================

    const conversationFilterButtons =
        document.querySelectorAll(
            ".conversation-filter"
        );


    let activeConversationFilter =
        "all";


    function filterConversations() {

        const searchValue =
            searchInput
                ? searchInput.value
                    .trim()
                    .toLowerCase()
                : "";


        const conversationItems =
            document.querySelectorAll(
                ".conversation-item"
            );


        conversationItems.forEach(
            function (conversation) {

                // ---------------------------------------------
                // SEARCH DATA
                // ---------------------------------------------

                const name =
                    conversation.dataset.searchName
                        ?.toLowerCase() || "";

                const context =
                    conversation.dataset.searchContext
                        ?.toLowerCase() || "";

                const title =
                    conversation.dataset.searchTitle
                        ?.toLowerCase() || "";

                const message =
                    conversation.dataset.searchMessage
                        ?.toLowerCase() || "";


                // ---------------------------------------------
                // SEARCH MATCH
                // ---------------------------------------------

                const matchesSearch =
                    name.includes(searchValue) ||
                    context.includes(searchValue) ||
                    title.includes(searchValue) ||
                    message.includes(searchValue);


                // ---------------------------------------------
                // FILTER MATCH
                // ---------------------------------------------

                const matchesFilter =
                    activeConversationFilter === "all" ||
                    conversation.dataset.searchContext ===
                    activeConversationFilter;


                // ---------------------------------------------
                // FINAL RESULT
                // ---------------------------------------------

                conversation.style.display =
                    matchesSearch &&
                        matchesFilter
                        ? "flex"
                        : "none";
            }
        );
    }


    // =========================================================
    // SEARCH INPUT
    // =========================================================

    if (searchInput) {

        searchInput.addEventListener(
            "input",
            filterConversations
        );
    }


    // =========================================================
    // FILTER BUTTONS
    // =========================================================

    conversationFilterButtons.forEach(
        function (button) {

            button.addEventListener(
                "click",
                function () {

                    activeConversationFilter =
                        button.dataset
                            .conversationFilter;


                    // Remove active state
                    // from every button.
                    conversationFilterButtons
                        .forEach(
                            function (filterButton) {

                                filterButton
                                    .classList
                                    .remove(
                                        "active"
                                    );
                            }
                        );


                    // Highlight selected filter.
                    button.classList.add(
                        "active"
                    );


                    // Re-run filtering.
                    filterConversations();
                }
            );
        }
    );


    // =========================================================
    // 3. MESSAGE IMAGE PREVIEW
    // =========================================================

    const previewInput =
        document.getElementById(
            "messageImageInput"
        );

    const previewContainer =
        document.getElementById(
            "messageImagePreview"
        );


    if (
        previewInput &&
        previewContainer &&
        typeof setupImagePreview === "function"
    ) {

        setupImagePreview(
            "messageImageInput",
            "messageImagePreview",
            {
                multiple: true,
                maxFiles: 5,
                previewWidth: 72,
                previewHeight: 72
            }
        );
    }


    // =========================================================
    // 4. SENT IMAGE LIGHTBOX
    // =========================================================

    const lightbox = document.getElementById("messageImageLightbox");

    const lightboxImage = document.getElementById("messageLightboxImage");

    const lightboxClose = document.getElementById("messageLightboxClose");

    const lightboxPrevious = document.getElementById("messageLightboxPrevious");

    const lightboxNext = document.getElementById("messageLightboxNext");

    const lightboxCounter = document.getElementById("messageLightboxCounter");


    let currentImages = [];
    let currentImageIndex = 0;


    function updateLightbox() {

        if (
            !lightboxImage ||
            currentImages.length === 0
        ) {
            return;
        }


        lightboxImage.src =
            currentImages[currentImageIndex];


        if (lightboxCounter) {

            lightboxCounter.textContent =
                `${currentImageIndex + 1} / ${currentImages.length}`;
        }


        const navigationDisplay =
            currentImages.length > 1
                ? "flex"
                : "none";


        if (lightboxPrevious) {
            lightboxPrevious.style.display =
                navigationDisplay;
        }


        if (lightboxNext) {
            lightboxNext.style.display =
                navigationDisplay;
        }
    }


    function openLightbox(
        images,
        index
    ) {

        if (
            !lightbox ||
            !lightboxImage ||
            !Array.isArray(images) ||
            images.length === 0
        ) {
            return;
        }


        currentImages =
            images;


        currentImageIndex =
            Math.max(
                0,
                Math.min(
                    index,
                    images.length - 1
                )
            );


        updateLightbox();


        lightbox.classList.add(
            "open"
        );


        document.body.style.overflow =
            "hidden";
    }


    function closeLightbox() {

        if (!lightbox) {
            return;
        }


        lightbox.classList.remove(
            "open"
        );


        document.body.style.overflow =
            "";
    }


    // Existing Razor-rendered images.
    document
        .querySelectorAll(
            ".message-image-button"
        )
        .forEach(
            function (button) {

                button.addEventListener(
                    "click",
                    function () {

                        let images = [];


                        try {

                            images =
                                JSON.parse(
                                    button.dataset.messageImages
                                    || "[]"
                                );

                        }
                        catch {

                            images = [];
                        }


                        const index =
                            parseInt(
                                button.dataset.imageIndex
                                || "0"
                            );


                        openLightbox(
                            images,
                            index
                        );
                    }
                );
            }
        );


    lightboxPrevious?.addEventListener(
        "click",
        function () {

            if (
                currentImages.length === 0
            ) {
                return;
            }


            currentImageIndex =
                (
                    currentImageIndex
                    - 1
                    + currentImages.length
                )
                % currentImages.length;


            updateLightbox();
        }
    );


    lightboxNext?.addEventListener(
        "click",
        function () {

            if (
                currentImages.length === 0
            ) {
                return;
            }


            currentImageIndex =
                (
                    currentImageIndex
                    + 1
                )
                % currentImages.length;


            updateLightbox();
        }
    );


    lightboxClose?.addEventListener(
        "click",
        closeLightbox
    );


    lightbox?.addEventListener(
        "click",
        function (event) {

            if (
                event.target === lightbox
            ) {

                closeLightbox();
            }
        }
    );


    document.addEventListener(
        "keydown",
        function (event) {

            if (
                !lightbox ||
                !lightbox.classList.contains(
                    "open"
                )
            ) {
                return;
            }


            if (
                event.key === "Escape"
            ) {

                closeLightbox();

                return;
            }


            if (
                event.key === "ArrowRight" &&
                currentImages.length > 1
            ) {

                currentImageIndex =
                    (
                        currentImageIndex
                        + 1
                    )
                    % currentImages.length;


                updateLightbox();
            }


            if (
                event.key === "ArrowLeft" &&
                currentImages.length > 1
            ) {

                currentImageIndex =
                    (
                        currentImageIndex
                        - 1
                        + currentImages.length
                    )
                    % currentImages.length;


                updateLightbox();
            }
        }
    );


    // =========================================================
    // 5. GENERAL HELPERS
    // =========================================================

    function getCurrentConversationId() {

        if (!conversationIdElement) {
            return null;
        }


        const conversationId =
            parseInt(
                conversationIdElement.value
            );


        return Number.isNaN(
            conversationId
        )
            ? null
            : conversationId;
    }


    function formatMessageTime(
        createdAt
    ) {

        const date =
            new Date(
                createdAt
            );


        if (
            Number.isNaN(
                date.getTime()
            )
        ) {
            return "";
        }


        return date.toLocaleTimeString(
            [],
            {
                hour: "numeric",
                minute: "2-digit"
            }
        );
    }


    function getMessagePreview(
        message,
        isMine
    ) {

        if (
            message.content &&
            message.content.trim() !== ""
        ) {

            return message.content;
        }


        const imageCount =
            Array.isArray(
                message.imagePaths
            )
                ? message.imagePaths.length
                : 0;


        if (
            imageCount === 1
        ) {

            return isMine
                ? "You sent a photo"
                : "Sent a photo";
        }


        if (
            imageCount > 1
        ) {

            return isMine
                ? `You sent ${imageCount} photos`
                : `Sent ${imageCount} photos`;
        }


        return "";
    }


    function incrementConversationUnread(
        conversationId
    ) {

        const badge =
            document.querySelector(
                `[data-conversation-unread="${conversationId}"]`
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


    function showArchivedEmptyStateIfNeeded() {

        if (!isArchiveView) {
            return;
        }

        const conversationList =
            document.querySelector(
                ".conversation-list"
            );

        if (!conversationList) {
            return;
        }

        const remainingConversations =
            conversationList.querySelectorAll(
                ".conversation-item"
            );

        if (remainingConversations.length > 0) {
            return;
        }

        const existingEmptyState =
            conversationList.querySelector(
                ".messages-empty-list"
            );

        if (existingEmptyState) {
            return;
        }


        const emptyState =
            document.createElement(
                "div"
            );

        emptyState.classList.add(
            "messages-empty-list"
        );


        const icon =
            document.createElement(
                "i"
            );

        icon.setAttribute(
            "data-lucide",
            "archive"
        );


        const message =
            document.createElement(
                "p"
            );

        message.textContent =
            "No archived chats.";


        emptyState.appendChild(
            icon
        );

        emptyState.appendChild(
            message
        );


        conversationList.appendChild(
            emptyState
        );


        if (window.lucide) {
            lucide.createIcons();
        }
    }


    // =========================================================
    // 6. CREATE BRAND-NEW CONVERSATION ITEM
    // =========================================================

    function createConversationItem(
        message
    ) {

        const conversationList =
            document.querySelector(
                ".conversation-list"
            );


        if (!conversationList) {
            return null;
        }


        const conversationItem =
            document.createElement(
                "a"
            );


        conversationItem.classList.add(
            "conversation-item"
        );


        conversationItem.href =
            `/Messages?conversationId=${message.conversationId}`;


        conversationItem.dataset.conversationId =
            message.conversationId;


        conversationItem.dataset.searchName =
            message.senderName || "";


        conversationItem.dataset.searchContext =
            message.contextType || "";


        conversationItem.dataset.searchTitle =
            message.contextTitle || "";


        conversationItem.dataset.searchMessage =
            message.content || "";


        // -----------------------------------------------------
        // Avatar
        // -----------------------------------------------------

        const avatar =
            document.createElement(
                "div"
            );


        avatar.classList.add(
            "conversation-avatar"
        );


        if (
            message.senderProfilePicture
        ) {

            const image =
                document.createElement(
                    "img"
                );


            image.src =
                message.senderProfilePicture;


            image.alt =
                message.senderName
                || "Member";


            avatar.appendChild(
                image
            );

        }
        else {

            const icon =
                document.createElement(
                    "i"
                );


            icon.setAttribute(
                "data-lucide",
                "user"
            );


            avatar.appendChild(
                icon
            );
        }


        // -----------------------------------------------------
        // Content container
        // -----------------------------------------------------

        const content =
            document.createElement(
                "div"
            );


        content.classList.add(
            "conversation-content"
        );


        // -----------------------------------------------------
        // Top row
        // -----------------------------------------------------

        const topRow =
            document.createElement(
                "div"
            );


        topRow.classList.add(
            "conversation-top-row"
        );


        const name =
            document.createElement(
                "span"
            );


        name.classList.add(
            "conversation-name"
        );


        name.textContent =
            message.senderName
            || "Member";


        const meta =
            document.createElement(
                "div"
            );


        meta.classList.add(
            "conversation-meta"
        );


        const unread =
            document.createElement(
                "span"
            );


        unread.classList.add(
            "conversation-unread-badge",
            "d-none"
        );


        unread.dataset.conversationUnread =
            message.conversationId;


        unread.textContent =
            "0";


        const time =
            document.createElement(
                "span"
            );


        time.classList.add(
            "conversation-time"
        );


        meta.appendChild(
            unread
        );


        meta.appendChild(
            time
        );


        topRow.appendChild(
            name
        );


        topRow.appendChild(
            meta
        );


        // -----------------------------------------------------
        // Context
        // -----------------------------------------------------

        const context =
            document.createElement(
                "div"
            );


        context.classList.add(
            "conversation-context"
        );


        const contextIcon =
            document.createElement(
                "i"
            );


        contextIcon.setAttribute(
            "data-lucide",
            message.contextType
                === "Marketplace"
                ? "shopping-bag"
                : "search"
        );


        const contextType =
            document.createElement(
                "span"
            );


        contextType.textContent =
            message.contextType
                === "Marketplace"
                ? "Marketplace"
                : "Lost & Found";


        const dot =
            document.createElement(
                "span"
            );


        dot.classList.add(
            "conversation-context-dot"
        );


        dot.textContent =
            "·";


        const title =
            document.createElement(
                "span"
            );


        title.classList.add(
            "conversation-context-title"
        );


        title.textContent =
            message.contextTitle || "";


        context.appendChild(
            contextIcon
        );


        context.appendChild(
            contextType
        );


        context.appendChild(
            dot
        );


        context.appendChild(
            title
        );


        // -----------------------------------------------------
        // Preview
        // -----------------------------------------------------

        const preview =
            document.createElement(
                "div"
            );


        preview.classList.add(
            "conversation-preview"
        );


        // -----------------------------------------------------
        // Build conversation item
        // -----------------------------------------------------

        content.appendChild(
            topRow
        );


        content.appendChild(
            context
        );


        content.appendChild(
            preview
        );


        conversationItem.appendChild(
            avatar
        );


        conversationItem.appendChild(
            content
        );


        conversationList.prepend(
            conversationItem
        );


        if (window.lucide) {

            lucide.createIcons();
        }


        return conversationItem;
    }


    // =========================================================
    // 7. UPDATE CONVERSATION PREVIEW
    // =========================================================

    function updateConversationPreview(
        message,
        isMine
    ) {

        const conversationItem =
            document.querySelector(
                `.conversation-item[data-conversation-id="${message.conversationId}"]`
            );


        if (!conversationItem) {
            return;
        }


        const preview =
            conversationItem.querySelector(
                ".conversation-preview"
            );


        if (preview) {

            preview.textContent =
                getMessagePreview(
                    message,
                    isMine
                );
        }


        const time =
            conversationItem.querySelector(
                ".conversation-time"
            );


        if (time) {

            time.textContent =
                formatMessageTime(
                    message.createdAt
                );


            time.classList.remove(
                "d-none"
            );
        }


        conversationItem.dataset.searchMessage =
            message.content || "";


        const conversationList =
            document.querySelector(
                ".conversation-list"
            );


        if (conversationList) {

            conversationList.prepend(
                conversationItem
            );
        }
    }


    // =========================================================
    // 8. APPEND REAL-TIME MESSAGE TO OPEN CHAT
    // =========================================================

    function appendMessageToChat(
        message,
        isMine
    ) {

        if (!chatMessages) {
            return;
        }


        const emptyState =
            chatMessages.querySelector(
                ".chat-empty"
            );


        emptyState?.remove();


        const messageRow =
            document.createElement(
                "div"
            );


        messageRow.classList.add(
            "message-row",
            isMine
                ? "mine"
                : "theirs"
        );


        messageRow.dataset.messageId =
            message.messageId;


        const messageBubble =
            document.createElement(
                "div"
            );


        messageBubble.classList.add(
            "message-bubble"
        );


        // -----------------------------------------------------
        // Text
        // -----------------------------------------------------

        if (
            message.content &&
            message.content.trim() !== ""
        ) {

            const messageText =
                document.createElement(
                    "div"
                );


            messageText.classList.add(
                "message-text"
            );


            messageText.textContent =
                message.content;


            messageBubble.appendChild(
                messageText
            );
        }


        // -----------------------------------------------------
        // Images
        // -----------------------------------------------------

        if (
            Array.isArray(
                message.imagePaths
            ) &&
            message.imagePaths.length > 0
        ) {

            const imageContainer =
                document.createElement(
                    "div"
                );


            imageContainer.classList.add(
                "message-images",
                `image-count-${message.imagePaths.length}`
            );


            message.imagePaths.forEach(
                function (
                    imagePath,
                    index
                ) {

                    const button =
                        document.createElement(
                            "button"
                        );


                    button.type =
                        "button";


                    button.classList.add(
                        "message-image-button"
                    );


                    const image =
                        document.createElement(
                            "img"
                        );


                    image.src =
                        imagePath;


                    image.alt =
                        "Sent image";


                    image.classList.add(
                        "message-image"
                    );


                    button.appendChild(
                        image
                    );


                    imageContainer.appendChild(
                        button
                    );


                    button.addEventListener(
                        "click",
                        function () {

                            openLightbox(
                                message.imagePaths,
                                index
                            );
                        }
                    );
                }
            );


            messageBubble.appendChild(
                imageContainer
            );
        }


        // -----------------------------------------------------
        // Time
        // -----------------------------------------------------

        const messageTime =
            document.createElement(
                "div"
            );


        messageTime.classList.add(
            "message-time"
        );


        messageTime.textContent =
            formatMessageTime(
                message.createdAt
            );


        messageBubble.appendChild(
            messageTime
        );


        // Bubble inside row.
        messageRow.appendChild(
            messageBubble
        );


        // -----------------------------------------------------
        // Sent status
        // -----------------------------------------------------

        if (isMine) {

            document
                .querySelectorAll(
                    ".message-delivery-status"
                )
                .forEach(
                    function (status) {

                        status.remove();
                    }
                );


            const deliveryStatus =
                document.createElement(
                    "div"
                );


            deliveryStatus.classList.add(
                "message-delivery-status"
            );


            deliveryStatus.dataset.messageStatusId =
                message.messageId;


            deliveryStatus.textContent =
                "Sent";


            // Outside message bubble.
            messageRow.appendChild(
                deliveryStatus
            );
        }


        chatMessages.appendChild(
            messageRow
        );


        chatMessages.scrollTop =
            chatMessages.scrollHeight;
    }


    // =========================================================
    // 9. TYPING INDICATOR
    //
    // Typing appears ONLY in the currently open chat.
    // The conversation list is NOT changed anymore.
    // =========================================================

    let localTypingTimeout =
        null;

    let isCurrentlyTyping =
        false;

    let lastTypingSignalAt =
        0;

    let remoteTypingTimeout =
        null;


    async function sendTypingState(
        isTyping
    ) {

        const conversationId =
            getCurrentConversationId();


        if (
            conversationId === null
        ) {
            return;
        }


        if (
            !connection ||
            connection.state !==
                signalR.HubConnectionState.Connected
        ) {
            return;
        }


        try {

            await connection.invoke(
                isTyping
                    ? "StartTyping"
                    : "StopTyping",
                conversationId
            );

        }
        catch (error) {

            console.error(
                "Typing indicator error:",
                error
            );
        }
    }


    async function stopLocalTyping() {

        if (
            localTypingTimeout
        ) {

            clearTimeout(
                localTypingTimeout
            );


            localTypingTimeout =
                null;
        }


        if (
            !isCurrentlyTyping
        ) {
            return;
        }


        isCurrentlyTyping =
            false;


        lastTypingSignalAt =
            0;


        await sendTypingState(
            false
        );
    }


    if (messageInput) {

        messageInput.addEventListener(
            "input",
            function () {

                const hasText =
                    messageInput.value
                        .trim()
                        .length > 0;


                // Empty input means stop immediately.
                if (!hasText) {

                    stopLocalTyping();

                    return;
                }


                const now =
                    Date.now();


                /*
                 * Send StartTyping:
                 * - when typing starts
                 * - about once every second while typing
                 *
                 * This works as a heartbeat so the
                 * receiver knows the user is still active.
                 */
                if (
                    !isCurrentlyTyping ||
                    now - lastTypingSignalAt
                        >= 1000
                ) {

                    isCurrentlyTyping =
                        true;


                    lastTypingSignalAt =
                        now;


                    sendTypingState(
                        true
                    );
                }


                if (
                    localTypingTimeout
                ) {

                    clearTimeout(
                        localTypingTimeout
                    );
                }


                // Stop after 1.5 seconds of inactivity.
                localTypingTimeout =
                    setTimeout(
                        function () {

                            stopLocalTyping();
                        },
                        1500
                    );
            }
        );
    }


    connection?.on(
        "UserTyping",
        function (data) {

            const currentConversationId =
                getCurrentConversationId();


            /*
             * Important:
             * Typing is ONLY shown if the event belongs
             * to the conversation currently open.
             */
            if (
                currentConversationId === null ||
                currentConversationId !==
                    data.conversationId ||
                !typingIndicator
            ) {
                return;
            }


            if (
                data.isTyping
            ) {

                typingIndicator
                    .classList
                    .remove(
                        "d-none"
                    );


                /*
                 * Reset fallback timer every time
                 * another StartTyping heartbeat arrives.
                 */
                if (
                    remoteTypingTimeout
                ) {

                    clearTimeout(
                        remoteTypingTimeout
                    );
                }


                /*
                 * Safety fallback:
                 * if StopTyping is somehow lost,
                 * hide the indicator after 3 seconds.
                 */
                remoteTypingTimeout =
                    setTimeout(
                        function () {

                            typingIndicator
                                .classList
                                .add(
                                    "d-none"
                                );


                            remoteTypingTimeout =
                                null;
                        },
                        3000
                    );

            }
            else {

                if (
                    remoteTypingTimeout
                ) {

                    clearTimeout(
                        remoteTypingTimeout
                    );


                    remoteTypingTimeout =
                        null;
                }


                typingIndicator
                    .classList
                    .add(
                        "d-none"
                    );
            }
        }
    );

    // =========================================================
    // 10. SEND MESSAGE WITHOUT PAGE REFRESH
    // =========================================================

    if (messageSendForm) {

        messageSendForm.addEventListener(
            "submit",
            async function (event) {

                event.preventDefault();


                const hasText =
                    messageInput &&
                    messageInput.value
                        .trim() !== "";

                const hasImages =
                    messageImageInput &&
                    messageImageInput.files.length > 0;


                // Do not send an empty message.
                if (
                    !hasText &&
                    !hasImages
                ) {
                    return;
                }


                // Prevent double-clicking Send.
                if (sendButton) {

                    sendButton.disabled =
                        true;
                }


                try {

                    const conversationId =
                        getCurrentConversationId();


                    if (
                        conversationId === null
                    ) {

                        console.error(
                            "No current conversation ID found."
                        );

                        return;
                    }


                    if (
                        !connection ||
                        connection.state !==
                            signalR.HubConnectionState.Connected
                    ) {

                        console.error(
                            "SignalR is not connected yet."
                        );

                        return;
                    }


                    // Make sure this browser has joined
                    // the current conversation group.
                    await connection.invoke(
                        "JoinConversation",
                        conversationId
                    );


                    /*
                     * FormData automatically collects:
                     *
                     * conversationId
                     * content
                     * imageFiles
                     * antiforgery token
                     */
                    const formData =
                        new FormData(
                            messageSendForm
                        );


                    const response =
                        await fetch(
                            messageSendForm.action,
                            {
                                method: "POST",

                                body: formData,

                                headers: {
                                    "X-Requested-With":
                                        "XMLHttpRequest"
                                }
                            }
                        );


                    if (!response.ok) {

                        let errorMessage =
                            "The message could not be sent. Please try again.";


                        try {

                            const errorData =
                                await response.json();


                            if (errorData?.message) {

                                errorMessage =
                                    errorData.message;
                            }

                        }
                        catch (error) {

                            console.error(
                                "Could not read message error response:",
                                error
                            );
                        }


                        if (typeof window.showSystemModal === "function") {

                            window.showSystemModal({
                                type: "warning",
                                title: "Message Not Sent",
                                message: errorMessage,
                                buttonText: "Okay"
                            });

                        }
                        else {

                            console.error(
                                errorMessage
                            );
                        }


                        return;
                    }


                    // Clear text input.
                    if (messageInput) {

                        messageInput.value =
                            "";
                    }


                    // Sending means the user
                    // is no longer typing.
                    await stopLocalTyping();


                    // Clear image preview/input.
                    if (
                        messageImageInput &&
                        typeof messageImageInput
                            .resetImagePreview === "function"
                    ) {

                        messageImageInput
                            .resetImagePreview();
                    }


                    // Keep cursor ready.
                    messageInput?.focus();

                }
                catch (error) {

                    console.error(
                        "Error sending message:",
                        error
                    );

                }
                finally {

                    if (sendButton) {

                        sendButton.disabled =
                            false;
                    }
                }

            }
        );
    }


    // =========================================================
    // 11. SIGNALR - RECEIVE MESSAGE
    // =========================================================

    connection?.on(
        "ReceiveMessage",
        async function (message) {

            const isMine =
                message.senderId ===
                currentUserId;


            // Add message to the open chat.
            appendMessageToChat(
                message,
                isMine
            );


            // Update conversation preview/time.
            updateConversationPreview(
                message,
                isMine
            );


            /*
             * If the OTHER user sent a real message,
             * hide their typing indicator immediately.
             */
            if (
                !isMine &&
                typingIndicator
            ) {

                if (
                    remoteTypingTimeout
                ) {

                    clearTimeout(
                        remoteTypingTimeout
                    );


                    remoteTypingTimeout =
                        null;
                }


                typingIndicator
                    .classList
                    .add(
                        "d-none"
                    );
            }


            // =================================================
            // MARK INCOMING MESSAGE AS READ
            // =================================================

            if (!isMine) {

                try {

                    await connection.invoke(
                        "MarkAsRead",
                        message.conversationId,
                        message.messageId
                    );

                }
                catch (error) {

                    console.error(
                        "Failed to mark message as read:",
                        error
                    );
                }
            }
        }
    );


    // =========================================================
    // 12. SIGNALR - SENT -> SEEN
    // =========================================================

    connection?.on(
        "MessageSeen",
        function (data) {

            const currentConversationId =
                getCurrentConversationId();


            // Only update the currently-open conversation.
            if (
                currentConversationId === null ||
                currentConversationId !==
                    data.conversationId
            ) {
                return;
            }


            /*
             * Only the newest outgoing message
             * should show Sent / Seen.
             */
            document
                .querySelectorAll(
                    ".message-delivery-status"
                )
                .forEach(
                    function (status) {

                        status.remove();
                    }
                );


            const messageRow =
                document.querySelector(
                    `[data-message-id="${data.messageId}"]`
                );


            if (!messageRow) {

                return;
            }


            const status =
                document.createElement(
                    "div"
                );


            status.classList.add(
                "message-delivery-status"
            );


            status.dataset.messageStatusId =
                data.messageId;


            status.textContent =
                "Seen";


            // Keep Seen outside the bubble.
            messageRow.appendChild(
                status
            );
        }
    );


    // =========================================================
    // 13. SIGNALR - MESSAGE NOTIFICATION
    //
    // Used when a message arrives from another conversation.
    // =========================================================

    connection?.on(
        "MessageNotification",
        function (message) {

            const currentConversationId =
                getCurrentConversationId();


            // =================================================
            // ARCHIVED VIEW
            // =================================================
            //
            // A new incoming message automatically unarchives
            // the conversation on the server.
            //
            // If we are currently looking at Archived Chats,
            // remove that conversation from this list instantly.
            // =================================================

            if (isArchiveView) {

                const archivedConversationItem =
                    document.querySelector(
                        `.conversation-item[data-conversation-id="${message.conversationId}"]`
                    );


                if (archivedConversationItem) {

                    archivedConversationItem.remove();

                    showArchivedEmptyStateIfNeeded();
                }


                /*
                 * If the conversation that just received
                 * a new message is currently open,
                 * move the user to the normal inbox
                 * while keeping that conversation open.
                 */
                if (
                    currentConversationId ===
                    message.conversationId
                ) {

                    window.location.href =
                        `/Messages?conversationId=${message.conversationId}`;

                    return;
                }


                return;
            }


            /*
             * If this is already the open conversation,
             * ReceiveMessage handles everything.
             */
            if (
                currentConversationId ===
                message.conversationId
            ) {
                return;
            }


            let conversationItem =
                document.querySelector(
                    `.conversation-item[data-conversation-id="${message.conversationId}"]`
                );


            // =================================================
            // BRAND-NEW CONVERSATION
            // =================================================

            if (!conversationItem) {

                conversationItem =
                    createConversationItem(
                        message
                    );
            }


            if (!conversationItem) {

                return;
            }


            // Update last-message preview and time.
            updateConversationPreview(
                message,
                false
            );


            // Increase only the conversation-list badge.
            //
            // The MAIN SYSTEM SIDEBAR badge is handled
            // by global-messaging.js.
            incrementConversationUnread(
                message.conversationId
            );
        }
    );


    // =========================================================
    // 14. JOIN CURRENT CONVERSATION
    // =========================================================

    async function joinCurrentConversation() {

        const conversationId =
            getCurrentConversationId();


        if (
            conversationId === null
        ) {
            return;
        }


        if (
            !connection ||
            connection.state !==
                signalR.HubConnectionState.Connected
        ) {
            return;
        }


        try {

            await connection.invoke(
                "JoinConversation",
                conversationId
            );

        }
        catch (error) {

            console.error(
                "Failed to join conversation:",
                error
            );
        }
    }


    // =========================================================
    // 15. SIGNALR RECONNECTION
    // =========================================================

    connection?.onreconnected(
        async function () {

            /*
             * SignalR group membership is tied
             * to the connection.
             *
             * After reconnecting we join the
             * current conversation again.
             */
            await joinCurrentConversation();

        }
    );


    // =========================================================
    // 16. WAIT FOR GLOBAL SIGNALR CONNECTION
    // =========================================================

    if (
        window.chatConnectionReady
    ) {

        window.chatConnectionReady
            .then(
                async function () {

                    await joinCurrentConversation();

                }
            )
            .catch(
                function (error) {

                    console.error(
                        "Could not prepare Messages page SignalR:",
                        error
                    );
                }
            );
    }


    // =========================================================
    // 17. INITIAL PAGE STATE
    // =========================================================

    /*
     * When a conversation first opens,
     * immediately show the latest messages
     * instead of starting from the top.
     */
    if (chatMessages) {

        chatMessages.scrollTop =
            chatMessages.scrollHeight;
    }

});
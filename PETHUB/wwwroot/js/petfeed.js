// Wait until the entire HTML document has been loaded.
document.addEventListener("DOMContentLoaded", function () {

    // ==========================================================
    // FEED CONTAINER
    // ==========================================================

    const feedContainer =
        document.querySelector(".feed-container");

    // petfeed.js is loaded globally, so stop when this is not PetFeed.
    if (!feedContainer) {
        return;
    }


    // ==========================================================
    // INITIALIZE DYNAMIC PETFEED CARD UI
    // ==========================================================
    //
    // Search and infinite scroll insert new HTML into the feed.
    // Direct click listeners from _FeedItems do not automatically
    // exist on those newly inserted elements.
    //
    // This function attaches the required UI behavior to new cards.
    // ==========================================================

    function initializePetFeedCards(container) {

        if (!container) {
            return;
        }


        // ======================================================
        // COMMENT TOGGLE
        // ======================================================

        container
            .querySelectorAll("[data-comment-toggle]")
            .forEach(function (button) {

                /*
                 * Prevent attaching the same listener twice.
                 */
                if (button.dataset.commentInitialized === "true") {
                    return;
                }


                button.dataset.commentInitialized = "true";


                button.addEventListener(
                    "click",
                    function () {

                        const postId =
                            button.getAttribute(
                                "data-comment-toggle"
                            );


                        const comments =
                            feedContainer.querySelector(
                                `[data-comments-for="${postId}"]`
                            );


                        if (!comments) {
                            return;
                        }


                        comments.hidden =
                            !comments.hidden;

                    }
                );

            });


        // ======================================================
        // SEE MORE / SEE LESS
        // ======================================================

        container
            .querySelectorAll(
                ".feed-item-description-wrapper"
            )
            .forEach(function (wrapper) {

                if (
                    wrapper.dataset.descriptionInitialized
                    === "true"
                ) {
                    return;
                }


                wrapper.dataset.descriptionInitialized =
                    "true";


                const description =
                    wrapper.querySelector(
                        ".feed-item-description"
                    );


                const seeMore =
                    wrapper.querySelector(
                        ".feed-item-see-more"
                    );


                if (!description || !seeMore) {
                    return;
                }


                if (
                    description.scrollHeight >
                    description.clientHeight + 1
                ) {
                    seeMore.classList.add("show");
                }


                seeMore.addEventListener(
                    "click",
                    function () {

                        const isExpanded =
                            description.classList.contains(
                                "expanded"
                            );


                        if (isExpanded) {

                            description.classList.remove(
                                "expanded"
                            );

                            seeMore.textContent =
                                "See more";

                        }
                        else {

                            description.classList.add(
                                "expanded"
                            );

                            seeMore.textContent =
                                "See less";

                        }

                    }
                );

            });
    }


    // ==========================================================
    // PETFEED NAVBAR LIVE SEARCH
    // ==========================================================

    const petFeedSearchInput =
        document.getElementById(
            "petFeedSearchInput"
        );


    let petFeedSearchTimer = null;

    let petFeedIsSearching = false;


    /*
     * Search only exists on PetFeed.
     * If the navbar input is not present,
     * nothing below will run.
     */
    if (petFeedSearchInput) {

        petFeedSearchInput.addEventListener(
            "input",
            function () {

                /*
                 * Wait 300ms after the user stops typing.
                 *
                 * This prevents sending a request for
                 * every single key press.
                 */
                clearTimeout(
                    petFeedSearchTimer
                );


                petFeedSearchTimer =
                    setTimeout(
                        async function () {

                            const search =
                                petFeedSearchInput
                                    .value
                                    .trim();


                            isPetFeedSearchActive =
                                search !== "";


                            /*
                             * feedSeed already belongs to the
                             * current PetFeed session.
                             *
                             * We send it so clearing search
                             * can restore the same randomized
                             * feed order.
                             */
                            const url =
                                `/PetFeeds/Search?search=${encodeURIComponent(search)}&feedSeed=${encodeURIComponent(feedSeed)}`;


                            try {

                                petFeedIsSearching = true;


                                const response =
                                    await fetch(
                                        url,
                                        {
                                            method: "GET",

                                            headers: {
                                                "X-Requested-With":
                                                    "XMLHttpRequest"
                                            }
                                        }
                                    );


                                if (!response.ok) {

                                    throw new Error(
                                        `PetFeed search failed: ${response.status}`
                                    );
                                }


                                const html =
                                    await response.text();


                                const trimmedHtml =
                                    html.trim();


                                const parser =
                                    new DOMParser();


                                const parsedDocument =
                                    parser.parseFromString(
                                        trimmedHtml,
                                        "text/html"
                                    );


                                const hasFeedItems =
                                    parsedDocument.querySelector(
                                        ".feed-item"
                                    ) !== null;


                                // ======================================================
                                // NO SEARCH RESULTS
                                // ======================================================

                                if (
                                    search !== "" &&
                                    !hasFeedItems
                                ) {

                                    feedContainer.innerHTML = `
                                                                <div class="petfeed-search-empty">

                                                                    <div class="petfeed-search-empty-icon">
                                                                        <i data-lucide="search-x"></i>
                                                                    </div>

                                                                    <h3>
                                                                        No results found
                                                                    </h3>

                                                                    <p>
                                                                        We couldn't find anything matching
                                                                        "<strong>${escapeHtml(search)}</strong>".
                                                                    </p>

                                                                    <span>
                                                                        Try another keyword.
                                                                    </span>

                                                                </div>
                                                            `;

                                }
                                else {

                                    feedContainer.innerHTML =
                                        trimmedHtml;


                                    initializePetFeedCards(
                                        feedContainer
                                    );

                                }


                                if (
                                    typeof lucide !== "undefined"
                                ) {

                                    lucide.createIcons();
                                }


                                /*
                                 * Search always starts from page 1.
                                 */
                                currentPage = 1;


                                /*
                                 * While searching, infinite scroll
                                 * should not fetch normal feed pages.
                                 */
                                if (search !== "") {

                                    hasMorePosts = false;

                                }
                                else {

                                    isPetFeedSearchActive = false;

                                    hasMorePosts = true;

                                    /*
                                     * The normal first page has returned,
                                     * so it is safe to save again.
                                     */
                                    saveFeedState();
                                }

                            }
                            catch (error) {

                                console.error(
                                    "Error searching PetFeed:",
                                    error
                                );

                            }
                            finally {

                                petFeedIsSearching = false;

                            }

                        },
                        300
                    );

            }
        );
    }

    // ==========================================================
    // PETFEED STATE
    // ==========================================================

    const feedStateKey =
        "pethub_petfeed_state";

    let currentPage = 1;

    let isLoading = false;

    let hasMorePosts = true;

    let isPetFeedSearchActive = false;

    let feedSeed =
        feedContainer.dataset.feedSeed || "";

    const currentUserId =
        feedContainer.dataset.userId || "";

    let saveStateTimeout = null;


    // ==========================================================
    // DETECT PAGE REFRESH
    // ==========================================================

    const navigationEntry =
        performance.getEntriesByType("navigation")[0];

    const isPageRefresh =
        navigationEntry &&
        navigationEntry.type === "reload";


    if (isPageRefresh) {

        sessionStorage.removeItem(
            feedStateKey
        );

        if ("scrollRestoration" in history) {

            history.scrollRestoration =
                "manual";
        }

        window.scrollTo(0, 0);
    }


    // ==========================================================
    // RESTORE PREVIOUS PETFEED STATE
    // ==========================================================

    if (!isPageRefresh) {

        const savedState =
            sessionStorage.getItem(
                feedStateKey
            );


        if (savedState) {

            try {

                const state =
                    JSON.parse(savedState);


                const savedUserId =
                    typeof state.userId === "string"
                        ? state.userId
                        : "";


                // Never restore another user's cached PetFeed.
                if (savedUserId !== currentUserId) {

                    sessionStorage.removeItem(
                        feedStateKey
                    );

                }
                else {

                    if (
                        typeof state.feedHtml ===
                        "string"
                    ) {

                        feedContainer.innerHTML =
                            state.feedHtml;
                    }


                    if (
                        Number.isInteger(
                            state.currentPage
                        )
                    ) {

                        currentPage =
                            state.currentPage;
                    }


                    if (
                        typeof state.feedSeed ===
                        "string" &&
                        state.feedSeed.length > 0
                    ) {

                        feedSeed =
                            state.feedSeed;
                    }


                    if (
                        typeof state.hasMorePosts ===
                        "boolean"
                    ) {

                        hasMorePosts =
                            state.hasMorePosts;
                    }


                    if (
                        typeof state.scrollPosition ===
                        "number"
                    ) {

                        requestAnimationFrame(
                            function () {

                                window.scrollTo(
                                    0,
                                    state.scrollPosition
                                );

                            }
                        );
                    }
                }

            }
            catch (error) {

                console.error(
                    "Unable to restore PetFeed state:",
                    error
                );

                sessionStorage.removeItem(
                    feedStateKey
                );
            }
        }
    }


    // ==========================================================
    // SAVE PETFEED STATE
    // ==========================================================

    function saveFeedState() {

        /*
         * Search results are temporary.
         * Never overwrite the normal saved PetFeed
         * with filtered search results.
         */
        if (isPetFeedSearchActive) {
            return;
        }


        try {

            const state = {

                feedHtml:
                    feedContainer.innerHTML,

                currentPage:
                    currentPage,

                hasMorePosts:
                    hasMorePosts,

                scrollPosition:
                    window.scrollY,

                feedSeed:
                    feedSeed,

                userId:
                    currentUserId
            };


            sessionStorage.setItem(
                feedStateKey,
                JSON.stringify(state)
            );

        }
        catch (error) {

            console.error(
                "Unable to save PetFeed state:",
                error
            );
        }
    }


    // ==========================================================
    // THROTTLED STATE SAVING
    // ==========================================================

    function scheduleStateSave() {

        if (saveStateTimeout !== null) {
            return;
        }


        saveStateTimeout =
            setTimeout(
                function () {

                    saveFeedState();

                    saveStateTimeout =
                        null;

                },
                250
            );
    }


    // ==========================================================
    // LOAD MORE POSTS
    // ==========================================================

    async function loadMorePosts() {

        if (
            isLoading ||
            !hasMorePosts ||
            petFeedIsSearching
        ) {
            return;
        }


        isLoading = true;


        const nextPage =
            currentPage + 1;


        try {

            const response =
                await fetch(
                    `/PetFeeds/LoadMore?page=${nextPage}&feedSeed=${encodeURIComponent(feedSeed)}`,
                    {
                        method: "GET",

                        headers: {
                            "X-Requested-With":
                                "XMLHttpRequest"
                        }
                    }
                );


            if (!response.ok) {

                throw new Error(
                    `Failed to load feed page: ${response.status}`
                );
            }


            const html =
                await response.text();


            const trimmedHtml =
                html.trim();


            if (!trimmedHtml) {

                hasMorePosts = false;

                saveFeedState();

                return;
            }


            feedContainer.insertAdjacentHTML(
                "beforeend",
                trimmedHtml
            );


            /*
             * Initialize UI behavior for newly loaded cards.
             */
            initializePetFeedCards(
                feedContainer
            );


            if (
                typeof lucide !== "undefined"
            ) {
                lucide.createIcons();
            }


            currentPage = nextPage;

            saveFeedState();

        }
        catch (error) {

            console.error(
                "Error loading more PetFeed posts:",
                error
            );

        }
        finally {

            isLoading = false;
        }
    }


    // ==========================================================
    // ESCAPE USER-GENERATED TEXT
    // ==========================================================

    function escapeHtml(value) {

        const element =
            document.createElement("div");

        element.textContent =
            value ?? "";

        return element.innerHTML;
    }


    // ==========================================================
    // GET ANTI-FORGERY TOKEN
    // ==========================================================

    function getAntiForgeryToken() {

        const token =
            document.querySelector(
                'input[name="__RequestVerificationToken"]'
            );

        return token?.value ?? "";
    }


    // ==========================================================
    // UPDATE PAW COUNT
    // ==========================================================

    function updatePetFeedPawCount(
        petFeedId,
        pawCount
    ) {

        /*
         * In _FeedItems.cshtml these two attributes
         * are on the SAME span:
         *
         * data-paw-count-for
         * data-paw-count-value
         */
        const countValue =
            feedContainer.querySelector(
                `[data-paw-count-for="${petFeedId}"][data-paw-count-value]`
            );


        if (countValue) {

            countValue.textContent =
                pawCount;
        }
    }


    // ==========================================================
    // UPDATE COMMENT COUNT
    // ==========================================================

    function updatePetFeedCommentCount(
        petFeedId,
        commentCount
    ) {

        /*
         * First try the dedicated attributes if you added
         * them to _FeedItems.cshtml.
         */
        let countValue =
            feedContainer.querySelector(
                `[data-comment-count-for="${petFeedId}"][data-comment-count-value]`
            );


        /*
         * Fallback for your original Razor markup.
         * The comment count is the span inside the button
         * with data-comment-toggle.
         */
        if (!countValue) {

            const commentButton =
                feedContainer.querySelector(
                    `[data-comment-toggle="${petFeedId}"]`
                );


            countValue =
                commentButton?.querySelector(
                    "span"
                );
        }


        if (countValue) {

            countValue.textContent =
                commentCount;
        }
    }


    // ==========================================================
    // GET / CREATE COMMENT LIST
    // ==========================================================

    function getOrCreateCommentList(
        petFeedId
    ) {

        const section =
            feedContainer.querySelector(
                `[data-comments-for="${petFeedId}"]`
            );


        if (!section) {
            return null;
        }


        let list =
            section.querySelector(
                "[data-comments-list]"
            );


        if (!list) {

            list =
                document.createElement(
                    "div"
                );


            list.classList.add(
                "feed-item-comments-list"
            );


            list.setAttribute(
                "data-comments-list",
                ""
            );


            const emptyMessage =
                section.querySelector(
                    "[data-empty-comments]"
                );


            if (emptyMessage) {

                emptyMessage.replaceWith(
                    list
                );

            }
            else {

                const heading =
                    section.querySelector(
                        ".feed-item-section-title"
                    );


                if (heading) {

                    heading.insertAdjacentElement(
                        "afterend",
                        list
                    );
                }
            }

        }
        else {

            const emptyMessage =
                section.querySelector(
                    "[data-empty-comments]"
                );


            if (emptyMessage) {

                emptyMessage.remove();
            }
        }


        return list;
    }


    // ==========================================================
    // APPEND COMMENT
    // ==========================================================

    function appendPetFeedComment(
        data,
        canDelete = false
    ) {

        // Prevent duplicates when AJAX and SignalR overlap.
        const existingComment =
            feedContainer.querySelector(
                `[data-comment-id="${data.commentId}"]`
            );


        if (existingComment) {
            return;
        }


        const list =
            getOrCreateCommentList(
                data.petFeedId
            );


        if (!list) {
            return;
        }


        const firstName =
            data.firstName ?? "";

        const lastName =
            data.lastName ?? "";

        const initial =
            (firstName || "?")
                .substring(0, 1);


        const avatarHtml =
            data.profilePicturePath

                ? `
                    <img src="${escapeHtml(data.profilePicturePath)}"
                         alt="Profile picture" />
                  `

                : escapeHtml(initial);


        let deleteButtonHtml = "";


        if (canDelete) {

            const token =
                getAntiForgeryToken();


            deleteButtonHtml = `
                <form action="/PetFeeds/DeleteComment"
                      method="post"
                      class="feed-item-delete-form"
                      data-delete-comment-form>

                    <input type="hidden"
                           name="__RequestVerificationToken"
                           value="${escapeHtml(token)}" />

                    <input type="hidden"
                           name="id"
                           value="${data.commentId}" />

                    <input type="hidden"
                           name="feedSeed"
                           value="${escapeHtml(feedSeed)}" />

                    <button type="submit"
                            class="feed-item-button feed-item-delete-button"
                            aria-label="Delete comment">

                        <i data-lucide="trash-2"></i>

                    </button>

                </form>
            `;
        }


        const commentHtml = `
            <div class="feed-item-comment"
                 data-comment-id="${data.commentId}">

                <div class="feed-item-comment-header">

                    <div class="feed-item-comment-author">

                        <div class="feed-item-comment-avatar">
                            ${avatarHtml}
                        </div>

                        <div class="feed-item-comment-author-info">

                            <strong class="feed-item-comment-name">
                                ${escapeHtml(firstName)}
                                ${escapeHtml(lastName)}
                            </strong>

                            <small class="feed-item-comment-date">
                                ${escapeHtml(data.datePosted)}
                            </small>

                        </div>

                    </div>

                    ${deleteButtonHtml}

                </div>

                <p class="feed-item-comment-content">
                    ${escapeHtml(data.content)}
                </p>

            </div>
        `;


        list.insertAdjacentHTML(
            "beforeend",
            commentHtml
        );


        if (
            typeof lucide !==
            "undefined"
        ) {

            lucide.createIcons();
        }
    }


    // ==========================================================
    // REMOVE COMMENT
    // ==========================================================

    function removePetFeedComment(data) {

        const comment =
            feedContainer.querySelector(
                `[data-comment-id="${data.commentId}"]`
            );


        if (comment) {

            comment.remove();
        }


        // There are still comments left.
        if (Number(data.commentCount) !== 0) {
            return;
        }


        const section =
            feedContainer.querySelector(
                `[data-comments-for="${data.petFeedId}"]`
            );


        const list =
            section?.querySelector(
                "[data-comments-list]"
            );


        if (!list) {
            return;
        }


        const emptyMessage =
            document.createElement("p");


        emptyMessage.classList.add(
            "feed-item-empty-comments"
        );


        emptyMessage.setAttribute(
            "data-empty-comments",
            ""
        );


        emptyMessage.textContent =
            "No comments yet. Be the first to comment!";


        list.replaceWith(
            emptyMessage
        );
    }


    // ==========================================================
    // PETFEED FORM SUBMISSIONS
    // ==========================================================

    feedContainer.addEventListener(
        "submit",
        async function (event) {

            const form =
                event.target;


            if (
                !(form instanceof HTMLFormElement)
            ) {
                return;
            }


            // ======================================================
            // PAW / UNPAW
            // ======================================================

            if (
                form.hasAttribute(
                    "data-paw-form"
                )
            ) {

                event.preventDefault();
                event.stopPropagation();


                const button =
                    form.querySelector(
                        "[data-paw-button]"
                    );


                if (
                    !button ||
                    button.disabled
                ) {
                    return;
                }


                button.disabled = true;


                try {

                    const formData =
                        new FormData(form);


                    const response =
                        await fetch(
                            form.action,
                            {
                                method:
                                    "POST",

                                headers: {
                                    "X-Requested-With":
                                        "XMLHttpRequest"
                                },

                                body:
                                    formData
                            }
                        );


                    if (!response.ok) {

                        throw new Error(
                            `Paw request failed: ${response.status}`
                        );
                    }


                    const result =
                        await response.json();


                    if (!result.success) {
                        return;
                    }


                    // Update sender's count immediately.
                    updatePetFeedPawCount(
                        result.petFeedId,
                        result.pawCount
                    );


                    // Only the current member's Paw state changes.
                    button.classList.toggle(
                        "active",
                        result.isPawed
                    );


                    button.setAttribute(
                        "aria-label",
                        result.isPawed
                            ? "Remove Paw"
                            : "Paw"
                    );


                    // Next click must perform the opposite action.
                    form.action =
                        result.isPawed

                            ? form.dataset.unpawUrl

                            : form.dataset.pawUrl;


                    form.setAttribute(
                        "data-is-pawed",
                        result.isPawed
                            ? "true"
                            : "false"
                    );


                    saveFeedState();

                }
                catch (error) {

                    console.error(
                        "Error submitting Paw/Unpaw:",
                        error
                    );

                }
                finally {

                    button.disabled =
                        false;
                }


                return;
            }


            // ======================================================
            // ADD COMMENT
            // ======================================================

            if (
                form.hasAttribute(
                    "data-add-comment-form"
                )
            ) {

                event.preventDefault();
                event.stopPropagation();


                const textarea =
                    form.querySelector(
                        "[data-comment-input]"
                    );


                const content =
                    textarea
                        ? textarea.value.trim()
                        : "";


                if (!content) {
                    return;
                }


                const submitButton =
                    form.querySelector(
                        'button[type="submit"]'
                    );


                if (submitButton?.disabled) {
                    return;
                }


                if (submitButton) {

                    submitButton.disabled =
                        true;
                }


                try {

                    const formData =
                        new FormData(form);


                    const response =
                        await fetch(
                            form.action,
                            {
                                method:
                                    "POST",

                                headers: {
                                    "X-Requested-With":
                                        "XMLHttpRequest"
                                },

                                body:
                                    formData
                            }
                        );


                    if (!response.ok) {

                        throw new Error(
                            `AddComment request failed: ${response.status}`
                        );
                    }


                    const result =
                        await response.json();


                    if (!result.success) {
                        return;
                    }


                    /*
                     * Sender gets their comment immediately
                     * through the AJAX response.
                     */
                    appendPetFeedComment(
                        result,
                        true
                    );


                    updatePetFeedCommentCount(
                        result.petFeedId,
                        result.commentCount
                    );


                    if (textarea) {

                        textarea.value =
                            "";
                    }


                    saveFeedState();

                }
                catch (error) {

                    console.error(
                        "Error submitting AddComment:",
                        error
                    );

                }
                finally {

                    if (submitButton) {

                        submitButton.disabled =
                            false;
                    }
                }


                return;
            }


            // ======================================================
            // DELETE COMMENT
            // ======================================================

            if (
                form.hasAttribute(
                    "data-delete-comment-form"
                )
            ) {

                event.preventDefault();
                event.stopPropagation();


                const submitButton =
                    form.querySelector(
                        'button[type="submit"]'
                    );


                if (submitButton?.disabled) {
                    return;
                }


                if (submitButton) {

                    submitButton.disabled =
                        true;
                }


                try {

                    const formData =
                        new FormData(form);


                    const response =
                        await fetch(
                            form.action,
                            {
                                method:
                                    "POST",

                                headers: {
                                    "X-Requested-With":
                                        "XMLHttpRequest"
                                },

                                body:
                                    formData
                            }
                        );


                    if (!response.ok) {

                        throw new Error(
                            `DeleteComment request failed: ${response.status}`
                        );
                    }


                    const result =
                        await response.json();


                    if (!result.success) {
                        return;
                    }


                    removePetFeedComment(
                        result
                    );


                    updatePetFeedCommentCount(
                        result.petFeedId,
                        result.commentCount
                    );


                    saveFeedState();

                }
                catch (error) {

                    console.error(
                        "Error submitting DeleteComment:",
                        error
                    );

                    if (submitButton) {

                        submitButton.disabled =
                            false;
                    }
                }


                return;
            }

        }
    );


    // ==========================================================
    // PETFEED SIGNALR
    // ==========================================================

    /*
     * SignalR is already loaded globally by _Layout.cshtml.
     *
     * Stop here only if the library somehow failed to load.
     */
    if (typeof signalR === "undefined") {

        console.error(
            "SignalR library is not available for PetFeed."
        );

        return;
    }


    const petFeedConnection =
        new signalR.HubConnectionBuilder()
            .withUrl("/petFeedHub")
            .withAutomaticReconnect()
            .build();


    // ==========================================================
    // SIGNALR - PAW COUNT UPDATED
    // ==========================================================

    petFeedConnection.on(
        "PetFeedPawUpdated",
        function (data) {

            /*
             * Every open PetFeed gets the new total count.
             *
             * We DO NOT change another member's button
             * active/inactive state because paw ownership
             * is different for every account.
             */
            updatePetFeedPawCount(
                data.petFeedId,
                data.pawCount
            );


            saveFeedState();
        }
    );


    // ==========================================================
    // SIGNALR - COMMENT ADDED
    // ==========================================================

    petFeedConnection.on(
        "PetFeedCommentAdded",
        function (data) {

            /*
             * The sender already inserted their comment
             * from the AJAX response.
             *
             * Without this check, the sender would see
             * their comment twice.
             */
            if (
                data.senderUserId &&
                data.senderUserId ===
                currentUserId
            ) {

                return;
            }


            appendPetFeedComment(
                data,
                false
            );


            updatePetFeedCommentCount(
                data.petFeedId,
                data.commentCount
            );


            saveFeedState();
        }
    );


    // ==========================================================
    // SIGNALR - COMMENT DELETED
    // ==========================================================

    petFeedConnection.on(
        "PetFeedCommentDeleted",
        function (data) {

            removePetFeedComment(
                data
            );


            updatePetFeedCommentCount(
                data.petFeedId,
                data.commentCount
            );


            saveFeedState();
        }
    );


    // ==========================================================
    // SIGNALR CONNECTION STATE
    // ==========================================================

    petFeedConnection.onreconnecting(
        function (error) {

            console.warn(
                "PetFeed SignalR reconnecting...",
                error
            );
        }
    );


    petFeedConnection.onreconnected(
        function () {

            console.log(
                "PetFeed SignalR reconnected."
            );
        }
    );


    petFeedConnection.onclose(
        function (error) {

            console.warn(
                "PetFeed SignalR disconnected.",
                error
            );
        }
    );


    // ==========================================================
    // START SIGNALR
    // ==========================================================

    async function startPetFeedConnection() {

        try {

            await petFeedConnection.start();


            console.log(
                "PetFeed SignalR connected."
            );

        }
        catch (error) {

            console.error(
                "PetFeed SignalR connection failed:",
                error
            );


            setTimeout(
                startPetFeedConnection,
                5000
            );
        }
    }


    startPetFeedConnection();


    // ==========================================================
    // SCROLL HANDLING
    // ==========================================================

    window.addEventListener(
        "scroll",
        function () {

            const distanceFromBottom =
                document.documentElement.scrollHeight -
                (
                    window.innerHeight +
                    window.scrollY
                );


            if (
                distanceFromBottom <= 400
            ) {

                loadMorePosts();
            }


            scheduleStateSave();
        }
    );


    // ==========================================================
    // NAVIGATION STATE SAVING
    // ==========================================================

    window.addEventListener(
        "pagehide",
        function () {

            saveFeedState();
        }
    );

});
// Wait until the entire HTML document has been loaded.
// This ensures the feed container exists before the script tries to use it.
document.addEventListener("DOMContentLoaded", function () {

    // Find the container that holds the rendered PetFeed cards.
    // Feed.cshtml contains this element around _FeedItems.cshtml.
    const feedContainer = document.querySelector(".feed-container");

    // Stop the script if this page does not contain the PetFeed container.
    if (!feedContainer) {
        return;
    }


    // ==========================================================
    // PETFEED STATE
    // ==========================================================

    // This unique key identifies the saved PetFeed state in sessionStorage.
    // sessionStorage keeps the data while the current browser tab/session
    // remains active.
    const feedStateKey = "pethub_petfeed_state";

    // The first batch of posts is rendered by Feed.cshtml.
    // Therefore, the next page that should be requested is page 2.
    let currentPage = 1;

    // Prevent multiple LoadMore requests from running at the same time.
    let isLoading = false;

    // Becomes false when the server tells us that there are no more posts.
    let hasMorePosts = true;

    // Stores the random seed used by the current PetFeed.
    // The controller uses this same value to keep pagination in a stable order.
    let feedSeed =
        feedContainer.dataset.feedSeed || "";

    // Used to prevent sessionStorage from being written on every single
    // scroll event. Scroll events can fire many times per second.
    let saveStateTimeout = null;


    // ==========================================================
    // DETECT PAGE REFRESH
    // ==========================================================

    // PerformanceNavigationTiming is a browser API that tells us how the
    // current document was opened.
    const navigationEntry =
        performance.getEntriesByType("navigation")[0];

    // A navigation type of "reload" means the user refreshed the page.
    // In that situation we intentionally start with a fresh PetFeed.
    const isPageRefresh =
        navigationEntry &&
        navigationEntry.type === "reload";

    // When PetFeed is refreshed, remove the previous saved feed state and
    // force the browser back to the top of the page. This ensures a refresh
    // behaves like a completely fresh PetFeed visit.
    if (isPageRefresh) {
        sessionStorage.removeItem(feedStateKey);

        // Disable the browser's automatic scroll restoration so it does not
        // return to the previous position after the page reloads.
        if ("scrollRestoration" in history) {
            history.scrollRestoration = "manual";
        }

        // Run after the page has loaded so the browser cannot restore the
        // previous scroll position afterward.
        window.scrollTo(0, 0);
    }

    // When the user refreshes PetFeed, remove the previously saved state.
    // This ensures the refreshed page starts completely fresh and that the
    // old feed cannot be restored later when the user navigates away and
    // returns to PetFeed.
    if (isPageRefresh) {
        sessionStorage.removeItem(feedStateKey);
    }

    // ==========================================================
    // RESTORE PREVIOUS PETFEED STATE
    // ==========================================================

    // Only restore state when the user returned through normal navigation.
    // A browser refresh intentionally starts a fresh PetFeed.
    if (!isPageRefresh) {

        // Retrieve the previously saved PetFeed state from this browser tab.
        const savedState =
            sessionStorage.getItem(feedStateKey);

        // Only attempt restoration when saved state exists.
        if (savedState) {

            try {

                // Convert the JSON string stored by sessionStorage back
                // into a JavaScript object.
                const state = JSON.parse(savedState);

                // Restore all feed cards that were already loaded.
                // This prevents the server-rendered first page from replacing
                // the feed the user previously built through pagination.
                if (typeof state.feedHtml === "string") {
                    feedContainer.innerHTML = state.feedHtml;
                }

                // Restore the last successfully loaded page.
                // This allows the next pagination request to continue correctly.
                if (Number.isInteger(state.currentPage)) {
                    currentPage = state.currentPage;
                }

                // Restore the original random feed seed.
                // This is required so pagination after returning to PetFeed continues
                // using the same ordering as the original session.
                if (typeof state.feedSeed === "string" &&
                    state.feedSeed.length > 0) {

                    feedSeed = state.feedSeed;
                }

                // Restore whether additional posts are available.
                if (typeof state.hasMorePosts === "boolean") {
                    hasMorePosts = state.hasMorePosts;
                }

                // Restore the user's previous scroll position.
                if (typeof state.scrollPosition === "number") {

                    // requestAnimationFrame() waits for the browser to repaint
                    // the restored feed before changing the scroll position.
                    requestAnimationFrame(function () {

                        // Return the user to exactly where they left PetFeed.
                        window.scrollTo(
                            0,
                            state.scrollPosition
                        );

                    });
                }

            }
            catch (error) {

                // If the saved JSON is invalid, remove it so the next
                // PetFeed load can start normally instead of repeatedly
                // failing to restore corrupted state.
                console.error(
                    "Unable to restore PetFeed state:",
                    error
                );

                sessionStorage.removeItem(feedStateKey);
            }
        }
    }


    // ==========================================================
    // SAVE PETFEED STATE
    // ==========================================================

    // Saves the current PetFeed state to sessionStorage.
    // This includes the rendered posts, pagination position, and scroll.
    function saveFeedState() {

        try {

            // Store everything required to restore the exact PetFeed session.
            const state = {

                // Store all currently rendered feed cards.
                feedHtml: feedContainer.innerHTML,

                // Store the last successfully loaded pagination page.
                currentPage: currentPage,

                // Store whether more posts are available.
                hasMorePosts: hasMorePosts,

                // Store the user's exact vertical scroll position.
                scrollPosition: window.scrollY,

                // Store the random seed used by the controller.
                // This allows the restored feed to continue pagination using the
                // same ordering as the original feed.
                feedSeed: feedSeed
            };

            // Convert the state object to JSON and store it in the current
            // browser tab's sessionStorage.
            sessionStorage.setItem(
                feedStateKey,
                JSON.stringify(state)
            );

        }
        catch (error) {

            // sessionStorage has limited storage space, so this catches
            // storage failures without breaking the PetFeed itself.
            console.error(
                "Unable to save PetFeed state:",
                error
            );
        }
    }


    // ==========================================================
    // THROTTLED STATE SAVING
    // ==========================================================

    // Schedule a state save without writing to sessionStorage for every
    // individual scroll event.
    function scheduleStateSave() {

        // If a save is already waiting to run, don't create another timer.
        if (saveStateTimeout !== null) {
            return;
        }

        // setTimeout() delays the save by 250 milliseconds.
        // This reduces unnecessary sessionStorage writes while scrolling.
        saveStateTimeout = setTimeout(function () {

            // Save the latest feed and scroll state.
            saveFeedState();

            // Clear the timer reference so another save can be scheduled.
            saveStateTimeout = null;

        }, 250);
    }


    // ==========================================================
    // LOAD MORE POSTS
    // ==========================================================

    // Loads the next page of PetFeed posts from the existing controller.
    async function loadMorePosts() {

        // Do nothing if a request is already running or the server has
        // previously indicated that there are no more posts.
        if (isLoading || !hasMorePosts) {
            return;
        }

        // Mark the request as active before making the HTTP request.
        isLoading = true;

        // The next page is one higher than the page currently displayed.
        const nextPage = currentPage + 1;

        try {

            // Request the next page while supplying the same feed seed.
            // The controller uses this seed to reproduce the same randomized ordering.
            const response = await fetch(
                `/PetFeeds/LoadMore?page=${nextPage}&feedSeed=${encodeURIComponent(feedSeed)}`,                {
                    method: "GET",
                    headers: {
                        "X-Requested-With": "XMLHttpRequest"
                    }
                }
            );

            // Throw an error when the server returns an unsuccessful
            // HTTP response such as 404 or 500.
            if (!response.ok) {
                throw new Error(
                    `Failed to load feed page: ${response.status}`
                );
            }

            // Convert the HTTP response body into HTML text.
            const html = await response.text();

            // Remove whitespace so an empty response can be detected reliably.
            const trimmedHtml = html.trim();

            // An empty response means the server has no more feed items.
            if (!trimmedHtml) {

                // Prevent future scroll events from repeatedly requesting
                // pages that do not exist.
                hasMorePosts = false;

                // Save this state so returning to PetFeed knows that there
                // are no additional pages available.
                saveFeedState();

                return;
            }

            // Append the newly returned feed cards after the existing cards.
            // insertAdjacentHTML() adds the HTML without replacing existing posts.
            feedContainer.insertAdjacentHTML(
                "beforeend",
                trimmedHtml
            );

            // Update the current page only after the new HTML was successfully
            // inserted into the feed.
            currentPage = nextPage;

            // Save the newly expanded feed immediately after pagination succeeds.
            saveFeedState();

        }
        catch (error) {

            // Log the error in the browser console so controller, routing,
            // or network problems can be diagnosed during testing.
            console.error(
                "Error loading more PetFeed posts:",
                error
            );

        }
        finally {

            // Allow another pagination request after the current request finishes.
            isLoading = false;
        }
    }


    // ==========================================================
    // SCROLL HANDLING
    // ==========================================================

    // Listen for scrolling anywhere on the PetFeed page.
    window.addEventListener("scroll", function () {

        // Calculate the remaining distance between the user's current
        // position and the bottom of the document.
        const distanceFromBottom =
            document.documentElement.scrollHeight -
            (window.innerHeight + window.scrollY);

        // Request the next page when the user is within 400 pixels of
        // the bottom of the feed.
        if (distanceFromBottom <= 400) {
            loadMorePosts();
        }

        // Save the latest scroll position using the throttled save function.
        // This means the state is updated while the user is scrolling,
        // without constantly writing to sessionStorage.
        scheduleStateSave();

    });


    // ==========================================================
    // NAVIGATION STATE SAVING
    // ==========================================================

    // pagehide fires when the current document is being unloaded or
    // moved into the browser's back/forward cache.
    window.addEventListener("pagehide", function () {

        // Save the final feed HTML, pagination state, and scroll position
        // before navigating to another page.
        saveFeedState();

    });

});
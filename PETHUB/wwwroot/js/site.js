// ==========================================
// PETHUB SIDEBAR
// ==========================================

document.addEventListener("DOMContentLoaded", function () {

    const sidebarToggle = document.getElementById("sidebarToggle");
    const sidebarWrapper = document.getElementById("sidebar-wrapper");

    if (sidebarToggle && sidebarWrapper) {

        const storageKey = "pethubSidebarCollapsed";
        const savedState = localStorage.getItem(storageKey);


        // ==========================================
        // ADDED:
        // Disable the sidebar transition temporarily
        // while restoring the saved state.
        //
        // This prevents the sidebar from animating
        // every time a new page is loaded.
        // ==========================================

        sidebarWrapper.style.transition = "none";


        // ==========================================
        // RESTORE SIDEBAR STATE
        // ==========================================

        // Sidebar starts open when no previous preference exists.
        if (savedState === "true") {

            sidebarWrapper.classList.add("collapsed");

        } else {

            sidebarWrapper.classList.remove("collapsed");

        }


        // ==========================================
        // ADDED:
        // Force the browser to apply the restored
        // sidebar width before turning the animation
        // back on.
        // ==========================================

        sidebarWrapper.offsetHeight;


        // ==========================================
        // ADDED:
        // Re-enable the normal CSS transition.
        //
        // From this point onward, the sidebar will
        // animate normally when the USER clicks
        // the toggle button.
        // ==========================================

        sidebarWrapper.style.transition = "";


        // ==========================================
        // TOGGLE SIDEBAR
        // ==========================================

        // Toggle and remember the user's selected state.
        sidebarToggle.addEventListener("click", function () {

            sidebarWrapper.classList.toggle("collapsed");

            const isCollapsed =
                sidebarWrapper.classList.contains("collapsed");

            localStorage.setItem(storageKey, isCollapsed);
        });
    }


    // ==========================================
    // INITIALIZE LUCIDE ICONS
    // ==========================================

    if (window.lucide) {
        lucide.createIcons();
    }

});
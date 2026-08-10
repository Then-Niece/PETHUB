// ==========================================
// PETHUB SIDEBAR
// ==========================================

document.addEventListener("DOMContentLoaded", function () {

    const sidebarToggle = document.getElementById("sidebarToggle");
    const sidebarWrapper = document.getElementById("sidebar-wrapper");

    if (sidebarToggle && sidebarWrapper) {

        const storageKey = "pethubSidebarCollapsed";
        const savedState = localStorage.getItem(storageKey);

        // Sidebar starts open when no previous preference exists.
        if (savedState === "true") {
            sidebarWrapper.classList.add("collapsed");
        } else {
            sidebarWrapper.classList.remove("collapsed");
        }

        // Toggle and remember the user's selected state.
        sidebarToggle.addEventListener("click", function () {

            sidebarWrapper.classList.toggle("collapsed");

            const isCollapsed =
                sidebarWrapper.classList.contains("collapsed");

            localStorage.setItem(storageKey, isCollapsed);
        });
    }

    // Initialize Lucide icons.
    if (window.lucide) {
        lucide.createIcons();
    }

});
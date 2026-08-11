// ==========================================
// PETHUB SIDEBAR
// ==========================================

document.addEventListener("DOMContentLoaded", function () {

    const sidebarToggle =
        document.getElementById("sidebarToggle");

    const sidebarWrapper =
        document.getElementById("sidebar-wrapper");

    const storageKey =
        "pethubSidebarCollapsed";


    if (sidebarToggle && sidebarWrapper) {

        const savedState =
            localStorage.getItem(storageKey);


        // ==========================================
        // RESTORE SIDEBAR STATE
        // ==========================================

        if (savedState === "true") {

            sidebarWrapper.classList.add("collapsed");

        }
        else {

            sidebarWrapper.classList.remove("collapsed");

        }


        // ==========================================
        // REMOVE INITIAL-LOAD STATE
        // ==========================================
        //
        // The sidebar has now received its correct
        // collapsed/expanded state.
        //
        // We can allow the normal CSS transition
        // again.
        // ==========================================

        requestAnimationFrame(function () {

            document.documentElement.classList.remove(
                "sidebar-loading",
                "sidebar-collapsed"
            );

        });


        // ==========================================
        // TOGGLE SIDEBAR
        // ==========================================

        sidebarToggle.addEventListener("click", function () {

            sidebarWrapper.classList.toggle("collapsed");

            const isCollapsed =
                sidebarWrapper.classList.contains("collapsed");


            // Save the user's preference
            localStorage.setItem(
                storageKey,
                isCollapsed
            );

        });

    }


    // ==========================================
    // INITIALIZE LUCIDE ICONS
    // ==========================================

    if (window.lucide) {
        lucide.createIcons();
    }

});
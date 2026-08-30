// ==========================================
// PETHUB SIDEBAR
// ==========================================
//
// This file contains all JavaScript related
// to the PETHUB sidebar.
//
// Responsibilities:
// 1. Restore collapsed/expanded sidebar state
// 2. Toggle sidebar collapse/expand
// 3. Remember sidebar state
// 4. Switch Admin between Admin View and
//    Member View
// 5. Remember Member View across page navigation
// 6. Initialize Lucide icons
//
// ==========================================

document.addEventListener("DOMContentLoaded", function () {

    // ==========================================
    // SIDEBAR ELEMENTS
    // ==========================================

    const sidebarToggle = document.getElementById("sidebarToggle");

    const sidebarWrapper = document.getElementById("sidebar-wrapper");

    // ==========================================
    // STORAGE KEYS
    // ==========================================

    /*
     * Stores whether the sidebar was previously
     * collapsed.
     *
     * true  = collapsed
     * false = expanded
     */
    const sidebarStorageKey = "pethubSidebarCollapsed";

    /*
     * Stores whether the Admin is currently using
     * Member View.
     *
     * This is intentionally separate from the
     * sidebar collapsed/expanded state.
     *
     * true  = Member View
     * false = Admin View
     */
    const memberViewStorageKey = "pethubAdminMemberView";

    // ==========================================
    // SIDEBAR COLLAPSE / EXPAND
    // ==========================================

    /*
     * Only run the collapse/expand logic when
     * the sidebar and toggle button exist.
     */
    if (sidebarToggle && sidebarWrapper) {

        const savedState = localStorage.getItem(sidebarStorageKey);

        // ==========================================
        // RESTORE SIDEBAR STATE
        // ==========================================

        /*
         * The layout has already placed the
         * "sidebar-loading" and "sidebar-collapsed"
         * classes on <html> before the page is
         * displayed.
         *
         * Here we apply the actual "collapsed"
         * class to the sidebar wrapper.
         */

        if (savedState === "true")
        {

            sidebarWrapper.classList.add("collapsed");

        }
        else {

            sidebarWrapper.classList.remove("collapsed");

        }


        // ==========================================
        // REMOVE INITIAL-LOAD STATE
        // ==========================================

        /*
         * The correct sidebar state has now been
         * restored.
         *
         * We can allow the normal CSS transition
         * to work again.
         *
         * requestAnimationFrame() waits until the
         * browser has processed the initial layout.
         */

        requestAnimationFrame(function () {

            document.documentElement.classList.remove(
                "sidebar-loading",
                "sidebar-collapsed"
            );

        });


        // ==========================================
        // TOGGLE SIDEBAR
        // ==========================================

        sidebarToggle.addEventListener(
            "click",
            function () {

                const isMobile =
                    window.innerWidth <= 768;


                // ==========================================
                // MOBILE
                // ==========================================

                if (isMobile) {

                    sidebarWrapper.classList.toggle(
                        "mobile-open"
                    );


                    const overlay =
                        document.getElementById(
                            "sidebarOverlay"
                        );


                    overlay?.classList.toggle(
                        "open"
                    );


                    return;
                }


                // ==========================================
                // DESKTOP
                // ==========================================

                sidebarWrapper.classList.toggle(
                    "collapsed"
                );


                const isCollapsed =
                    sidebarWrapper.classList.contains(
                        "collapsed"
                    );


                localStorage.setItem(
                    sidebarStorageKey,
                    isCollapsed
                );
            }
        );

    }

    // ==========================================
    // ADMIN / MEMBER VIEW
    // ==========================================

    /*
     * These elements only exist in the Admin
     * sidebar.
     *
     * The regular Member sidebar does not have
     * the toggle, so the code below safely does
     * nothing for normal Members.
     */

    const memberViewToggle = document.getElementById("memberViewToggle");

    const memberNavigation = document.getElementById("memberNavigation");

    const adminNavigation = document.getElementById("adminNavigation");


    /*
     * Make sure all required elements exist
     * before attempting to use the toggle.
     */
    if (memberViewToggle && memberNavigation && adminNavigation)
    {

        // ==========================================
        // SWITCH SIDEBAR VIEW
        // ==========================================

        /*
         * This function controls which navigation
         * section is visible.
         *
         * true:
         *     Member View
         *
         * false:
         *     Admin View
         */
        function updateSidebarView(isMemberView) {

            if (isMemberView) {

                // Show Member navigation.
                memberNavigation.style.display = "";

                // Hide Admin navigation.
                adminNavigation.style.display = "none";

            }
            else
            {

                // Hide Member navigation.
                memberNavigation.style.display = "none";

                // Show Admin navigation.
                adminNavigation.style.display = "";

            }

        }
        
        // ==========================================
        // RESTORE MEMBER VIEW STATE
        // ==========================================

        /*
         * Read the Admin's previous view preference
         * from localStorage.
         *
         * localStorage stores values as strings,
         * so we compare the value with "true".
         *
         * If nothing has been saved yet, the result
         * will be false, which means Admin View.
         */
        const savedMemberView = localStorage.getItem(memberViewStorageKey);

        const isMemberView = savedMemberView === "true";

        /*
         * Restore the switch itself.
         *
         * This keeps the visual state of the switch
         * synchronized with the navigation.
         */
        memberViewToggle.checked = isMemberView;

        /*
         * Restore the correct navigation.
         */
        updateSidebarView(isMemberView);


        // ==========================================
        // MEMBER VIEW TOGGLE
        // ==========================================

        /*
         * Run whenever the Admin changes the switch.
         */
        memberViewToggle.addEventListener("change", function ()
        {

                const isMemberView = this.checked;
                
                /*
                 * Immediately update the navigation.
                 */
                updateSidebarView(isMemberView);

                /*
                 * Save the new preference so it survives
                 * normal page navigation.
                 *
                 * Example:
                 *
                 * Member View ON
                 *     ↓
                 * "pethubAdminMemberView" = "true"
                 *
                 * Member View OFF
                 *     ↓
                 * "pethubAdminMemberView" = "false"
                 */
                localStorage.setItem(memberViewStorageKey, isMemberView);

            }
        );

    }


    // ==========================================
    // MOBILE SIDEBAR OVERLAY
    // ==========================================

    const sidebarOverlay =
        document.getElementById(
            "sidebarOverlay"
        );


    if (sidebarOverlay && sidebarWrapper) {

        sidebarOverlay.addEventListener(
            "click",
            function () {

                sidebarWrapper.classList.remove(
                    "mobile-open"
                );


                sidebarOverlay.classList.remove(
                    "open"
                );
            }
        );
    }


    // ==========================================
    // INITIALIZE LUCIDE ICONS
    // ==========================================

    /*
     * Initialize Lucide icons after the sidebar
     * has been rendered.
     */

    if (window.lucide) {

        lucide.createIcons();

    }



    if (sidebarWrapper) {

        const sidebarLinks =
            sidebarWrapper.querySelectorAll(
                ".sidebar-link"
            );


        sidebarLinks.forEach(
            function (link) {

                link.addEventListener(
                    "click",
                    function () {

                        if (
                            window.innerWidth > 768
                        ) {
                            return;
                        }


                        sidebarWrapper.classList.remove(
                            "mobile-open"
                        );


                        sidebarOverlay?.classList.remove(
                            "open"
                        );
                    }
                );
            }
        );
    }


});
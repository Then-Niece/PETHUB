// ==========================================================
// PETHUB CUSTOM SELECT
//
// Reusable custom dropdown component.
//
// This script does NOT control what data a dropdown contains.
// It only controls the visual dropdown interface.
//
// Can be reused for:
// - Address fields
// - Marketplace filters
// - Lost & Found filters
// - Profile forms
// - Admin forms
// - Other select fields
// ==========================================================

document.addEventListener("DOMContentLoaded", function () {

    // Find every real select that uses the
    // reusable PetHub custom-select component.
    document
        .querySelectorAll(".pethub-select-native")
        .forEach(function (realSelect) {

            setupCustomSelect(realSelect);

        });


    // Refresh Lucide icons if available.
    if (window.lucide) {
        lucide.createIcons();
    }

});


// ==========================================================
// SETUP CUSTOM SELECT
// ==========================================================

function setupCustomSelect(realSelect) {

    const selectId = realSelect.id;


    if (!selectId) {
        return;
    }


    const wrapper =
        realSelect.closest(
            ".pethub-select-wrapper"
        );


    if (!wrapper) {
        return;
    }


    const trigger =
        wrapper.querySelector(
            `.pethub-select-trigger[data-select-target="${selectId}"]`
        );


    const menu =
        wrapper.querySelector(
            `.pethub-select-menu[data-menu-for="${selectId}"]`
        );


    if (!trigger || !menu) {
        return;
    }


    const textElement =
        trigger.querySelector(
            ".pethub-select-text"
        );


    if (!textElement) {
        return;
    }


    // ======================================================
    // UPDATE THE TEXT / STATE
    // ======================================================

    function updateTrigger() {

        const selectedOption =
            realSelect.options[
            realSelect.selectedIndex
            ];


        const hasValue =
            selectedOption &&
            selectedOption.value !== "";


        if (hasValue) {

            textElement.textContent =
                selectedOption.textContent.trim();


            trigger.classList.remove(
                "placeholder"
            );

        }
        else {

            const placeholder =
                realSelect.options.length > 0
                    ? realSelect.options[0]
                        .textContent
                        .trim()
                    : "Select";


            textElement.textContent =
                placeholder;


            trigger.classList.add(
                "placeholder"
            );

        }


        // Match disabled state of real select.
        trigger.disabled =
            realSelect.disabled;


        trigger.classList.toggle(
            "disabled",
            realSelect.disabled
        );

    }


    // ======================================================
    // CREATE VISUAL OPTIONS
    // ======================================================

    function renderMenu() {

        menu.innerHTML = "";


        Array
            .from(realSelect.options)
            .forEach(function (option) {

                // Skip empty placeholder option.
                if (option.value === "") {
                    return;
                }


                const item =
                    document.createElement(
                        "button"
                    );


                item.type =
                    "button";


                item.className =
                    "pethub-select-option";


                item.textContent =
                    option.textContent;


                item.dataset.value =
                    option.value;


                if (option.selected) {

                    item.classList.add(
                        "active"
                    );

                }


                // ==========================================
                // USER SELECTS AN OPTION
                // ==========================================

                item.addEventListener(
                    "click",
                    function () {

                        realSelect.value =
                            option.value;


                        // Important:
                        // This allows other scripts such as
                        // address-helper.js to detect the
                        // selection.
                        realSelect.dispatchEvent(
                            new Event(
                                "change",
                                {
                                    bubbles: true
                                }
                            )
                        );


                        updateTrigger();

                        renderMenu();

                        closeMenu();

                    }
                );


                menu.appendChild(item);

            });

    }


    // ======================================================
    // OPEN MENU
    // ======================================================

    function openMenu() {

        if (realSelect.disabled) {
            return;
        }


        closeAllCustomSelectMenus();


        // Rebuild before opening in case another script
        // changed the options.
        renderMenu();


        menu.classList.add(
            "show"
        );


        trigger.classList.add(
            "open"
        );

    }


    // ======================================================
    // CLOSE MENU
    // ======================================================

    function closeMenu() {

        menu.classList.remove(
            "show"
        );


        trigger.classList.remove(
            "open"
        );

    }


    // ======================================================
    // TRIGGER CLICK
    // ======================================================

    trigger.addEventListener(
        "click",
        function () {

            if (
                trigger.classList.contains(
                    "open"
                )
            ) {

                closeMenu();

            }
            else {

                openMenu();

            }

        }
    );


    // ======================================================
    // REAL SELECT CHANGED
    //
    // Allows other JavaScript to change the real select.
    // ======================================================

    realSelect.addEventListener(
        "change",
        function () {

            updateTrigger();

            renderMenu();

        }
    );


    // ======================================================
    // WATCH FOR DYNAMIC CHANGES
    //
    // Important for things such as:
    //
    // Province selected
    //      ↓
    // address-helper.js adds Cities
    //      ↓
    // this observer notices those new options
    // ======================================================

    const observer =
        new MutationObserver(
            function () {

                updateTrigger();

                renderMenu();

            }
        );


    observer.observe(
        realSelect,
        {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: [
                "disabled"
            ]
        }
    );


    // ======================================================
    // INITIAL DISPLAY
    // ======================================================

    updateTrigger();

    renderMenu();

}


// ==========================================================
// CLOSE ALL CUSTOM SELECTS
// ==========================================================

function closeAllCustomSelectMenus() {

    document
        .querySelectorAll(
            ".pethub-select-menu.show"
        )
        .forEach(function (menu) {

            menu.classList.remove(
                "show"
            );

        });


    document
        .querySelectorAll(
            ".pethub-select-trigger.open"
        )
        .forEach(function (trigger) {

            trigger.classList.remove(
                "open"
            );

        });

}


// ==========================================================
// CLICK OUTSIDE
// ==========================================================

document.addEventListener(
    "click",
    function (event) {

        if (
            event.target.closest(
                ".pethub-select-wrapper"
            )
        ) {
            return;
        }


        closeAllCustomSelectMenus();

    }
);


// ==========================================================
// ESCAPE KEY
// ==========================================================

document.addEventListener(
    "keydown",
    function (event) {

        if (event.key === "Escape") {

            closeAllCustomSelectMenus();

        }

    }
);
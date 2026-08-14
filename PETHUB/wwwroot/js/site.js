// ==========================================
// PETHUB PASSWORD VISIBILITY
// ==========================================
//
// Reusable helper for showing and hiding
// password fields.
//
// Any password input can use this helper
// as long as its toggle button contains:
//
// data-password-target="inputId"
//
// Example:
//
// <input id="loginPassword" type="password">
//
// <button
//     class="password-toggle"
//     data-password-target="loginPassword">
// </button>
//
// ==========================================

document.addEventListener("DOMContentLoaded", function () {

    /*
     * Find every button that has the
     * "password-toggle" class.
     *
     * This allows the same JavaScript to work
     * with multiple password fields throughout
     * the application.
     */
    const passwordToggles = document.querySelectorAll(".password-toggle");

    /*
     * If there are no password toggle buttons
     * on the current page, there is nothing
     * for this helper to do.
     */
    if (passwordToggles.length === 0) {
        return;
    }

    // ==========================================
    // SETUP EACH PASSWORD TOGGLE
    // ==========================================

    passwordToggles.forEach(function (toggle) {

        /*
         * Get the ID of the password input that
         * this button controls.
         *
         * Example:
         *
         * data-password-target="loginPassword"
         *
         * gives us:
         *
         * "loginPassword"
         */
        const targetId = toggle.dataset.passwordTarget;

        /*
         * Find the actual password input using
         * the ID we just retrieved.
         */
        const passwordInput = document.getElementById(targetId);

        /*
         * If the target input cannot be found,
         * skip this toggle instead of causing
         * a JavaScript error.
         */
        if (!passwordInput) {
            return;
        }

        // ==========================================
        // TOGGLE PASSWORD VISIBILITY
        // ==========================================

        toggle.addEventListener("click", function () {

            /*
             * Check whether the password is
             * currently hidden.
             *
             * password = hidden
             * text     = visible
             */
            const isHidden = passwordInput.type === "password";

            if (isHidden)
            {

                // ==================================
                // SHOW PASSWORD
                // ==================================

                passwordInput.type = "text";

                /*
                 * Update the accessibility label.
                 */
                toggle.setAttribute(
                    "aria-label",
                    "Hide password"
                );

                /*
                 * Change the icon from "eye"
                 * to "eye-off".
                 */
                const icon = toggle.querySelector("[data-lucide]");

                if (icon)
                {
                    icon.setAttribute("data-lucide","eye-off");
                }

            }
            else
            {

                // ==================================
                // HIDE PASSWORD
                // ==================================

                passwordInput.type = "password";

                /*
                 * Update the accessibility label.
                 */
                toggle.setAttribute(
                    "aria-label",
                    "Show password"
                );

                /*
                 * Change the icon back to "eye".
                 */
                const icon = toggle.querySelector("[data-lucide]");

                if (icon)
                {
                    icon.setAttribute("data-lucide","eye");
                }

            }

            /*
             * Re-render the Lucide icon after
             * changing data-lucide.
             */
            if (window.lucide) {
                lucide.createIcons();
            }

        });

    });

});
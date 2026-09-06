document.addEventListener(
    "DOMContentLoaded",
    function () {

        const darkModeToggle =
            document.getElementById(
                "darkModeToggle"
            );


        // Dark mode toggle only exists
        // on the Settings page.
        if (!darkModeToggle) {
            return;
        }


        // =====================================================
        // MATCH TOGGLE WITH CURRENT SAVED THEME
        // =====================================================

        darkModeToggle.checked =
            document.documentElement
                .classList
                .contains("dark-mode");


        // =====================================================
        // DARK MODE TOGGLE
        // =====================================================

        darkModeToggle.addEventListener(
            "change",
            async function () {

                const wantsDarkMode =
                    darkModeToggle.checked;


                const newTheme =
                    wantsDarkMode
                        ? "Dark"
                        : "Light";


                // Remember the previous state
                // in case saving fails.
                const previousTheme =
                    wantsDarkMode
                        ? "Light"
                        : "Dark";


                // =================================================
                // APPLY THEME IMMEDIATELY
                // =================================================

                if (wantsDarkMode) {

                    document.documentElement
                        .classList
                        .add("dark-mode");

                }
                else {

                    document.documentElement
                        .classList
                        .remove("dark-mode");

                }


                // =================================================
                // GET ANTI-FORGERY TOKEN
                // =================================================

                const token =
                    document.querySelector(
                        "#themeTokenForm " +
                        "input[name='__RequestVerificationToken']"
                    )?.value;


                if (!token) {

                    console.error(
                        "Theme anti-forgery token was not found."
                    );

                    restorePreviousTheme(
                        previousTheme
                    );

                    return;
                }


                // =================================================
                // PREPARE FORM DATA
                // =================================================

                const formData =
                    new FormData();


                formData.append(
                    "theme",
                    newTheme
                );


                formData.append(
                    "__RequestVerificationToken",
                    token
                );


                // =================================================
                // SAVE THEME TO DATABASE
                // =================================================

                try {

                    const response =
                        await fetch(
                            "/UserAccount/UpdateTheme",
                            {
                                method: "POST",
                                body: formData
                            }
                        );


                    if (!response.ok) {

                        throw new Error(
                            "Unable to save theme preference."
                        );
                    }


                    const result =
                        await response.json();


                    if (!result.success) {

                        throw new Error(
                            result.message ||
                            "Unable to save theme preference."
                        );
                    }

                }
                catch (error) {

                    console.error(
                        "Theme save failed:",
                        error
                    );


                    restorePreviousTheme(
                        previousTheme
                    );

                }

            }
        );


        // =====================================================
        // RESTORE THEME IF DATABASE SAVE FAILS
        // =====================================================

        function restorePreviousTheme(
            previousTheme
        ) {

            const wasDark =
                previousTheme === "Dark";


            darkModeToggle.checked =
                wasDark;


            if (wasDark) {

                document.documentElement
                    .classList
                    .add("dark-mode");

            }
            else {

                document.documentElement
                    .classList
                    .remove("dark-mode");

            }

        }

    }
);
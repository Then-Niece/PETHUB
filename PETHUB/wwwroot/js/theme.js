document.addEventListener(
    "DOMContentLoaded",
    function () {

        const darkModeToggle =
            document.getElementById(
                "darkModeToggle"
            );

        const themeStorageKey =
            "pethubTheme";


        // Match the toggle with the current theme.
        if (darkModeToggle) {

            darkModeToggle.checked =
                document.documentElement
                    .classList
                    .contains("dark-mode");


            darkModeToggle.addEventListener(
                "change",
                function () {

                    if (darkModeToggle.checked) {

                        document.documentElement
                            .classList.add(
                                "dark-mode"
                            );

                        localStorage.setItem(
                            themeStorageKey,
                            "dark"
                        );

                    }
                    else {

                        document.documentElement
                            .classList.remove(
                                "dark-mode"
                            );

                        localStorage.setItem(
                            themeStorageKey,
                            "light"
                        );

                    }

                }
            );
        }

    }
);
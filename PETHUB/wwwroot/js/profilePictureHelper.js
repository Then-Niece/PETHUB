
// ============================================================
// Profile Picture Helper
// ============================================================
// Handles:
// - Opening the file picker
// - Previewing the selected profile picture
// - Removing the selected/current profile picture
// - Restoring the original profile picture
// ============================================================

function setupProfilePicture(options = {}) {

    // ------------------------------------------------------------
    // Default element IDs
    // ------------------------------------------------------------

    const settings = {

        inputId: "profilePictureInput",

        previewId: "profilePicturePreview",

        changeButtonId: "changeProfilePictureBtn",

        removeButtonId: "removeProfilePictureBtn",

        removeInputId: "RemoveProfilePicture",

        defaultImage: "/images/default_profile.jpg",

        ...options

    };
    // ------------------------------------------------------------
    // Get HTML elements
    // ------------------------------------------------------------

    const fileInput = document.getElementById(settings.inputId);

    const preview = document.getElementById(settings.previewId);

    const changeButton = document.getElementById(settings.changeButtonId);

    const removeButton = document.getElementById(settings.removeButtonId);

    const removeInput = document.getElementById(settings.removeInputId);

    // ------------------------------------------------------------
    // Safety check
    // ------------------------------------------------------------

    if (!fileInput || !preview) {
        return;
    }

    // ------------------------------------------------------------
    // CHANGE PROFILE PICTURE BUTTON
    // ------------------------------------------------------------

    if (changeButton) {

        changeButton.addEventListener("click", function () {

            fileInput.click();

        });

    }


    // ------------------------------------------------------------
    // PROFILE PICTURE SELECTED
    // ------------------------------------------------------------

    fileInput.addEventListener("change", function () {

        // Make sure a file was selected.
        if (!this.files || !this.files[0]) {
            return;
        }


        const file = this.files[0];


        // --------------------------------------------------------
        // A new picture was selected.
        //
        // Therefore, cancel "Remove Profile Picture".
        // --------------------------------------------------------

        if (removeInput) {
            removeInput.value = false;
        }


        // --------------------------------------------------------
        // Preview the selected image.
        // --------------------------------------------------------

        const reader = new FileReader();

        reader.onload = function (e) {

            preview.src = e.target.result;

        };

        reader.readAsDataURL(file);

    });


    // ------------------------------------------------------------
    // REMOVE PROFILE PICTURE BUTTON
    // ------------------------------------------------------------

    if (removeButton) {

        removeButton.addEventListener("click", function () {

            // ----------------------------------------------------
            // Mark the profile picture for removal.
            // ----------------------------------------------------

            if (removeInput) {
                removeInput.value = true;
            }

            // ----------------------------------------------------
            // Remove the selected file from the input.
            //
            // This is important because otherwise the browser
            // may still submit the selected image.
            // ----------------------------------------------------

            fileInput.value = "";


            // ----------------------------------------------------
            // Show the default anonymous profile picture.
            // ----------------------------------------------------

            preview.src = settings.defaultImage;

        });

    }

}
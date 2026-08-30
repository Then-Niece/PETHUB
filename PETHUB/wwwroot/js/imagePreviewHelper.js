// =========================================================
// IMAGE PREVIEW HELPER
// =========================================================
//
// Reusable image preview helper.
//
// Supports:
// - Multiple image selection
// - Single image selection
// - Removing selected images
// - Maximum file count
// - Configurable preview size
// - Optional placeholder
//
// =========================================================

function setupImagePreview(inputId, previewContainerId, options = {}) {

    // Each uploader gets its own selected files.
    let selectedFiles = [];


    // =====================================================
    // SETTINGS
    // =====================================================

    const settings = {

        // Multiple images by default
        multiple: true,

        // Existing images already stored in database
        existingCount: 0,

        // null = unlimited
        maxFiles: null,

        // Preview dimensions
        previewWidth: 120,
        previewHeight: 120,

        // Optional placeholder element
        placeholderId: null,

        ...options
    };


    const imageInput =
        document.getElementById(inputId);

    const previewContainer =
        document.getElementById(previewContainerId);


    // Prevent errors on pages that don't have this uploader
    if (!imageInput || !previewContainer) {
        return;
    }


    // =====================================================
    // WHEN USER SELECTS FILES
    // =====================================================

    imageInput.addEventListener("change", function () {

        const newFiles =
            Array.from(this.files);


        if (settings.multiple) {

            selectedFiles.push(...newFiles);


            // =================================================
            // MAX FILE LIMIT
            // =================================================

            if (
                settings.maxFiles !== null &&
                selectedFiles.length > settings.maxFiles
            ) {

                selectedFiles =
                    selectedFiles.slice(
                        0,
                        settings.maxFiles
                    );

                showImageLimitMessage();
            }

        }
        else {

            // Single image mode
            selectedFiles =
                newFiles.slice(0, 1);
        }


        updateRealInput();

        renderPreviews();
    });


    // =====================================================
    // UPDATE ACTUAL FILE INPUT
    // =====================================================

    function updateRealInput() {

        const dataTransfer =
            new DataTransfer();


        selectedFiles.forEach(file => {

            dataTransfer.items.add(file);

        });


        imageInput.files =
            dataTransfer.files;
    }


    // =====================================================
    // RENDER IMAGE PREVIEWS
    // =====================================================

    function renderPreviews() {

        previewContainer.innerHTML = "";


        selectedFiles.forEach(file => {

            // Preview wrapper
            const previewDiv =
                document.createElement("div");


            previewDiv.classList.add(
                "image-preview-item"
            );


            previewDiv.style.width =
                `${settings.previewWidth}px`;

            previewDiv.style.height =
                `${settings.previewHeight}px`;


            // =================================================
            // IMAGE
            // =================================================

            const img =
                document.createElement("img");


            const objectUrl =
                URL.createObjectURL(file);


            img.src = objectUrl;

            img.classList.add(
                "image-preview-img"
            );


            // Release temporary browser URL after loading
            img.onload = function () {

                URL.revokeObjectURL(objectUrl);

            };


            // =================================================
            // REMOVE BUTTON
            // =================================================

            const removeBtn =
                document.createElement("button");


            removeBtn.type = "button";

            removeBtn.innerHTML = "&times;";

            removeBtn.classList.add(
                "remove-image-btn"
            );


            removeBtn.addEventListener(
                "click",
                function () {

                    const index =
                        selectedFiles.indexOf(file);


                    if (index !== -1) {

                        selectedFiles.splice(
                            index,
                            1
                        );
                    }


                    updateRealInput();

                    renderPreviews();
                }
            );


            previewDiv.appendChild(img);

            previewDiv.appendChild(
                removeBtn
            );


            previewContainer.appendChild(
                previewDiv
            );
        });


        updatePlaceholder();
    }


    // =====================================================
    // MAX IMAGE WARNING
    // =====================================================

    function showImageLimitMessage() {

        if (!settings.maxFiles) {
            return;
        }


        /*
         * Check parent because the warning is inserted
         * beside the preview container, not inside it.
         */
        let message =
            previewContainer.parentElement
                ?.querySelector(
                    ".image-limit-message"
                );


        if (!message) {

            message =
                document.createElement("div");


            message.classList.add(
                "image-limit-message"
            );


            previewContainer.parentElement
                ?.insertBefore(
                    message,
                    previewContainer
                );
        }


        message.textContent =
            `You can send up to ${settings.maxFiles} photos at a time.`;



        // Remove old timer if user triggers warning repeatedly
        if (message._removeTimer) {

            clearTimeout(
                message._removeTimer
            );
        }


        message._removeTimer =
            setTimeout(function () {

                message.remove();

            }, 3000);
    }


    // =====================================================
    // OPTIONAL PLACEHOLDER
    // =====================================================

    function updatePlaceholder() {

        if (!settings.placeholderId) {
            return;
        }


        const placeholder =
            document.getElementById(
                settings.placeholderId
            );


        if (!placeholder) {
            return;
        }


        placeholder.style.display =
            selectedFiles.length > 0
                ? "none"
                : "block";
    }


    // =====================================================
    // ALLOW OTHER JS FILES TO RESET THIS UPLOADER
    // =====================================================

    imageInput.resetImagePreview = function () {

        selectedFiles = [];

        updateRealInput();

        renderPreviews();
    };


}
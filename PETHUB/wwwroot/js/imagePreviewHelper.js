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
// - Allowed file types
// - Maximum file size
// - Configurable preview size
// - Optional placeholder
// - Optional upload count
// - Dynamic layout count
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

        // null = unlimited
        maxFileSize: null,

        // Example:
        // ["image/jpeg", "image/png", "image/webp"]
        allowedTypes: null,

        // Preview dimensions
        previewWidth: 120,
        previewHeight: 120,

        // Optional placeholder element
        placeholderId: null,

        // Optional count element
        countId: null,

        // Optional error element
        errorId: null,

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

        clearError();

        const newFiles =
            Array.from(this.files);


        if (settings.multiple) {

            for (const file of newFiles) {

                // -----------------------------------------
                // MAX FILE COUNT
                // -----------------------------------------

                const totalCount =
                    settings.existingCount +
                    selectedFiles.length;


                if (
                    settings.maxFiles !== null &&
                    totalCount >= settings.maxFiles
                ) {

                    showError(
                        `You can upload a maximum of ${settings.maxFiles} photos.`
                    );

                    break;
                }


                // -----------------------------------------
                // FILE TYPE
                // -----------------------------------------

                if (
                    settings.allowedTypes !== null &&
                    !settings.allowedTypes.includes(file.type)
                ) {

                    showError(
                        `"${file.name}" is not a supported image type.`
                    );

                    continue;
                }


                // -----------------------------------------
                // FILE SIZE
                // -----------------------------------------

                if (
                    settings.maxFileSize !== null &&
                    file.size > settings.maxFileSize
                ) {

                    showError(
                        `"${file.name}" exceeds the allowed file size.`
                    );

                    continue;
                }


                selectedFiles.push(file);
            }

        }
        else {

            const file =
                newFiles[0];


            if (!file) {
                return;
            }


            if (
                settings.allowedTypes !== null &&
                !settings.allowedTypes.includes(file.type)
            ) {

                showError(
                    `"${file.name}" is not a supported image type.`
                );

                updateRealInput();
                renderPreviews();

                return;
            }


            if (
                settings.maxFileSize !== null &&
                file.size > settings.maxFileSize
            ) {

                showError(
                    `"${file.name}" exceeds the allowed file size.`
                );

                updateRealInput();
                renderPreviews();

                return;
            }


            selectedFiles = [file];
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


        // Used by CSS for dynamic layout.
        previewContainer.dataset.count =
            selectedFiles.length;


        selectedFiles.forEach(file => {

            // Preview wrapper
            const previewDiv =
                document.createElement("div");


            previewDiv.classList.add(
                "image-preview-item"
            );


            // Only apply fixed dimensions
            // if custom dimensions are provided.
            if (settings.previewWidth) {

                previewDiv.style.width =
                    `${settings.previewWidth}px`;
            }


            if (settings.previewHeight) {

                previewDiv.style.height =
                    `${settings.previewHeight}px`;
            }


            // =================================================
            // IMAGE
            // =================================================

            const img =
                document.createElement("img");


            const objectUrl =
                URL.createObjectURL(file);


            img.src =
                objectUrl;


            img.classList.add(
                "image-preview-img"
            );


            // Release temporary browser URL after loading
            img.onload =
                function () {

                    URL.revokeObjectURL(
                        objectUrl
                    );

                };


            // =================================================
            // REMOVE BUTTON
            // =================================================

            const removeBtn =
                document.createElement("button");


            removeBtn.type =
                "button";


            removeBtn.innerHTML =
                "&times;";


            removeBtn.classList.add(
                "remove-image-btn"
            );


            removeBtn.addEventListener(
                "click",
                function () {

                    const index =
                        selectedFiles.indexOf(
                            file
                        );


                    if (index !== -1) {

                        selectedFiles.splice(
                            index,
                            1
                        );
                    }


                    updateRealInput();

                    renderPreviews();

                    clearError();
                }
            );


            previewDiv.appendChild(
                img
            );


            previewDiv.appendChild(
                removeBtn
            );


            previewContainer.appendChild(
                previewDiv
            );
        });


        updatePlaceholder();

        updateCount();
    }


    // =====================================================
    // COUNT DISPLAY
    // =====================================================

    function updateCount() {

        if (!settings.countId) {
            return;
        }


        const countElement =
            document.getElementById(
                settings.countId
            );


        if (!countElement) {
            return;
        }


        const totalSelected =
            settings.existingCount +
            selectedFiles.length;


        if (settings.maxFiles !== null) {

            countElement.textContent =
                `${totalSelected} / ${settings.maxFiles} photos`;

        }
        else {

            countElement.textContent =
                `${totalSelected} photo${totalSelected === 1 ? "" : "s"}`;

        }
    }


    // =====================================================
    // ERROR MESSAGE
    // =====================================================

    function showError(messageText) {

        if (settings.errorId) {

            const errorElement =
                document.getElementById(
                    settings.errorId
                );


            if (errorElement) {

                errorElement.textContent =
                    messageText;

                return;
            }
        }


        showImageLimitMessage(
            messageText
        );
    }


    function clearError() {

        if (!settings.errorId) {
            return;
        }


        const errorElement =
            document.getElementById(
                settings.errorId
            );


        if (errorElement) {

            errorElement.textContent = "";
        }
    }


    // =====================================================
    // FALLBACK WARNING MESSAGE
    // =====================================================

    function showImageLimitMessage(
        messageText
    ) {

        let message =
            previewContainer.parentElement
                ?.querySelector(
                    ".image-limit-message"
                );


        if (!message) {

            message =
                document.createElement(
                    "div"
                );


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
            messageText;


        if (message._removeTimer) {

            clearTimeout(
                message._removeTimer
            );
        }


        message._removeTimer =
            setTimeout(
                function () {

                    message.remove();

                },
                3000
            );
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


        if (selectedFiles.length > 0) {

            placeholder.style.display = "none";

        }
        else {

            // Let CSS decide whether this should be
            // flex, grid, block, etc.
            placeholder.style.display = "";

        }
    }

    // =====================================================
    // ALLOW OTHER JS FILES TO RESET THIS UPLOADER
    // =====================================================

    imageInput.resetImagePreview =
        function () {

            selectedFiles = [];

            updateRealInput();

            renderPreviews();

            clearError();
        };


    // Initial UI state
    renderPreviews();
}
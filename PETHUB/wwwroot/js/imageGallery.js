/* ==========================================================
   PETHUB - SHARED IMAGE GALLERY
   ========================================================== */

(function () {

    function createGallery(images, startIndex) {

        if (!images || images.length === 0) {
            return;
        }

        let currentIndex = startIndex || 0;

        // Remove an existing gallery if one is already open
        const existingGallery = document.getElementById("sharedImageGallery");

        if (existingGallery) {
            existingGallery.remove();
        }

        // Create gallery overlay
        const overlay = document.createElement("div");
        overlay.id = "sharedImageGallery";
        overlay.className = "shared-image-gallery";

        overlay.innerHTML = `
            <div class="shared-gallery-backdrop"></div>

            <div class="shared-gallery-content">

                <button type="button"
                        class="shared-gallery-close"
                        aria-label="Close gallery">
                    &times;
                </button>

                <button type="button"
                        class="shared-gallery-prev"
                        aria-label="Previous image">
                    &#10094;
                </button>

                <img class="shared-gallery-image"
                     src=""
                     alt="Gallery image">

                <button type="button"
                        class="shared-gallery-next"
                        aria-label="Next image">
                    &#10095;
                </button>

                <div class="shared-gallery-counter"></div>

            </div>
        `;

        document.body.appendChild(overlay);

        const galleryImage =
            overlay.querySelector(".shared-gallery-image");

        const counter =
            overlay.querySelector(".shared-gallery-counter");

        const previousButton =
            overlay.querySelector(".shared-gallery-prev");

        const nextButton =
            overlay.querySelector(".shared-gallery-next");

        function showImage(index) {

            if (index < 0) {
                index = images.length - 1;
            }

            if (index >= images.length) {
                index = 0;
            }

            currentIndex = index;

            galleryImage.src = images[currentIndex];

            counter.textContent =
                `${currentIndex + 1} / ${images.length}`;

            if (images.length <= 1) {
                previousButton.style.display = "none";
                nextButton.style.display = "none";
            }
            else {
                previousButton.style.display = "flex";
                nextButton.style.display = "flex";
            }
        }

        function closeGallery() {
            overlay.remove();
        }

        previousButton.addEventListener(
            "click",
            function (event) {

                event.stopPropagation();

                showImage(currentIndex - 1);
            }
        );

        nextButton.addEventListener(
            "click",
            function (event) {

                event.stopPropagation();

                showImage(currentIndex + 1);
            }
        );

        overlay.querySelector(".shared-gallery-close")
            .addEventListener(
                "click",
                function () {
                    closeGallery();
                }
            );

        overlay.querySelector(".shared-gallery-backdrop")
            .addEventListener(
                "click",
                function () {
                    closeGallery();
                }
            );

        document.addEventListener(
            "keydown",
            function handleKeyboard(event) {

                if (!document.getElementById("sharedImageGallery")) {
                    document.removeEventListener(
                        "keydown",
                        handleKeyboard
                    );

                    return;
                }

                if (event.key === "Escape") {
                    closeGallery();
                }

                if (event.key === "ArrowLeft") {
                    showImage(currentIndex - 1);
                }

                if (event.key === "ArrowRight") {
                    showImage(currentIndex + 1);
                }
            }
        );

        showImage(currentIndex);
    }


    /*
     * Public function
     *
     * Other pages can call:
     *
     * openImageGallery(images, index);
     */
    window.openImageGallery = function (images, index) {
        createGallery(images, index);
    };


    /*
     * Automatically makes images with
     * .gallery-image clickable.
     */
    window.setupImageGallery = function (container) {

        if (!container) {
            return;
        }

        function attachGalleryToImages() {

            const imageElements =
                Array.from(
                    container.querySelectorAll("img")
                );

            if (imageElements.length === 0) {
                return;
            }

            const images =
                imageElements.map(function (image) {
                    return image.src;
                });

            imageElements.forEach(
                function (image, index) {

                    // Prevent attaching the event more than once
                    if (image.dataset.galleryAttached === "true") {
                        return;
                    }

                    image.dataset.galleryAttached = "true";

                    image.style.cursor = "pointer";

                    image.addEventListener(
                        "click",
                        function (event) {

                            event.preventDefault();
                            event.stopPropagation();

                            // Get the latest images
                            const currentImages =
                                Array.from(
                                    container.querySelectorAll("img")
                                ).map(function (img) {
                                    return img.src;
                                });

                            const currentIndex =
                                currentImages.indexOf(image.src);

                            openImageGallery(
                                currentImages,
                                currentIndex >= 0
                                    ? currentIndex
                                    : index
                            );
                        }
                    );
                }
            );
        }


        // Attach to images that already exist
        attachGalleryToImages();


        /*
         * Watch for new preview images created by
         * imagePreviewHelper.js.
         */
        const observer =
            new MutationObserver(function () {

                attachGalleryToImages();

            });

        observer.observe(
            container,
            {
                childList: true,
                subtree: true
            }
        );

    };

})();
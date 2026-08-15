//LEARN THIS CODE!!!

// This is an image preview helper function that allows users to preview selected images before uploading them.
// It also provides a remove button for each image preview, allowing users to remove images from the selection.
//
// Fixed problem: This doesnt reset the preview images
//
// NEW:
// Added an optional "options" parameter so the same helper can work for both:
// - Multiple image uploads (default behavior - existing pages won't break)
// - Single image uploads (ex. ID Photo in Register page)

function setupImagePreview(inputId, previewContainerId, options = {}) {

   
    // Each uploader gets its own file storage.
    // Prevents the pet images and ID image from mixing together.
    let selectedFiles = [];

  
    // Default settings.
    // Existing pages automatically use multiple = true, so no changes are needed.
    const settings = {
        multiple: true,

      
        // Number of existing database photos on Edit pages.
        existingCount: 0,

        ...options
    };

    const imageInput = document.getElementById(inputId);
    const previewContainer = document.getElementById(previewContainerId);

    if (!imageInput || !previewContainer) return;

    imageInput.addEventListener("change", function () {

      
        // If this input only accepts one image,
        // replace the previous image instead of adding another one.
        if (settings.multiple) {

            // Existing behavior for multiple image uploads.
            selectedFiles.push(...Array.from(this.files));

        } else {

         
            // For single image uploads (ID Photo),
            // keep only the newly selected file.
            selectedFiles = Array.from(this.files);

        }

        // Update the real input
        const dataTransfer = new DataTransfer();

        selectedFiles.forEach(file => {
            dataTransfer.items.add(file);
        });

        imageInput.files = dataTransfer.files;

        renderPreviews();

    });

    function renderPreviews() {

        previewContainer.innerHTML = "";

      
        // Existing photos already use some of the 4 visible spaces.
        const visibleSlots =
            Math.max(0, 4 - settings.existingCount);

       
        // Count all photos that are hidden from the preview.
        const hiddenCount =
            Math.max(
                0,
                settings.existingCount +
                selectedFiles.length -
                4
            );

        const visibleFiles =
            selectedFiles.slice(0, visibleSlots);

  
        // Keep track of loaded previews so +more appears last.
        let loadedImages = 0;

        visibleFiles.forEach((file, index) => {

            const reader = new FileReader();

            reader.onload = function (e) {

                const previewDiv = document.createElement("div");
                previewDiv.classList.add("position-relative", "border", "rounded");
                previewDiv.style.width = "120px";
                previewDiv.style.height = "120px";
                previewDiv.style.overflow = "hidden";

                const img = document.createElement("img");
                img.src = e.target.result;
                img.classList.add("w-100", "h-100", "object-fit-cover");

                const removeBtn = document.createElement("button");

             
                // Prevent the remove button from submitting the form.
                removeBtn.type = "button";

                removeBtn.innerHTML = "&times;";

                removeBtn.classList.add("remove-image-btn");

                removeBtn.style.top = "0";
                removeBtn.style.right = "0";
                removeBtn.style.borderRadius = "50%";

                removeBtn.addEventListener("click", () => {

                    selectedFiles.splice(index, 1);

                    const dt = new DataTransfer();

                    selectedFiles.forEach(f => dt.items.add(f));

                    imageInput.files = dt.files;

                    renderPreviews();

                
                    // Show placeholder again if all images are removed.
                    if (selectedFiles.length === 0 && settings.placeholderId) {

                        const placeholder = document.getElementById(settings.placeholderId);

                        if (placeholder) {
                            placeholder.style.display = "block";
                        }

                    }

                });

                previewDiv.appendChild(img);
                previewDiv.appendChild(removeBtn);
                previewContainer.appendChild(previewDiv);

             
                // Hide placeholder when an image is selected.
                // If a page does not have a placeholder, nothing happens.
                if (settings.placeholderId) {

                    const placeholder = document.getElementById(settings.placeholderId);

                    if (placeholder) {
                        placeholder.style.display = "none";
                    }

                }

               
                // Wait until all visible images are rendered.
                loadedImages++;

                if (loadedImages === visibleFiles.length) {

             
                    // Put +more after the visible photos.
                    if (hiddenCount > 0) {

                        const more = document.createElement("div");

                        more.classList.add("more-images");

                        more.textContent = `+${hiddenCount} more photos`;

                        previewContainer.appendChild(more);

                    }

                }

            };

            reader.readAsDataURL(file);

        });

      
        // Handles cases where there are no new photos to preview.
        if (visibleFiles.length === 0 && hiddenCount > 0) {

            const more = document.createElement("div");

            more.classList.add("more-images");

            more.textContent = `+${hiddenCount} more photos`;

            previewContainer.appendChild(more);

        }

    }

}
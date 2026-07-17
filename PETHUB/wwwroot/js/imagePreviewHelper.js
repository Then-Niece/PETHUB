//This is an image preview helper function that allows users to preview selected images before uploading them. It also provides a remove button for each image preview, allowing users to remove images from the selection.
//Fixed problem: This doesnt reset the preview images

//LEARN THIS CODE!!!

let selectedFiles = [];

function setupImagePreview(inputId, previewContainerId) {
    const imageInput = document.getElementById(inputId);
    const previewContainer = document.getElementById(previewContainerId);

    if (!imageInput || !previewContainer) return;

    imageInput.addEventListener("change", function () {

        // Add newly selected files
        selectedFiles.push(...Array.from(this.files));

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

        selectedFiles.forEach((file, index) => {

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
                removeBtn.innerHTML = "×";
                removeBtn.classList.add(
                    "btn",
                    "btn-sm",
                    "btn-danger",
                    "position-absolute"
                );

                removeBtn.style.top = "0";
                removeBtn.style.right = "0";
                removeBtn.style.borderRadius = "50%";

                removeBtn.addEventListener("click", () => {

                    selectedFiles.splice(index, 1);

                    const dt = new DataTransfer();

                    selectedFiles.forEach(f => dt.items.add(f));

                    imageInput.files = dt.files;

                    renderPreviews();

                });

                previewDiv.appendChild(img);
                previewDiv.appendChild(removeBtn);
                previewContainer.appendChild(previewDiv);

            };

            reader.readAsDataURL(file);

        });
    }
}
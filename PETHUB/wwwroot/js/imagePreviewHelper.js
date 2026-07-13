//This is an image preview helper function that allows users to preview selected images before uploading them. It also provides a remove button for each image preview, allowing users to remove images from the selection.
function setupImagePreview(inputId, previewContainerId) {
    const imageInput = document.getElementById(inputId);
    const previewContainer = document.getElementById(previewContainerId);

    if (!imageInput || !previewContainer) return;

    imageInput.addEventListener("change", function () {
        previewContainer.innerHTML = "";

        Array.from(this.files).forEach((file, index) => {
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
                removeBtn.classList.add("btn", "btn-sm", "btn-danger", "position-absolute");
                removeBtn.style.top = "0";
                removeBtn.style.right = "0";
                removeBtn.style.borderRadius = "50%";

                removeBtn.addEventListener("click", () => {
                    const dataTransfer = new DataTransfer();
                    const files = Array.from(imageInput.files);
                    files.splice(index, 1);
                    files.forEach(f => dataTransfer.items.add(f));
                    imageInput.files = dataTransfer.files;
                    previewDiv.remove();
                });

                previewDiv.appendChild(img);
                previewDiv.appendChild(removeBtn);
                previewContainer.appendChild(previewDiv);
            };
            reader.readAsDataURL(file);
        });
    });
}

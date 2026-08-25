document.addEventListener("DOMContentLoaded", function () {
    const priceInput = document.querySelector('#priceGroup input[name="Price"]');
    const pesoSign = document.getElementById("pesoSign");

    if (!priceInput || !pesoSign) return;

    function updatePesoSign() {
        if (priceInput.value.trim() !== "") {
            pesoSign.style.display = "block";
            priceInput.classList.add("has-price");
        } else {
            pesoSign.style.display = "none";
            priceInput.classList.remove("has-price");
        }
    }

    priceInput.addEventListener("input", updatePesoSign);

    // Handles Edit.cshtml when Price already has a value
    updatePesoSign();
});
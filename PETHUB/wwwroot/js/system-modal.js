// =========================================================
// PETHUB SHARED SYSTEM MODAL
// =========================================================

window.showSystemModal = function (options = {}) {

    const modalElement =
        document.getElementById("systemModal");

    const iconContainer =
        document.getElementById("systemModalIcon");

    const titleElement =
        document.getElementById("systemModalTitle");

    const messageElement =
        document.getElementById("systemModalMessage");

    const primaryButton =
        document.getElementById("systemModalPrimaryButton");


    if (
        !modalElement ||
        !iconContainer ||
        !titleElement ||
        !messageElement ||
        !primaryButton
    ) {
        return;
    }


    const type =
        options.type || "error";

    const title =
        options.title || "Something went wrong";

    const message =
        options.message || "Please try again.";

    const buttonText =
        options.buttonText || "Okay";


    // =====================================================
    // ICON
    // =====================================================

    const iconMap = {
        error: "triangle-alert",
        success: "circle-check",
        warning: "circle-alert",
        info: "info"
    };


    iconContainer.className =
        `system-modal-icon ${type}`;


    iconContainer.innerHTML =
        `<i data-lucide="${iconMap[type] || iconMap.error}"></i>`;


    // =====================================================
    // CONTENT
    // =====================================================

    titleElement.textContent =
        title;


    messageElement.textContent =
        message;


    primaryButton.textContent =
        buttonText;


    // =====================================================
    // REFRESH LUCIDE ICON
    // =====================================================

    if (window.lucide) {
        lucide.createIcons();
    }


    // =====================================================
    // SHOW MODAL
    // =====================================================

    const modal =
        bootstrap.Modal.getOrCreateInstance(
            modalElement
        );


    modal.show();
};
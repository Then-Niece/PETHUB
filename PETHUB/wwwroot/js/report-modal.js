// Wait until the HTML document has been loaded before locating
// the shared Report modal and its form controls.
document.addEventListener("DOMContentLoaded", function () {

    // Locate the reusable Report modal rendered by _ReportModal.cshtml.
    // The same modal is used by Marketplace and Lost & Found cards/details pages.
    const reportModal = document.getElementById("reportModal");

    // Stop initialization if the shared modal is not present.
    // This prevents JavaScript errors on pages where the partial is not rendered.
    if (!reportModal) {
        return;
    }

    // Locate the hidden fields and visible controls inside the Report modal.
    const reportType = document.getElementById("reportContentType");
    const reportId = document.getElementById("reportContentId");
    const reportTitle = document.getElementById("reportPostTitle");
    const reportReason = document.getElementById("reportReason");
    const otherReasonContainer = document.getElementById("otherReasonContainer");
    const otherReason = document.getElementById("otherReason");

    // Bootstrap fires "show.bs.modal" immediately before the modal opens.
    // event.relatedTarget is the Report button that caused the modal to open.
    // The button's data-report-* attributes identify the reported post.
    reportModal.addEventListener("show.bs.modal", function (event) {

        // Get the specific Report button that opened the shared modal.
        const button = event.relatedTarget;

        if (!button) {
            return;
        }

        // Copy the reported content type and ID from the button into
        // the hidden form fields that will be submitted to ReportsController.
        reportType.value = button.getAttribute("data-report-type");
        reportId.value = button.getAttribute("data-report-id");

        // Display the title of the reported post so the member can
        // confirm which listing or Lost & Found report they selected.
        const title = button.getAttribute("data-report-title");

        reportTitle.textContent = title
            ? `Reporting: ${title}`
            : "";

        // Reset the reason and custom reason whenever a new report is opened.
        // This prevents values from a previous report from remaining in the form.
        reportReason.value = "";
        otherReason.value = "";

        // Hide the custom reason field by default.
        otherReasonContainer.classList.add("d-none");

        // The custom reason is only required when "Other" is selected.
        otherReason.removeAttribute("required");
    });

    // Show or hide the custom reason field when the member changes
    // the selected report reason.
    reportReason.addEventListener("change", function () {

        // UserReportReason.Other has the enum value 7.
        const isOther = reportReason.value === "7";

        if (isOther) {

            // Show the custom reason input when Other is selected.
            otherReasonContainer.classList.remove("d-none");

            // Add browser-level required validation for the custom reason.
            // The server-side ViewModel validation remains the final authority.
            otherReason.setAttribute("required", "required");

        } else {

            // Hide and clear the custom reason when another predefined
            // reason is selected.
            otherReasonContainer.classList.add("d-none");
            otherReason.removeAttribute("required");
            otherReason.value = "";
        }
    });
});
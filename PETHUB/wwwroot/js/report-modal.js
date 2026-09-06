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

    // Hidden native select used by the backend.
    const reportReason = document.getElementById("reportReason");

    // Custom dropdown controls.
    const reportReasonDropdown =
        document.getElementById("reportReasonDropdown");

    const reportReasonTrigger =
        document.getElementById("reportReasonTrigger");

    const reportReasonText =
        document.getElementById("reportReasonText");

    const reportReasonMenu =
        document.getElementById("reportReasonMenu");

    const reportReasonOptions =
        document.querySelectorAll(".report-dropdown-option");

    // Other reason controls.
    const otherReasonContainer =
        document.getElementById("otherReasonContainer");

    const otherReason =
        document.getElementById("otherReason");


    // ==========================================================
    // CUSTOM DROPDOWN
    // ==========================================================

    // Open or close the custom dropdown when the trigger is clicked.
    reportReasonTrigger.addEventListener("click", function () {

        const isOpen =
            reportReasonDropdown.classList.contains("open");

        if (isOpen) {

            reportReasonDropdown.classList.remove("open");
            reportReasonTrigger.setAttribute("aria-expanded", "false");

        } else {

            reportReasonDropdown.classList.add("open");
            reportReasonTrigger.setAttribute("aria-expanded", "true");

        }
    });


    // Handle selection of a report reason.
    reportReasonOptions.forEach(function (option) {

        option.addEventListener("click", function () {

            const selectedValue =
                option.getAttribute("data-value");

            const selectedText =
                option.textContent.trim();


            // Update the hidden native select.
            // This preserves the existing backend field:
            // name="Reason"
            reportReason.value = selectedValue;


            // Update the visible custom dropdown text.
            reportReasonText.textContent = selectedText;


            // Mark the selected option.
            reportReasonOptions.forEach(function (item) {

                item.classList.remove("selected");

                item.setAttribute(
                    "aria-selected",
                    "false"
                );

            });

            option.classList.add("selected");

            option.setAttribute(
                "aria-selected",
                "true"
            );


            // Remove any previous validation error.
            reportReasonDropdown.classList.remove(
                "validation-error"
            );


            // Close the dropdown.
            reportReasonDropdown.classList.remove("open");

            reportReasonTrigger.setAttribute(
                "aria-expanded",
                "false"
            );


            // Trigger the existing reason-change behavior.
            // This preserves the existing "Other" logic.
            reportReason.dispatchEvent(
                new Event("change")
            );
        });
    });


    // Close the custom dropdown when clicking outside it.
    document.addEventListener("click", function (event) {

        if (!reportReasonDropdown.contains(event.target)) {

            reportReasonDropdown.classList.remove("open");

            reportReasonTrigger.setAttribute(
                "aria-expanded",
                "false"
            );
        }
    });


    // ==========================================================
    // REPORT MODAL OPEN
    // ==========================================================

    // Bootstrap fires "show.bs.modal" immediately before the modal opens.
    // event.relatedTarget is the Report button that caused the modal to open.
    reportModal.addEventListener("show.bs.modal", function (event) {

        // Get the specific Report button that opened the shared modal.
        const button = event.relatedTarget;

        if (!button) {
            return;
        }


        // Copy the reported content type and ID from the button into
        // the hidden form fields that will be submitted to ReportsController.
        reportType.value =
            button.getAttribute("data-report-type");

        reportId.value =
            button.getAttribute("data-report-id");


        // Display the title of the reported post.
        const title =
            button.getAttribute("data-report-title");

        reportTitle.textContent = title
            ? `Reporting: ${title}`
            : "";


        // ==========================================================
        // RESET REASON
        // ==========================================================

        // Reset the hidden backend select.
        reportReason.value = "";


        // Reset the visible custom dropdown.
        reportReasonText.textContent = "Select a reason";

        reportReasonOptions.forEach(function (option) {

            option.classList.remove("selected");

            option.setAttribute(
                "aria-selected",
                "false"
            );
        });


        // Remove any previous validation error.
        reportReasonDropdown.classList.remove(
            "validation-error"
        );


        // Close the custom dropdown.
        reportReasonDropdown.classList.remove("open");

        reportReasonTrigger.setAttribute(
            "aria-expanded",
            "false"
        );


        // ==========================================================
        // RESET OTHER REASON
        // ==========================================================

        otherReason.value = "";

        // Hide the custom reason field by default.
        otherReasonContainer.classList.add("d-none");

        // The custom reason is only required when "Other" is selected.
        otherReason.removeAttribute("required");
    });


    // ==========================================================
    // OTHER REASON
    // ==========================================================

    // Show or hide the custom reason field when the member changes
    // the selected report reason.
    reportReason.addEventListener("change", function () {

        // UserReportReason.Other has the enum value 7.
        const isOther =
            reportReason.value === "7";


        if (isOther) {

            // Show the custom reason input when Other is selected.
            otherReasonContainer.classList.remove("d-none");

            // Add browser-level required validation for the custom reason.
            // The server-side ViewModel validation remains the final authority.
            otherReason.setAttribute(
                "required",
                "required"
            );

        } else {

            // Hide and clear the custom reason when another
            // predefined reason is selected.
            otherReasonContainer.classList.add("d-none");

            otherReason.removeAttribute("required");

            otherReason.value = "";
        }
    });


    // ==========================================================
    // FORM VALIDATION AND DUPLICATE REPORT CHECK
    // ==========================================================

    // Since the visible dropdown is custom, validate that a reason
    // has been selected before allowing the form to submit.
    const reportForm =
        reportModal.querySelector(".report-form");

    reportForm.addEventListener("submit", async function (event) {

        // ======================================================
        // REASON VALIDATION
        // ======================================================

        if (!reportReason.value) {

            event.preventDefault();

            reportReasonDropdown.classList.add(
                "validation-error"
            );

            reportReasonTrigger.focus();

            reportReasonDropdown.classList.add(
                "open"
            );

            reportReasonTrigger.setAttribute(
                "aria-expanded",
                "true"
            );

            return;
        }

        reportReasonDropdown.classList.remove(
            "validation-error"
        );


        // ======================================================
        // SUBMIT REPORT
        // ======================================================

        // Prevent the normal form submission so we can check
        // the server response for a duplicate report.
        event.preventDefault();

        try {

            const response = await fetch(
                reportForm.action,
                {
                    method: "POST",
                    body: new FormData(reportForm),
                    credentials: "same-origin"
                }
            );


            // ======================================================
            // DUPLICATE REPORT
            // ======================================================

            if (response.status === 409) {

                // ReportsController returns this message when
                // the current Member has already reported this post.
                const message = await response.text();

                alert(
                    message ||
                    "You have already reported this post."
                );


                // Get the type and ID of the post that was reported.
                const contentType =
                    reportType.value;

                const contentId =
                    reportId.value;


                // ==================================================
                // RETURN TO THE ORIGINAL POST
                // ==================================================

                // Lost & Found post.
                if (
                    contentType === "LostFound" ||
                    contentType === "LostFounds"
                ) {

                    window.location.href =
                        `/LostFounds/BrowseDetails/${contentId}`;

                }

                // Marketplace post.
                else if (
                    contentType === "Listing" ||
                    contentType === "Listings"
                ) {

                    window.location.href =
                        `/Listings/MarketplaceDetails/${contentId}`;

                }

                // Fallback if the content type is unexpected.
                else {

                    window.location.reload();

                }

                return;
            }


            // ======================================================
            // SUCCESS
            // ======================================================

            if (response.ok) {

                // ReportsController redirects the Member to Home
                // after successfully creating the report.
                if (response.redirected) {

                    window.location.href =
                        response.url;

                    return;
                }

                // Fallback if the server returns a successful
                // response without a redirect.
                window.location.reload();

                return;
            }


            // ======================================================
            // OTHER SERVER ERROR
            // ======================================================

            alert(
                "Something went wrong while submitting your report. Please try again."
            );

        }
        catch (error) {

            console.error(
                "Report submission error:",
                error
            );

            alert(
                "Something went wrong while submitting your report. Please try again."
            );
        }
    });
});
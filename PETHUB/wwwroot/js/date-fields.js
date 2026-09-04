document.addEventListener("DOMContentLoaded", function () {

    document
        .querySelectorAll(".pethub-date-fields")
        .forEach(function (component) {

            setupDateField(component);

        });

});



function setupDateField(component) {

    const monthSelect =
        component.querySelector(".pethub-date-month");

    const daySelect =
        component.querySelector(".pethub-date-day");

    const yearSelect =
        component.querySelector(".pethub-date-year");

    const hiddenInput =
        component.querySelector(".pethub-date-hidden");

    const errorElement =
        component.querySelector(".pethub-date-error");


    if (
        !monthSelect ||
        !daySelect ||
        !yearSelect ||
        !hiddenInput
    ) {
        return;
    }


    const allowFuture =
        component.dataset.allowFuture === "true";


    const minimumAge =
        component.dataset.minimumAge
            ? parseInt(component.dataset.minimumAge)
            : null;


    const minimumYear =
        component.dataset.minimumYear
            ? parseInt(component.dataset.minimumYear)
            : 1900;


    const today = new Date();

    today.setHours(0, 0, 0, 0);



    // =====================================================
    // YEAR RANGE
    // =====================================================

    let maximumYear;


    if (minimumAge !== null) {

        maximumYear =
            today.getFullYear() - minimumAge;

    }
    else if (!allowFuture) {

        maximumYear =
            today.getFullYear();

    }
    else {

        maximumYear =
            today.getFullYear() + 20;

    }



    // =====================================================
    // EXISTING VALUES
    // =====================================================

    const existingMonth =
        parseInt(monthSelect.dataset.selected || monthSelect.value);

    const existingDay =
        parseInt(daySelect.dataset.selected || 0);

    const existingYear =
        parseInt(yearSelect.dataset.selected || 0);



    // =====================================================
    // YEARS
    // =====================================================

    function populateYears() {

        const currentValue =
            yearSelect.value ||
            existingYear;


        yearSelect.innerHTML =
            `<option value="">Year</option>`;


        for (
            let year = maximumYear;
            year >= minimumYear;
            year--
        ) {

            const option =
                document.createElement("option");

            option.value = year;
            option.textContent = year;


            if (
                parseInt(currentValue) === year
            ) {

                option.selected = true;

            }


            yearSelect.appendChild(option);

        }

    }



    // =====================================================
    // DAYS
    // =====================================================

    function populateDays() {

        const currentDay =
            parseInt(
                daySelect.value ||
                existingDay
            );


        const month =
            parseInt(monthSelect.value);


        const year =
            parseInt(yearSelect.value);


        let maximumDay = 31;


        if (month) {

            const yearForCalculation =
                year || today.getFullYear();


            maximumDay =
                new Date(
                    yearForCalculation,
                    month,
                    0
                ).getDate();

        }


        daySelect.innerHTML =
            `<option value="">Day</option>`;


        for (
            let day = 1;
            day <= maximumDay;
            day++
        ) {

            const option =
                document.createElement("option");

            option.value = day;
            option.textContent = day;


            if (currentDay === day) {

                option.selected = true;

            }


            daySelect.appendChild(option);

        }


        // If previous day is no longer valid
        if (
            currentDay > maximumDay
        ) {

            daySelect.value =
                maximumDay;

        }


        daySelect.dispatchEvent(
            new Event(
                "change",
                { bubbles: true }
            )
        );

    }



    // =====================================================
    // FINAL VALUE
    // =====================================================

    function updateHiddenDate() {

        hiddenInput.value = "";

        errorElement.textContent = "";


        if (
            !monthSelect.value ||
            !daySelect.value ||
            !yearSelect.value
        ) {

            return;

        }


        const month =
            parseInt(monthSelect.value);

        const day =
            parseInt(daySelect.value);

        const year =
            parseInt(yearSelect.value);


        const selectedDate =
            new Date(
                year,
                month - 1,
                day
            );


        selectedDate.setHours(
            0,
            0,
            0,
            0
        );



        // FUTURE DATE
        if (
            !allowFuture &&
            selectedDate > today
        ) {

            errorElement.textContent =
                "Date cannot be in the future.";

            return;

        }



        // MINIMUM AGE
        if (minimumAge !== null) {

            let age =
                today.getFullYear() -
                selectedDate.getFullYear();


            const monthDifference =
                today.getMonth() -
                selectedDate.getMonth();


            if (
                monthDifference < 0 ||
                (
                    monthDifference === 0 &&
                    today.getDate() <
                    selectedDate.getDate()
                )
            ) {

                age--;

            }


            if (age < minimumAge) {

                errorElement.textContent =
                    `You must be at least ${minimumAge} years old.`;

                return;

            }

        }



        const formattedMonth =
            String(month)
                .padStart(2, "0");


        const formattedDay =
            String(day)
                .padStart(2, "0");


        hiddenInput.value =
            `${year}-${formattedMonth}-${formattedDay}`;

    }



    // =====================================================
    // EVENTS
    // =====================================================

    monthSelect.addEventListener(
        "change",
        function () {

            populateDays();

            updateHiddenDate();

        }
    );


    yearSelect.addEventListener(
        "change",
        function () {

            populateDays();

            updateHiddenDate();

        }
    );


    daySelect.addEventListener(
        "change",
        updateHiddenDate
    );



    // =====================================================
    // INITIALIZE
    // =====================================================

    populateYears();

    populateDays();

    updateHiddenDate();

}
// ==========================================================
// Start the Address Helper once the page loads.
// ==========================================================
document.addEventListener("DOMContentLoaded", function () {

    const provinceDropdown =
        document.getElementById("Province");

    // Load all address information.
    loadAddressData();

    // When the Province changes,
    // populate the corresponding Cities.
    if (provinceDropdown) {

        provinceDropdown.addEventListener("change", function () {

            populateCityDropdown(this.value);
            // Reset Barangays whenever the Province changes.
            populateBarangayDropdown("", "");
        });

        const cityDropdown =
            document.getElementById("City");

        if (cityDropdown) {

            cityDropdown.addEventListener("change", function () {

                populateBarangayDropdown(
                    provinceDropdown.value,
                    this.value
                );

            });

        }
    }

});

// ==========================================================
// Address Helper
//
// Handles the Province -> City -> Barangay cascading
// dropdowns used throughout PETHUB.
//
// This script is reusable for:
// - Register
// - My Profile
// - Marketplace
// - Lost & Found
// ==========================================================


// Stores all address data returned by the API.
let addressData = {};


// ==========================================================
// Loads all address information from the server.
// ==========================================================
async function loadAddressData() {

    try {

        // Request the complete address hierarchy.
        const response = await fetch("/api/address/locations");

        // Convert the JSON response into a JavaScript object.
        addressData = await response.json();

        // Populate the Province dropdown.
        populateProvinceDropdown();

    }
    catch (error) {

        console.error("Unable to load address data.", error);

    }

}

// ==========================================================
// Populates the Province dropdown.
// ==========================================================
function populateProvinceDropdown() {

    const provinceDropdown =
        document.getElementById("Province");

    if (!provinceDropdown)
        return;

    // Read the saved Province from the HTML.
    const selectedProvince =
        provinceDropdown.dataset.selected;

    // Clear existing options except the first one.
    provinceDropdown.length = 1;

    // Add every Province returned by the API.
    Object.keys(addressData).forEach(province => {

        const option = document.createElement("option");

        option.value = province;
        option.text = province;

        provinceDropdown.appendChild(option);

    });

    // Restore the saved Province.
    if (selectedProvince) {

        provinceDropdown.value = selectedProvince;

        populateCityDropdown(selectedProvince);

    }

}

// ==========================================================
// Populates the City dropdown based on the selected Province.
// ==========================================================
function populateCityDropdown(selectedProvince) {

    const cityDropdown =
        document.getElementById("City");

    const barangayDropdown =
        document.getElementById("Barangay");

    if (!cityDropdown || !barangayDropdown)
        return;

    // Reset the City dropdown.
    cityDropdown.length = 1;

    // Reset the Barangay dropdown.
    barangayDropdown.length = 1;

    // Disable Barangay until a City is selected.
    barangayDropdown.disabled = true;

    // If no Province is selected,
    // disable the City dropdown as well.
    if (!selectedProvince) {

        cityDropdown.disabled = true;
        return;

    }

    // Enable the City dropdown.
    cityDropdown.disabled = false;

    // Get all Cities belonging to the selected Province.
    const cities = addressData[selectedProvince];

    if (!cities)
        return;

    // Add each City as an option.
    Object.keys(cities).forEach(city => {

        const option = document.createElement("option");

        option.value = city;
        option.text = city;

        cityDropdown.appendChild(option);

    });

    // Read the saved City from the HTML.
    const selectedCity =
        cityDropdown.dataset.selected;

    // Restore the saved City.
    if (selectedCity) {

        cityDropdown.value = selectedCity;

        // Automatically populate the Barangays.
        populateBarangayDropdown(
            selectedProvince,
            selectedCity
        );

    }

}

// ==========================================================
// Populates the Barangay dropdown based on the selected
// Province and City.
// ==========================================================
function populateBarangayDropdown(selectedProvince, selectedCity) {

    const barangayDropdown =
        document.getElementById("Barangay");

    if (!barangayDropdown)
        return;

    // Reset the dropdown while keeping
    // the "Select Barangay" option.
    barangayDropdown.length = 1;

    // Disable when no City is selected.
    if (!selectedProvince || !selectedCity) {

        barangayDropdown.disabled = true;
        return;

    }

    // Enable the Barangay dropdown.
    barangayDropdown.disabled = false;

    // Retrieve all Barangays that belong
    // to the selected City.
    const barangays =
        addressData[selectedProvince]?.[selectedCity];

    if (!barangays)
        return;

    // Add each Barangay to the dropdown.
    barangays.forEach(barangay => {

        const option = document.createElement("option");

        option.value = barangay;
        option.text = barangay;

        barangayDropdown.appendChild(option);

    });

    // Read the saved Barangay.
    const selectedBarangay =
        barangayDropdown.dataset.selected;

    // Restore the saved Barangay.
    if (selectedBarangay) {

        barangayDropdown.value =
            selectedBarangay;

    }

}
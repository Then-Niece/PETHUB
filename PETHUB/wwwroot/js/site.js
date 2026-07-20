// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {

    const step1 = document.getElementById("step1");
    const step2 = document.getElementById("step2");
    const step3 = document.getElementById("step3");

    if (!step1 || !step2 || !step3)
        return;

    const circles = document.querySelectorAll(".step-circle");

    function showStep(step) {

        // Hide all
        step1.style.display = "none";
        step2.style.display = "none";
        step3.style.display = "none";

        // Show current
        document.getElementById("step" + step).style.display = "block";

        // Reset circles
        circles.forEach(circle => {
            circle.classList.remove("active");
        });

        // Highlight completed/current
        for (let i = 0; i < step; i++) {
            circles[i].classList.add("active");
        }
    }

    // Default
    showStep(1);

    // Step 1 -> Step 2
    document.getElementById("next1")?.addEventListener("click", function () {
        showStep(2);
    });

    // Step 2 -> Step 1
    document.getElementById("back1")?.addEventListener("click", function () {
        showStep(1);
    });

    // Step 2 -> Step 3
    document.getElementById("next2")?.addEventListener("click", function () {
        showStep(3);
    });

    // Step 3 -> Step 2
    document.getElementById("back2")?.addEventListener("click", function () {
        showStep(2);
    });

});

//login validation

const loginForm = document.querySelector('form');

if (loginForm) {

    loginForm.addEventListener("submit", function (e) {

        let valid = true;

        const email = document.getElementById("emailInput");
        const password = document.getElementById("passwordInput");

        email.classList.remove("input-error");
        password.classList.remove("input-error");

        email.placeholder = "Enter your username or email";
        password.placeholder = "Enter your password";

        if (email.value.trim() == "") {
            email.classList.add("input-error");
            email.placeholder = "Email or Username is required";
            valid = false;
        }

        if (password.value.trim() == "") {
            password.classList.add("input-error");
            password.placeholder = "Password is required";
            valid = false;
        }

        if (!valid) {
            e.preventDefault();
        }

    });

}
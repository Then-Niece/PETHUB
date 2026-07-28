// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", function () {

    const toggle = document.getElementById("sidebarToggle");
    const sidebar = document.getElementById("sidebar-wrapper");

    if (toggle && sidebar) {

        toggle.addEventListener("click", function () {

            sidebar.classList.toggle("collapsed");

        });

    }

    if (window.lucide) {
        lucide.createIcons();
    }

});
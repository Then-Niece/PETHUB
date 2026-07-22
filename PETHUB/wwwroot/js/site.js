// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", function () {

    const btn = document.getElementById("sidebarToggle");
    const handle = document.getElementById("sidebarHandle");
    const sidebarContent = document.getElementById("sidebarContent");

    if (btn && sidebarContent) {
        btn.addEventListener("click", function () {
            sidebarContent.classList.toggle("collapsed");
        });
    }

    if (handle && sidebarContent) {
        handle.addEventListener("click", function () {
            sidebarContent.classList.remove("collapsed");
        });
    }

});
document.querySelectorAll('.action-menu-btn').forEach(button => {

    button.addEventListener('click', function (e) {

        e.stopPropagation();

        const menu = this.nextElementSibling;

        // Close every other menu
        document.querySelectorAll('.action-menu').forEach(m => {

            if (m !== menu) {

                m.classList.remove('show');

                // Return menu to its original parent
                if (m.dataset.placeholder) {

                    document.getElementById(m.dataset.placeholder).replaceWith(m);

                    delete m.dataset.placeholder;

                }

            }

        });

        // Close if already open
        if (menu.classList.contains('show')) {

            menu.classList.remove('show');

            return;

        }

        // Save original location
        const placeholder = document.createElement('span');

        placeholder.id = 'placeholder-' + Date.now();

        menu.parentNode.insertBefore(placeholder, menu);

        menu.dataset.placeholder = placeholder.id;

        // Move menu to body
        document.body.appendChild(menu);

        // Show temporarily
        menu.classList.add('show');

        const btnRect = this.getBoundingClientRect();

        const menuRect = menu.getBoundingClientRect();

        let top = btnRect.bottom + 8;

        // If no space below, open upward
        if (top + menuRect.height > window.innerHeight) {

            top = btnRect.top - menuRect.height - 8;

        }

        menu.style.top = top + "px";

        menu.style.left = (btnRect.right - menuRect.width) + "px";

    });

});

document.addEventListener('click', function () {

    document.querySelectorAll('.action-menu.show').forEach(menu => {

        menu.classList.remove('show');

        if (menu.dataset.placeholder) {

            document.getElementById(menu.dataset.placeholder).replaceWith(menu);

            delete menu.dataset.placeholder;

        }

    });

});

function closeAllMenus() {

    document.querySelectorAll('.action-menu.show').forEach(menu => {

        menu.classList.remove('show');

        if (menu.dataset.placeholder) {

            document.getElementById(menu.dataset.placeholder).replaceWith(menu);

            delete menu.dataset.placeholder;

        }

    });

}

// Close when page scrolls
window.addEventListener('scroll', closeAllMenus, true);

// Close when browser resizes
window.addEventListener('resize', closeAllMenus);
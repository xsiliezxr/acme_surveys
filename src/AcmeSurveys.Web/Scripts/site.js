(function () {
    'use strict';

    // El model binding de MVC exige índices consecutivos (Fields[0], Fields[1]...): se renumeran tras cada cambio.
    var INDEX_PATTERN = /Fields(\[|_)(\d+|__index__)(\]|__)/;

    function reindex(list) {
        var rows = list.querySelectorAll('[data-field-row]');
        rows.forEach(function (row, index) {
            row.querySelectorAll('[name],[id],[for],[data-valmsg-for]').forEach(function (el) {
                ['name', 'id', 'for', 'data-valmsg-for'].forEach(function (attr) {
                    var value = el.getAttribute(attr);
                    if (value) {
                        el.setAttribute(attr, value.replace(INDEX_PATTERN, function (_, open, __, close) {
                            return 'Fields' + open + index + close;
                        }));
                    }
                });
            });
        });
        updateButtons(list);
    }

    function updateButtons(list) {
        var rows = list.querySelectorAll('[data-field-row]');
        var max = parseInt(list.getAttribute('data-max-fields'), 10) || 50;
        var addButton = document.querySelector('[data-add-field]');
        if (addButton) addButton.disabled = rows.length >= max;

        rows.forEach(function (row, index) {
            var up = row.querySelector('[data-move-up]');
            var down = row.querySelector('[data-move-down]');
            var remove = row.querySelector('[data-remove-field]');
            if (up) up.disabled = index === 0;
            if (down) down.disabled = index === rows.length - 1;
            if (remove) remove.disabled = rows.length === 1;
        });
    }

    function initFieldEditor() {
        var list = document.querySelector('[data-field-list]');
        var template = document.getElementById('field-row-template');
        var addButton = document.querySelector('[data-add-field]');
        if (!list || !template || !addButton) return;

        addButton.addEventListener('click', function () {
            var fragment = template.content.cloneNode(true);
            list.appendChild(fragment);
            reindex(list);
            var rows = list.querySelectorAll('[data-field-row]');
            var nameInput = rows[rows.length - 1].querySelector('input[type="text"]');
            if (nameInput) nameInput.focus();
        });

        list.addEventListener('click', function (event) {
            var button = event.target.closest('button');
            if (!button) return;
            var row = button.closest('[data-field-row]');

            if (button.hasAttribute('data-remove-field')) {
                row.remove();
            } else if (button.hasAttribute('data-move-up') && row.previousElementSibling) {
                list.insertBefore(row, row.previousElementSibling);
            } else if (button.hasAttribute('data-move-down') && row.nextElementSibling) {
                list.insertBefore(row.nextElementSibling, row);
            } else {
                return;
            }
            reindex(list);
        });

        updateButtons(list);
    }

    function initCopyButtons() {
        document.querySelectorAll('[data-copy-target]').forEach(function (button) {
            button.addEventListener('click', function () {
                var input = document.getElementById(button.getAttribute('data-copy-target'));
                if (!input) return;

                var done = function () {
                    var original = button.textContent;
                    button.textContent = 'Copiado';
                    setTimeout(function () { button.textContent = original; }, 1500);
                };

                if (navigator.clipboard && window.isSecureContext) {
                    navigator.clipboard.writeText(input.value).then(done);
                } else {
                    input.select();
                    document.execCommand('copy');
                    done();
                }
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        initFieldEditor();
        initCopyButtons();
    });
})();

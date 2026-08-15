(function () {
    let pin = '';
    let selectedUsername = '';
    const MAX_DIGITS = 6;

    document.querySelectorAll('.hq-pin-account-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            document.querySelectorAll('.hq-pin-account-btn').forEach(function (b) { b.classList.remove('selected'); });
            btn.classList.add('selected');
            selectedUsername = btn.dataset.username;
            document.getElementById('usernameInput').value = selectedUsername;
        });
    });

    function renderDots() {
        document.querySelectorAll('#pinDots span').forEach(function (dot, i) {
            dot.classList.toggle('filled', i < pin.length);
        });
    }

    document.querySelectorAll('.hq-pin-key[data-digit]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            if (pin.length >= MAX_DIGITS) return;
            pin += btn.dataset.digit;
            document.getElementById('pinInput').value = pin;
            renderDots();
        });
    });

    var clearBtn = document.getElementById('pinClear');
    if (clearBtn) {
        clearBtn.addEventListener('click', function () {
            pin = '';
            document.getElementById('pinInput').value = pin;
            renderDots();
        });
    }

    var submitBtn = document.getElementById('pinSubmit');
    if (submitBtn) {
        submitBtn.addEventListener('click', function () {
            if (!selectedUsername) {
                alert('الرجاء اختيار الجهاز أولاً');
                return;
            }
            if (pin.length < 4) {
                alert('الرمز السري يجب أن يكون 4 أرقام على الأقل');
                return;
            }
            document.getElementById('pinForm').submit();
        });
    }
})();

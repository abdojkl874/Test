// Ticket printing via the browser's own print pipeline instead of a local
// print-bridge service: we inject the receipt markup into the current page,
// scope an @media print stylesheet so ONLY the receipt renders (everything
// else is hidden), size the page to an 80mm thermal roll with auto height,
// and call window.print(). This works with whatever printer is already set
// as the Windows/OS default — no vendor driver quirks, no separate service.
//
// For dialog-free printing on the kiosk PC, launch its browser in kiosk mode
// with silent printing enabled, e.g. (Edge/Chrome):
//   msedge.exe --kiosk-printing --kiosk "https://<host>/kiosk"
// and set the thermal printer as the OS default with its paper size set to
// the continuous/roll option (not a fixed length) so tickets aren't padded.
(function () {
    function ensureStyle() {
        if (document.getElementById('hq-print-style')) return;
        var style = document.createElement('style');
        style.id = 'hq-print-style';
        style.textContent = `
            #hq-print-root { display: none; }
            @media print {
                @page { size: 80mm auto; margin: 0; }
                html, body { margin: 0 !important; padding: 0 !important; background: #fff !important; }
                body > *:not(#hq-print-root) { display: none !important; }
                #hq-print-root { display: block !important; width: 80mm; }
                #hq-print-root .hq-receipt {
                    width: 80mm; padding: 3mm 4mm; text-align: center;
                    font-family: Tahoma, Arial, sans-serif; color: #000;
                }
                #hq-print-root .hq-receipt > :first-child { margin-top: 0 !important; }
                #hq-print-root .hq-org { font-size: 12pt; font-weight: 800; border-bottom: 1px solid #000; padding-bottom: 2mm; margin-bottom: 2mm; }
                #hq-print-root .hq-service { font-size: 15pt; font-weight: 800; margin-top: 1mm; }
                #hq-print-root .hq-label { font-size: 10pt; font-weight: 700; margin-top: 3mm; }
                #hq-print-root .hq-number { font-size: 46pt; font-weight: 800; letter-spacing: 2px; margin: 2mm 0; }
                #hq-print-root .hq-meta { font-size: 9pt; display: flex; justify-content: space-between; margin-top: 2mm; }
                #hq-print-root .hq-footer { font-size: 9pt; font-weight: 700; border-top: 1px dashed #000; margin-top: 3mm; padding-top: 2mm; }
            }`;
        document.head.appendChild(style);
    }

    function escapeHtml(s) {
        return String(s ?? '').replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    window.hospitalQueuePrint = {
        printTicket: function (number, serviceName, orgName, issuedAtIso) {
            try {
                ensureStyle();
                var issued = issuedAtIso ? new Date(issuedAtIso) : new Date();
                var date = issued.toLocaleDateString('ar-EG');
                var time = issued.toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' });

                var root = document.getElementById('hq-print-root');
                if (!root) {
                    root = document.createElement('div');
                    root.id = 'hq-print-root';
                    root.setAttribute('aria-hidden', 'true');
                    document.body.appendChild(root);
                }

                root.innerHTML =
                    '<div class="hq-receipt">' +
                    '  <div class="hq-org">' + escapeHtml(orgName || 'نظام إدارة طابور المشفى') + '</div>' +
                    '  <div class="hq-service">' + escapeHtml(serviceName) + '</div>' +
                    '  <div class="hq-label">رقم تذكرتك</div>' +
                    '  <div class="hq-number">' + escapeHtml(number) + '</div>' +
                    '  <div class="hq-meta"><span>' + escapeHtml(date) + '</span><span>' + escapeHtml(time) + '</span></div>' +
                    '  <div class="hq-footer">يرجى الانتظار حتى يتم نداء رقمك</div>' +
                    '</div>';

                window.print();
                return true;
            } catch (e) {
                console.warn('printTicket failed:', e);
                return false;
            }
        }
    };
})();

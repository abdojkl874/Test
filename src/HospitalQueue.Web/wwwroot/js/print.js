// Ticket printing via the browser's own print pipeline instead of a local
// print-bridge service: we inject the receipt markup into the current page,
// scope an @media print stylesheet so ONLY the receipt renders (everything
// else is hidden), size the page to an 80mm thermal roll with auto height,
// and call window.print(). This works with whatever printer is already set
// as the Windows/OS default — no vendor driver quirks, no separate service.
//
// The receipt's actual layout (which lines appear, in what order, size,
// alignment, bold) comes from the admin's active template (see
// Pages/Admin/ReceiptDesigner.razor) — elementsJson is that template's
// element list, serialized with HospitalQueue.Domain.Receipts.ReceiptElement
// property names (PascalCase) and string enum values.
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
                    width: 80mm; padding: 3mm 4mm;
                    font-family: Tahoma, Arial, sans-serif; color: #000;
                }
                #hq-print-root .hq-receipt > :first-child { margin-top: 0 !important; }
                #hq-print-root .hq-el { margin: 1mm 0; }
                #hq-print-root .hq-el.align-right { text-align: right; }
                #hq-print-root .hq-el.align-center { text-align: center; }
                #hq-print-root .hq-el.align-left { text-align: left; }
                #hq-print-root .hq-divider { border: none; border-top: 1px dashed #000; margin: 2mm 0; }
                #hq-print-root .hq-spacer { height: 3mm; }
            }`;
        document.head.appendChild(style);
    }

    function escapeHtml(s) {
        return String(s ?? '').replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    function alignClass(align) {
        if (align === 'End') return 'align-left';
        if (align === 'Start') return 'align-right';
        return 'align-center';
    }

    function resolveText(el, fields) {
        switch (el.Type) {
            case 'OrgName': return fields.orgName;
            case 'ServiceName': return fields.serviceName;
            case 'TicketNumber': return fields.number;
            case 'DateTimeStamp': return fields.dateText;
            case 'StaticText': return el.Text || '';
            default: return '';
        }
    }

    var defaultElements = [
        { Type: 'OrgName', Align: 'Center', FontSize: 13, Bold: true },
        { Type: 'Divider' },
        { Type: 'ServiceName', Align: 'Center', FontSize: 15, Bold: true },
        { Type: 'StaticText', Text: 'رقم تذكرتك', Align: 'Center', FontSize: 11, Bold: false },
        { Type: 'TicketNumber', Align: 'Center', FontSize: 46, Bold: true },
        { Type: 'DateTimeStamp', Align: 'Center', FontSize: 9, Bold: false },
        { Type: 'StaticText', Text: 'يرجى الانتظار حتى يتم نداء رقمك', Align: 'Center', FontSize: 9, Bold: true }
    ];

    function buildReceiptHtml(elements, fields) {
        var html = '<div class="hq-receipt">';
        elements.forEach(function (el) {
            if (el.Type === 'Divider') {
                html += '<hr class="hq-divider" />';
                return;
            }
            if (el.Type === 'Spacer') {
                html += '<div class="hq-spacer"></div>';
                return;
            }
            var text = escapeHtml(resolveText(el, fields));
            var style = 'font-size:' + (el.FontSize || 12) + 'pt; font-weight:' + (el.Bold ? 800 : 400) + ';';
            html += '<div class="hq-el ' + alignClass(el.Align) + '" style="' + style + '">' + text + '</div>';
        });
        html += '</div>';
        return html;
    }

    window.hospitalQueuePrint = {
        printTicket: function (number, serviceName, orgName, issuedAtIso, elementsJson) {
            try {
                ensureStyle();
                var issued = issuedAtIso ? new Date(issuedAtIso) : new Date();
                var dateText = issued.toLocaleDateString('ar-EG') + '   ' +
                    issued.toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' });

                var elements = defaultElements;
                if (elementsJson) {
                    try {
                        var parsed = JSON.parse(elementsJson);
                        if (Array.isArray(parsed) && parsed.length > 0) {
                            elements = parsed;
                        }
                    } catch (e) {
                        console.warn('printTicket: invalid elementsJson, using default layout', e);
                    }
                }

                var root = document.getElementById('hq-print-root');
                if (!root) {
                    root = document.createElement('div');
                    root.id = 'hq-print-root';
                    root.setAttribute('aria-hidden', 'true');
                    document.body.appendChild(root);
                }

                root.innerHTML = buildReceiptHtml(elements, {
                    number: number,
                    serviceName: serviceName,
                    orgName: orgName || 'نظام إدارة طابور المشفى',
                    dateText: dateText
                });

                window.print();
                return true;
            } catch (e) {
                console.warn('printTicket failed:', e);
                return false;
            }
        }
    };
})();

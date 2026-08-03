// Talks to the small local print-bridge service (HospitalQueue.PrintBridge)
// that must be running on the kiosk PC itself, listening on localhost.
// This file executes in the kiosk's own browser, so "localhost" here is the
// kiosk machine — not the app server — which is what lets it reach a printer
// physically attached to that PC.
window.hospitalQueuePrint = {
    endpoint: "http://localhost:9100/print",

    printTicket: async function (number, serviceName, issuedAt) {
        try {
            const res = await fetch(this.endpoint, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ number, serviceName, issuedAt })
            });
            return res.ok;
        } catch (e) {
            console.warn("Print bridge unreachable:", e);
            return false;
        }
    }
};

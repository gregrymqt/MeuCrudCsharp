// /js/modules/services/signalRService.js
import * as signalR from "@microsoft/signalr";

let connection = null;

export function initializeSignalR() {
    if (connection) return;

    const token = localStorage.getItem('authToken'); // Pega seu token de autenticação

    connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub", { // A mesma URL que você mapeou no Program.cs
            accessTokenFactory: () => token // Envia o token para autenticação no Hub
        })
        .withAutomaticReconnect() // Tenta reconectar automaticamente
        .build();

    // Ouve o método "ReceiveRefundStatus" enviado pelo backend
    connection.on("ReceiveRefundStatus", (data) => {
        console.log("Notificação de reembolso recebida:", data);

        if (data.status === 'completed') {
            const refundStep2 = document.getElementById('refund-step-2');
            const refundStep3 = document.getElementById('refund-step-3');

            if (refundStep2 && refundStep3) {
                refundStep2.style.display = 'none';
                refundStep3.style.display = 'block';

                // Notificação visual opcional
                Swal.fire({
                    icon: 'success', title: 'Reembolso Confirmado!',
                    toast: true, position: 'top-end',
                    showConfirmButton: false, timer: 3500
                });
            }
        }
    });

    // Inicia a conexão
    connection.start()
        .then(() => console.log("SignalR Connected."))
        .catch(err => console.error("SignalR Connection Error: ", err));
}
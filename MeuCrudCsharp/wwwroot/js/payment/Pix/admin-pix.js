// js/admin-pix.js
import { fetchPublicKey, postCreatePayment } from './modules/api/api.js';
import { displayPixPayment, setLoading, displayError, setupCopyButton, showSuccessScreen } from './modules/ui/ui.js';
import { initializeMercadoPago, loadIdentificationTypes } from './modules/services/mercadoPago.js';
import { createPaymentHubConnection } from './modules/services/signalr.js';

let hubConnection;

// 1. Define a função que tratará as atualizações de status vindas do Hub
function handlePaymentStatusUpdate(data) {
    // A tela de "carregando" não é necessária aqui, pois o usuário já está vendo o QR Code
    // Apenas agimos quando o pagamento é concluído
    if (data.isComplete) {
        hubConnection.stop(); // Encerra a conexão após o status final

        if (data.status === 'approved') {
            // Sucesso! Mostra a tela de confirmação.
            showSuccessScreen(data.paymentId);
        } else {
            // Se o PIX foi cancelado, expirado ou rejeitado.
            displayError(`O pagamento falhou ou foi cancelado. Status: ${data.status}`);
            // Aqui você pode adicionar lógica para permitir que o usuário gere um novo PIX.
        }
    }
}

// 2. Função principal que executa quando a página carrega
async function main() {
    try {
        // Assume que o ID do usuário está disponível globalmente, como no seu exemplo do cartão
        // Você DEVE injetar isso na sua página via backend. Ex: <script>window.paymentConfig = { userId: '@User.Identity.Name' };</script>
        if (!window.paymentConfig || !window.paymentConfig.userId) {
            throw new Error("ID do usuário não encontrado. A comunicação em tempo real não pode ser iniciada.");
        }

        // Inicia e subscreve a conexão com o Hub assim que a página carrega
        hubConnection = createPaymentHubConnection(handlePaymentStatusUpdate);
        await hubConnection.start();
        await hubConnection.subscribe(window.paymentConfig.userId);
        console.log("Conectado ao Hub de Pagamentos e aguardando atualizações.");

        // Lógica existente para carregar o formulário do MP
        const publicKey = await fetchPublicKey();
        await initializeMercadoPago(publicKey);
        await loadIdentificationTypes();
        setupCopyButton();

    } catch (error) {
        console.error("Erro na inicialização:", error);
        displayError("Não foi possível carregar a página de pagamento. Tente novamente.");
    }
}

// 3. Listener do formulário (Lógica simplificada)
const form = document.getElementById('form-checkout');
form.addEventListener('submit', async (e) => {
    e.preventDefault();
    setLoading(true);
    displayError('');

    try {
        const formData = new FormData(form);
        const paymentData = {
            transactionAmount: Number(document.getElementById('transactionAmount').value),
            description: document.getElementById('description').value,
            payer: {
                firstName: formData.get('payerFirstName'),
                lastName: formData.get('payerLastName'),
                email: formData.get('email'),
                identification: {
                    type: formData.get('identificationType'),
                    number: formData.get('identificationNumber'),
                },
            },
        };

        // A chamada à API agora só gera o PIX. A confirmação vem pelo SignalR.
        const pixResult = await postCreatePayment(paymentData);

        // Exibe o QR Code para o usuário pagar.
        displayPixPayment(pixResult);

    } catch (error) {
        displayError(error.message || "Não foi possível gerar o PIX. Verifique seus dados.");
    } finally {
        setLoading(false);
    }
});

// Inicia a aplicação
main();
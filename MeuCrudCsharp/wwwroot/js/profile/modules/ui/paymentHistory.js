import { fetchPaymentHistory } from '../api/subscriptionApi.js';

function renderPaymentHistoryTable(history) {
    const tableBody = document.querySelector('#payment-history-content tbody');
    if (!tableBody) return;

    if (history && history.length > 0) {
        // Mapeia os dados do histórico para criar as linhas <tr> da tabela
        const rowsHtml = history.map(payment => {
            const statusClass = payment.status?.toLowerCase() || 'desconhecido';
            const paymentDate = new Date(payment.createdAt).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
            const paymentAmount = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(payment.amount);
            const paymentDescription = `Pagamento Ref. ${payment.externalId?.substring(0, 8) || 'N/A'}`;

            return `
                <tr>
                    <td>${paymentDate}</td>
                    <td>${paymentDescription}</td>
                    <td>${paymentAmount}</td>
                    <td><span class="status-badge status-${statusClass}">${payment.status}</span></td>
                </tr>
            `;
        }).join('');

        tableBody.innerHTML = rowsHtml;
    } else {
        // Se não houver histórico, mostra uma mensagem amigável
        tableBody.innerHTML = '<tr><td colspan="4" style="text-align: center;">Nenhum pagamento encontrado.</td></tr>';
    }
}

/**
 * Função principal exportada. Orquestra a busca e renderização do histórico.
 */
export async function initializePaymentHistory() {
    try {
        const data = await fetchPaymentHistory();
        renderPaymentHistoryTable(data);
    } catch (error) {
        console.error('Falha ao inicializar o histórico de pagamentos:', error);
        // UX: Mostra uma mensagem de erro na tabela se a API falhar
        const tableBody = document.querySelector('#payment-history-content tbody');
        if (tableBody) {
            tableBody.innerHTML = '<tr><td colspan="4" style="text-align: center; color: var(--danger-color);">Não foi possível carregar o histórico.</td></tr>';
        }
    }
}

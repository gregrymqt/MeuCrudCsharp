// /js/admin/modules/panels/subscriptionsPanel.js

import * as api from '../api/adminAPI.js';

const actionResultsContainer = document.getElementById('action-results-container');
const actionSelector = document.getElementById('subscription-action-selector');
const actionForms = document.querySelectorAll('.action-form');

function showResults(data, isSuccess = true) {
    actionResultsContainer.innerHTML = ''; // Limpa resultados anteriores

    const resultBox = document.createElement('div');
    resultBox.className = isSuccess ? 'result-box' : 'result-box result-box-error';

    const title = document.createElement('h4');
    title.textContent = isSuccess ? 'Resultado da Operação' : 'Falha na Operação';
    resultBox.appendChild(title);

    const content = document.createElement('pre');
    if (isSuccess) {
        content.textContent = JSON.stringify(data, null, 2);
    } else {
        // Se 'data' for um objeto de erro com 'message', use-o, senão, use uma string padrão.
        content.textContent = data?.message ?? 'Ocorreu um erro inesperado.';
    }
    resultBox.appendChild(content);

    actionResultsContainer.appendChild(resultBox);
}


async function handleFormSubmit(form, apiCall) {
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const submitButton = e.submitter || form.querySelector('button[type="submit"]');
        const originalButtonText = submitButton.textContent;

        // UX: Desabilita o botão e mostra feedback de carregamento
        submitButton.disabled = true;
        submitButton.textContent = 'Processando...';
        actionResultsContainer.innerHTML = ''; // Limpa resultados antigos

        try {
            // A função apiCall recebe o evento para poder extrair dados se necessário
            const result = await apiCall(e);
            showResults(result, true);
        } catch (error) {
            showResults(error, false);
        } finally {
            // UX: Reabilita o botão e restaura o texto original
            submitButton.disabled = false;
            submitButton.textContent = originalButtonText;
        }
    });
}

export function initializeSubscriptionsPanel() {
    if (!actionSelector) {
        console.error("Elementos essenciais do painel de assinaturas não foram encontrados.");
        return;
    }

    // Mostra/esconde o formulário correto quando o seletor muda
    actionSelector.addEventListener('change', function () {
        const selectedFormId = `form-${this.value}`;
        actionForms.forEach(form => {
            form.classList.toggle('active', form.id === selectedFormId);
        });
        actionResultsContainer.innerHTML = ''; // Limpa resultados ao trocar de ação
    });


    handleFormSubmit(document.getElementById('form-search'), (e) => {
        const query = e.target.querySelector('#search-id').value;
        return api.searchSubscription(query);
    });

    handleFormSubmit(document.getElementById('form-update-value'), (e) => {
        const id = e.target.querySelector('#update-value-id').value;
        const amount = parseFloat(e.target.querySelector('#update-value-amount').value);
        return api.updateSubscriptionValue(id, amount);
    });

    handleFormSubmit(document.getElementById('form-pause-cancel'), (e) => {
        const id = e.target.querySelector('#pause-cancel-id').value;
        const status = e.submitter.value;
        return api.updateSubscriptionStatus(id, status);
    });

    handleFormSubmit(document.getElementById('form-reactivate'), (e) => {
        const id = e.target.querySelector('#reactivate-id').value;
        return api.updateSubscriptionStatus(id, 'authorized');
    });
}
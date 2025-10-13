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
    if (!form || form.hasAttribute('data-submit-handler-attached')) {
        return;
    }

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

    form.setAttribute('data-submit-handler-attached', 'true');
}

function initializeActionSelector() {
    const actionSelector = document.getElementById('subscription-action-selector');
    const allForms = document.querySelectorAll('.action-form');
    const resultsContainer = document.getElementById('action-results-container');

    // VERIFICAÇÃO: Se o seletor não existe ou se o listener já foi anexado, não faz nada.
    if (!actionSelector || actionSelector.hasAttribute('data-change-handler-attached')) {
        return;
    }

    actionSelector.addEventListener('change', function() {
        const selectedValue = this.value;
        allForms.forEach(form => {
            form.classList.remove('active');
        });
        if (resultsContainer) {
            resultsContainer.innerHTML = '';
        }
        if (selectedValue) {
            const targetForm = document.getElementById(`form-${selectedValue}`);
            if (targetForm) {
                targetForm.classList.add('active');
            }
        }
    });

    // FLAG: Marca o seletor para indicar que o listener foi anexado.
    actionSelector.setAttribute('data-change-handler-attached', 'true');
}

export function initializeSubscriptionsPanel() {

    initializeActionSelector();

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

export function loadSubscriptions() {
    const panel = document.getElementById('content-subscriptions');
    if (!panel) {
        console.error('Painel de conteúdo de assinaturas #content-subscriptions não encontrado.');
        return;
    }

    fetch('/Profile/Admin?handler=SubscriptionsPartial')
        .then(response => {
            if (!response.ok) {
                throw new Error(`Erro na rede: ${response.statusText}`);
            }
            return response.text();
        })
        .then(html => {
            panel.innerHTML = html;
            initializeSubscriptionsPanel();
        })
        .catch(error => {
            console.error('Falha ao carregar o painel de assinaturas:', error);
            panel.innerHTML = '<p class="error-message">Não foi possível carregar o conteúdo das assinaturas. Tente recarregar a página.</p>';
        });
}
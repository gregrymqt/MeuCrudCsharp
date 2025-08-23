// /js/admin/modules/panels/subscriptionsPanel.js

import * as api from '../api/adminAPI.js';

const actionResultsDiv = document.getElementById('action-results');

function showResults(data, success = true) {
    const content = success ? JSON.stringify(data, null, 2) : `<p style="color:red;">${data.message || 'Ocorreu um erro.'}</p>`;
    actionResultsDiv.innerHTML = `<pre>${content}</pre>`;
}

async function handleFormSubmit(form, apiCall) {
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        try {
            const result = await apiCall(e);
            showResults(result);
        } catch (error) {
            showResults(error, false);
        }
    });
}

export function initializeSubscriptionsPanel() {
    const actionSelector = document.getElementById('subscription-action-selector');
    const actionForms = document.querySelectorAll('.action-form');

    actionSelector?.addEventListener('change', function () {
        actionForms.forEach(form => form.classList.toggle('active', form.id === `form-${this.value}`));
        actionResultsDiv.innerHTML = '';
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
/**
 * @file Manages the interactivity of the user's profile page.
 *
 * This script handles:
 * 1. Tab navigation (Subscription, Cards, etc.).
 * 2. Accordion logic to show/hide forms.
 * 3. Initialization and dynamic rendering of Mercado Pago Card Bricks.
 * 4. Communication with the backend API to:
 *    - Update a subscription's card.
 *    - Reactivate a subscription.
 *    - Request a refund.
 * 5. Displaying user feedback using the SweetAlert2 library.
 */
document.addEventListener('DOMContentLoaded', () => {
    // --- 1. GLOBAL INITIALIZATION AND CONFIGURATION ---

    /**
     * Mercado Pago public key for SDK initialization.
     * @type {string}
     * @constant
     * @warning **IMPORTANT:** This key must be replaced with your actual public key.
     */
    const publicKey = 'APP_USR-9237cffa-5ad4-4056-956b-20d62d1d0dab';
    if (!publicKey || publicKey === 'APP_USR-9237cffa-5ad4-4056-956b-20d62d1d0dab') {
        console.error('Mercado Pago public key is not configured!');
        return;
    }

    /**
     * Main instance of the Mercado Pago SDK.
     * @type {MercadoPago}
     */
    const mp = new MercadoPago(publicKey);
    /**
     * Mercado Pago Bricks builder, used to create UI instances.
     * @type {object}
     */
    const bricksBuilder = mp.bricks();

    /**
     * Flags to prevent unnecessary re-rendering of the card Bricks.
     * @type {boolean}
     */
    let primaryBrickRendered = false;
    let secondaryBrickRendered = false;

    // --- 2. TAB NAVIGATION LOGIC ---
    const navLinks = document.querySelectorAll('.profile-nav-link');
    const contentSections = document.querySelectorAll('.main-section');
    navLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const targetId = link.getAttribute('data-target');
            if (!targetId) return; // Ignore clicks on links without a target

            // Update the visual state of tabs and content sections
            navLinks.forEach(nav => nav.classList.remove('active'));
            link.classList.add('active');
            contentSections.forEach(section => section.classList.remove('active'));
            document.querySelector(targetId).classList.add('active');
        });
    });

    // --- 3. ACCORDION LOGIC WITH DYNAMIC RENDERING ---
    const accordionHeaders = document.querySelectorAll('.accordion-header');
    accordionHeaders.forEach(header => {
        header.addEventListener('click', () => {
            const body = header.nextElementSibling;
            const wasActive = header.classList.contains('active');

            // Close all accordions
            accordionHeaders.forEach(h => {
                h.classList.remove('active');
                h.nextElementSibling.classList.remove('active');
            });

            // If the clicked accordion was not active, open it.
            if (!wasActive) {
                header.classList.add('active');
                body.classList.add('active');

                // Check if this accordion should trigger the rendering of a Card Brick.
                // The 'data-brick-target' attribute in the HTML defines the Brick's container.
                const brickTargetId = header.getAttribute('data-brick-target');
                if (brickTargetId === 'primary-card-brick-container' && !primaryBrickRendered) {
                    createAndRenderCardBrick('primary-card-brick-container', handlePrimaryCardUpdate);
                    primaryBrickRendered = true;
                } else if (brickTargetId === 'secondary-card-brick-container' && !secondaryBrickRendered) {
                    createAndRenderCardBrick('secondary-card-brick-container', handleSecondaryCardUpdate);
                    secondaryBrickRendered = true;
                }
            }
        });
    });

    // --- 4. API INTERACTION LOGIC ---
    const profileContainer = document.querySelector('.profile-container');
    const SUBSCRIPTION_ID = profileContainer.dataset.subscriptionId;

    /**
     * Sends a request to the backend to update the card associated with a subscription.
     * Uses SweetAlert for visual feedback to the user.
     * @param {object} payload - The request body to be sent. Usually contains the new card token.
     * @returns {Promise<void>} A promise that resolves on success and rejects on error.
     */
    async function updateSubscriptionOnBackend(payload) {
        if (!SUBSCRIPTION_ID || SUBSCRIPTION_ID.includes('SEU_ID')) { // Security validation
            Swal.fire({
                icon: 'error',
                title: 'Configuration Error',
                text: 'The subscription ID is invalid. Please refresh the page and try again.'
            });
            return Promise.reject('Invalid subscription ID.');
        }

        const backendApiUrl = '/api/user/subscription/card';

        try {
            const response = await fetch(backendApiUrl, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ newCardToken: payload.token }),
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Failed to update the subscription.');
            }

            Swal.fire({
                icon: 'success',
                title: 'Success!',
                text: 'Your subscription has been updated successfully.',
                timer: 2000,
                showConfirmButton: false
            });

            // Optional: reload the page after a while to reflect the changes.
            // setTimeout(() => location.reload(), 2200);

        } catch (error) {
            Swal.fire({
                icon: 'error',
                title: 'Oops...',
                text: error.message
            });
            // Rejects the promise so the Brick knows there was an error.
            return Promise.reject(error);
        }
    }

    /**
     * Callback executed when the primary card form is submitted.
     * Prepares the payload and calls the backend update function.
     * @param {object} formData - Dados do formulário retornados pelo Brick do Mercado Pago.
     * @param {string} formData.token - O token do cartão gerado.
     * @returns {Promise<void>}
     */
    function handlePrimaryCardUpdate(formData) {
        console.log("Submitting for primary card change:", formData.token);
        const payload = { card_token_id: formData.token };
        return updateSubscriptionOnBackend(payload);
    }

    /**
     * Callback executed when the secondary card form is submitted.
     * Prepares the payload and calls the backend update function.
     * @param {object} formData - Dados do formulário retornados pelo Brick do Mercado Pago.
     * @param {string} formData.token - O token do cartão gerado.
     * @param {string} formData.payment_method_id - O ID do método de pagamento (ex: 'master', 'visa').
     * @returns {Promise<void>}
     */
    function handleSecondaryCardUpdate(formData) {
        console.log("Submitting for secondary card change:", formData.token);
        const payload = {
            card_token_id_secondary: formData.token,
            payment_method_id_secondary: formData.payment_method_id
        };
        return updateSubscriptionOnBackend(payload);
    }

    // --- 5. SUBSCRIPTION REACTIVATION FORM LOGIC ---
    document.getElementById('form-reactivate-subscription').addEventListener('submit', async (e) => {
        e.preventDefault();
        const submitButton = e.target.querySelector('button[type="submit"]');

        const result = await Swal.fire({
            title: 'Reactivate Subscription?',
            text: 'You are about to reactivate your subscription. Billing will resume in the next cycle.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#28a745',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Yes, reactivate!',
            cancelButtonText: 'Cancel'
        });

        if (!result.isConfirmed) {
            return;
        }

        // Visual processing feedback
        submitButton.disabled = true;
        submitButton.textContent = 'Reactivating...';

        try {
            const response = await fetch(`/api/user/subscription/reactivate`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' }
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Could not reactivate the subscription.');
            }

            await Swal.fire({
                icon: 'success',
                title: 'Reactivated!',
                text: 'Your subscription has been successfully reactivated.'
            });

            location.reload(); // Reloads the page to show the new subscription status

        } catch (error) {
            Swal.fire({
                icon: 'error',
                title: 'Error!',
                text: error.message
            });
        } finally {
            submitButton.disabled = false;
            submitButton.textContent = 'Reactivate Subscription';
        }
    });

    // --- 6. GENERIC FUNCTION TO CREATE AND RENDER BRICKS ---
    /**
     * Generic function to create and render a Mercado Pago 'cardPayment' Brick.
     * @param {string} containerId - The ID of the HTML element where the Brick will be rendered.
     * @param {function} onSubmitCallback - The function to be executed when the Brick form is submitted.
     * @returns {Promise<object>} A promise that resolves with the Brick controller after rendering.
     */
    async function createAndRenderCardBrick(containerId, onSubmitCallback) {
        const settings = {
            initialization: { amount: 1.00 }, // Symbolic value for card validation
            customization: {
                visual: {
                    style: {
                        theme: 'bootstrap', // or 'default', 'dark'
                    }
                }
            },
            callbacks: {
                onReady: () => console.log(`Brick in #${containerId} is ready.`),
                onError: (error) => console.error(`Error in Brick #${containerId}:`, error),
                onSubmit: (formData) => {
                    // The Brick's 'onSubmit' callback expects a Promise.
                    return new Promise((resolve, reject) => {
                        // Executes the specific callback function (e.g., handlePrimaryCardUpdate)
                        onSubmitCallback(formData)
                            .then(resolve)
                            .catch(reject);
                    });
                },
            },
        };
        // Clears the container before rendering to avoid duplicates
        document.getElementById(containerId).innerHTML = '';
        return await bricksBuilder.create('cardPayment', containerId, settings);
    }

    // --- 7. REFUND REQUEST LOGIC ---
    const refundForm = document.getElementById('form-request-refund');
    // If the refund form does not exist on the page, do nothing.
    if (!refundForm) {
        return;
    }

    const refundStep1 = document.getElementById('refund-step-1');
    const refundStep2 = document.getElementById('refund-step-2');
    const submitButton = refundForm.querySelector('button[type="submit"]');

    refundForm.addEventListener('submit', async function (event) {
        event.preventDefault();

        // Asks the user for confirmation with a warning about the consequences.
        const result = await Swal.fire({
            title: 'Are you sure?',
            text: "This action cannot be undone. Your access to the content will be revoked.",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Yes, request refund!',
            cancelButtonText: 'Cancel'
        });

        if (!result.isConfirmed) {
            return;
        }

        // Visual processing feedback
        submitButton.disabled = true;
        submitButton.textContent = 'Processing...';
        removeExistingErrors();

        try {
            const response = await fetch('/api/profile/request-refund', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' }
            });

            if (response.ok) {
                // SUCCESS: Toggles the visibility of the panels to show the confirmation message.
                refundStep1.style.display = 'none';
                refundStep2.style.display = 'block';
            } else {
                const errorData = await response.json();
                Swal.fire({
                    icon: 'error',
                    title: 'Could not process',
                    text: errorData.message || 'An error occurred while requesting the refund.'
                }); // The showRefundError function could also be used here.
            }

        } catch (error) {
            console.error('Network error when requesting refund:', error);
            Swal.fire({
                icon: 'error',
                title: 'Connection Error',
                text: 'Could not connect to the server. Check your internet connection and try again.'
            });
        } finally {
            // Restores the button in case of an error
            submitButton.disabled = false;
            submitButton.textContent = 'I Understand and Wish to Request a Refund';
        }
    });

    /**
     * @deprecated The showRefundError function has been replaced by SweetAlert for UI consistency.
     *             Kept for reference in case inline errors are decided upon in the future.
     * Displays an error message within the refund section.
     * @param {string} message The error message to be displayed.
     */
    function showRefundError(message) {
        removeExistingErrors();
        const errorDiv = document.createElement('div');
        errorDiv.className = 'refund-error-message';
        errorDiv.textContent = message;

        // Inserts the error message before the form
        refundForm.parentNode.insertBefore(errorDiv, refundForm);
    }

    /**
     * Removes existing refund error messages to prevent them from piling up.
     */
    function removeExistingErrors() {
        const existingError = document.querySelector('.refund-error-message');
        if (existingError) {
            existingError.remove();
        }
    }

});
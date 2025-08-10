/**
 * @file Manages the Mercado Pago payment flow using Bricks.
 * This script initializes and renders the Payment Brick, handles form submission,
 * processes the payment with the backend, and displays the final status.
 */

/**
 * Displays an error message in the UI and logs it to the console.
 * @param {string} message - The error message to be displayed.
 */
function showError(message) {
    const errorContainer = document.getElementById('error-container');
    const loadingMessage = document.getElementById('loading-message');
    const paymentContainer = document.getElementById('paymentBrick_container');
    if (errorContainer) {
        errorContainer.textContent = message;
        errorContainer.style.display = 'block';
    }
    // Hide the loading indicator
    if (loadingMessage) {
        loadingMessage.style.display = 'none';
    }
    // Show the payment form again so the user can retry.
    if (paymentContainer) {
        paymentContainer.style.display = 'block'; // Corrected from 'none' to allow retry
    }
    console.error(message);
}

/**
 * Entry point for initializing the Mercado Pago payment process.
 * It verifies that the SDK and necessary configurations are available before rendering the Payment Brick.
 */
function initializePayment() {
    // Check if the Mercado Pago SDK has loaded successfully.
    if (typeof MercadoPago === 'undefined') {
        showError('Mercado Pago SDK failed to load.');
        return;
    }
    // Check if the backend has provided the necessary configuration object.
    if (!window.paymentConfig || !window.paymentConfig.publicKey || !window.paymentConfig.preferenceId) {
        showError('Configuration error: Public key or preference ID not found.');
        return;
    }

    const mp = new MercadoPago(window.paymentConfig.publicKey, {
        locale: 'pt-BR'
    });
    const bricksBuilder = mp.bricks();

    renderPaymentBrick(bricksBuilder);
}

/**
 * Renders the Mercado Pago Payment Brick.
 * This function sets up the configuration and callbacks for handling form submission,
 * errors, and the ready state.
 * @param {object} builder - The Mercado Pago Bricks builder instance.
 */
function renderPaymentBrick(builder) {
    const settings = {
        initialization: {
            amount: window.paymentConfig.amount,
            preferenceId: window.paymentConfig.preferenceId
        },
        customization: {
            paymentMethods: {
                creditCard: "all", // Allows all credit cards
                ticket: "all",     // Allows all ticket payments (e.g., Boleto)
                pix: "all"         // Allows PIX
            }
        },
        callbacks: {
            onReady: function() {
                console.log("Payment Brick is ready.");
                // Hide the main loader once the brick is ready to be displayed
                const loadingMessage = document.getElementById('loading-message');
                if (loadingMessage) loadingMessage.style.display = 'none';
            },
            onSubmit: function(params) {
                // When the user submits the form, hide the payment brick and show a loading message.
                document.getElementById('paymentBrick_container').style.display = 'none';
                document.getElementById('loading-message').style.display = 'block';
                document.getElementById('error-container').style.display = 'none';

                // Send the payment data to the backend for processing.
                fetch(window.paymentConfig.processPaymentUrl, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(params.formData)
                })
                .then(function(response) {
                    if (!response.ok) {
                        // If the server returns an error, parse the JSON and throw an error to be caught below.
                        return response.json().then(function(err) {
                            throw new Error(err.message || "HTTP Error: " + response.status);
                        });
                    }
                    return response.json();
                })
                .then(function(responseData) {
                    // Validate the response from the server.
                    if (!responseData.id || !responseData.status) {
                        throw new Error(responseData.message || 'Invalid response from server.');
                    }
                    // On success, hide the loading message and show the container for the status screen.
                    document.getElementById('loading-message').style.display = 'none';
                    document.getElementById('statusScreenBrick_container').style.display = 'block';
                    // Render the status screen with the payment ID received from the backend.
                    renderStatusScreenBrick(builder, responseData.id);
                })
                .catch(function(error) {
                    // If any error occurs during the fetch process, display it.
                    showError('Error processing payment: ' + error.message);
                });
            },
            onError: function(error) {
                // This callback handles client-side validation errors from the Brick itself (e.g., invalid card number).
                showError('Please check the data you entered. ' + (error && error.message ? error.message : ''));
            }
        }
    };

    builder.create("payment", "paymentBrick_container", settings).then(function(controller) {
        window.paymentBrickController = controller;
    });
}

/**
 * Renders the Mercado Pago Status Screen Brick after a payment is processed.
 * @param {object} builder - The Mercado Pago Bricks builder instance.
 * @param {string} paymentId - The ID of the payment whose status will be displayed.
 */
function renderStatusScreenBrick(builder, paymentId) {
    const settings = {
        initialization: { paymentId: paymentId },
        callbacks: {
            onReady: function() { console.log('Status Screen Brick is ready.'); },
            onError: function(error) { showError('An error occurred while displaying the payment status: ' + error.message); }
        }
    };

    builder.create('statusScreen', 'statusScreenBrick_container', settings).then(function(controller) {
        window.statusScreenBrickController = controller;
    });
}

// Initializes the payment process as soon as the DOM content is fully loaded.
document.addEventListener('DOMContentLoaded', initializePayment);
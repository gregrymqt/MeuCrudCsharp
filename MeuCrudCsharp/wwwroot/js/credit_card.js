// ====================================================================================
// PASSO 1: DECLARAÇÕES GLOBAIS E INTERFACES
// ====================================================================================
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
// ====================================================================================
// PASSO 2: FUNÇÕES AUXILIARES
// ====================================================================================
const showError = (message) => {
    const errorContainer = document.getElementById('error-container');
    const loadingMessage = document.getElementById('loading-message');
    const paymentContainer = document.getElementById('paymentBrick_container');
    if (errorContainer) {
        errorContainer.textContent = message;
        errorContainer.style.display = 'block';
    }
    if (loadingMessage) {
        loadingMessage.style.display = 'none';
    }
    if (paymentContainer) {
        paymentContainer.style.display = 'block';
    }
    console.error(message);
};
// ====================================================================================
// PASSO 3: LÓGICA PRINCIPAL DOS BRICKS
// ====================================================================================
const initializePayment = () => {
    var _a, _b;
    if (typeof MercadoPago === 'undefined') {
        showError('O SDK do Mercado Pago não foi carregado.');
        return;
    }
    if (!((_a = window.paymentConfig) === null || _a === void 0 ? void 0 : _a.publicKey) || !((_b = window.paymentConfig) === null || _b === void 0 ? void 0 : _b.preferenceId)) {
        showError('Erro de configuração: Chave pública ou ID da preferência não encontrados.');
        return;
    }
    const mp = new MercadoPago(window.paymentConfig.publicKey, {
        locale: 'pt-BR'
    });
    const bricksBuilder = mp.bricks();
    renderPaymentBrick(bricksBuilder);
};
const renderPaymentBrick = (builder) => __awaiter(void 0, void 0, void 0, function* () {
    const settings = {
        initialization: {
            amount: window.paymentConfig.amount,
            preferenceId: window.paymentConfig.preferenceId,
        },
        customization: {
            paymentMethods: {
                creditCard: "all",
                ticket: "all",
                pix: "all",
            },
        },
        callbacks: {
            onReady: () => {
                console.log("Payment Brick está pronto.");
            },
            onSubmit: (params) => {
                document.getElementById('paymentBrick_container').style.display = 'none';
                document.getElementById('loading-message').style.display = 'block';
                document.getElementById('error-container').style.display = 'none';
                return new Promise((resolve, reject) => {
                    fetch(window.paymentConfig.processPaymentUrl, {
                        method: "POST",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify(params.formData),
                    })
                        .then(response => {
                        if (!response.ok) {
                            return response.json().then(err => {
                                throw new Error(err.message || `Erro HTTP: ${response.status}`);
                            });
                        }
                        return response.json();
                    })
                        .then(responseData => {
                        if (!responseData.id || !responseData.status) {
                            throw new Error(responseData.message || 'Resposta inválida do servidor.');
                        }
                        document.getElementById('loading-message').style.display = 'none';
                        document.getElementById('statusScreenBrick_container').style.display = 'block';
                        renderStatusScreenBrick(builder, responseData.id);
                        resolve();
                    })
                        .catch(error => {
                        showError(`Erro ao processar pagamento: ${error.message}`);
                        reject(error);
                    });
                });
            },
            onError: (error) => {
                showError('Verifique os dados informados. ' + ((error === null || error === void 0 ? void 0 : error.message) || ''));
            },
        },
    };
    window.paymentBrickController = yield builder.create("payment", "paymentBrick_container", settings);
});
const renderStatusScreenBrick = (builder, paymentId) => __awaiter(void 0, void 0, void 0, function* () {
    const settings = {
        initialization: { paymentId: paymentId },
        callbacks: {
            onReady: () => console.log('Status Screen Brick pronto.'),
            onError: (error) => showError('Ocorreu um erro ao exibir o status do pagamento: ' + error.message),
        },
    };
    window.statusScreenBrickController = yield builder.create('statusScreen', 'statusScreenBrick_container', settings);
});
// ====================================================================================
// PASSO 4: INICIALIZAÇÃO
// ====================================================================================
document.addEventListener('DOMContentLoaded', initializePayment);
export {};
//# sourceMappingURL=credit_card.js.map
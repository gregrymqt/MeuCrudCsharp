// ====================================================================================
// PASSO 1: DECLARAÇÕES GLOBAIS E INTERFACES
// ====================================================================================

// Informamos ao TypeScript sobre a existência da biblioteca do Mercado Pago.
declare const MercadoPago: any;

// Com o arquivo sendo um módulo (ver final do arquivo), este bloco agora é válido.
// Ele "ensina" ao TypeScript sobre as propriedades que adicionamos ao objeto 'window'.
declare global {
    interface Window {
        paymentConfig: {
            publicKey: string;
            preferenceId: string;
            amount: number;
            processPaymentUrl: string;
        };
        paymentBrickController: any;
        statusScreenBrickController: any;
    }
}

// Interfaces para dar "forma" aos nossos objetos, melhorando a segurança e o autocomplete.
interface OnSubmitParams {
    selectedPaymentMethod: string;
    formData: any;
}

interface PaymentServerResponse {
    id?: number;
    status?: string;
    message?: string;
}

// ====================================================================================
// PASSO 2: FUNÇÕES AUXILIARES
// ====================================================================================

const showError = (message: string): void => {
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

const initializePayment = (): void => {
    if (typeof MercadoPago === 'undefined') {
        showError('O SDK do Mercado Pago não foi carregado.');
        return;
    }
    if (!window.paymentConfig?.publicKey || !window.paymentConfig?.preferenceId) {
        showError('Erro de configuração: Chave pública ou ID da preferência não encontrados.');
        return;
    }

    const mp = new MercadoPago(window.paymentConfig.publicKey, {
        locale: 'pt-BR'
    });
    const bricksBuilder = mp.bricks();

    renderPaymentBrick(bricksBuilder);
};

const renderPaymentBrick = async (builder: any): Promise<void> => {
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
            onReady: (): void => {
                console.log("Payment Brick está pronto.");
            },
            onSubmit: (params: OnSubmitParams): Promise<void> => {
                document.getElementById('paymentBrick_container')!.style.display = 'none';
                document.getElementById('loading-message')!.style.display = 'block';
                document.getElementById('error-container')!.style.display = 'none';

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
                            return response.json() as Promise<PaymentServerResponse>;
                        })
                        .then(responseData => {
                            if (!responseData.id || !responseData.status) {
                                throw new Error(responseData.message || 'Resposta inválida do servidor.');
                            }
                            document.getElementById('loading-message')!.style.display = 'none';
                            document.getElementById('statusScreenBrick_container')!.style.display = 'block';
                            renderStatusScreenBrick(builder, responseData.id);
                            resolve();
                        })
                        .catch(error => {
                            showError(`Erro ao processar pagamento: ${error.message}`);
                            reject(error);
                        });
                });
            },
            onError: (error: any): void => {
                showError('Verifique os dados informados. ' + (error?.message || ''));
            },
        },
    };
    window.paymentBrickController = await builder.create("payment", "paymentBrick_container", settings);
};

const renderStatusScreenBrick = async (builder: any, paymentId: number): Promise<void> => {
    const settings = {
        initialization: { paymentId: paymentId },
        callbacks: {
            onReady: (): void => console.log('Status Screen Brick pronto.'),
            onError: (error: any): void => showError('Ocorreu um erro ao exibir o status do pagamento: ' + error.message),
        },
    };
    window.statusScreenBrickController = await builder.create('statusScreen', 'statusScreenBrick_container', settings);
};

// ====================================================================================
// PASSO 4: INICIALIZAÇÃO
// ====================================================================================

document.addEventListener('DOMContentLoaded', initializePayment);

// ====================================================================================
// A SOLUÇÃO: Transformamos este arquivo em um módulo para o TypeScript.
// Isso permite o uso de 'declare global' e resolve o erro TS2669.
// ====================================================================================
export { };

document.addEventListener('DOMContentLoaded', () => {
    // --- 1. INICIALIZAÇÃO DO SDK DO MERCADO PAGO ---
    // Substitua pela sua Chave Pública
    const publicKey = 'SUA_PUBLIC_KEY'; 
    if (!publicKey || publicKey === 'SUA_PUBLIC_KEY') {
        console.error('Chave pública do Mercado Pago não configurada!');
        return;
    }
    const mp = new MercadoPago(publicKey);
    const bricksBuilder = mp.bricks();

    // Flags para controlar se os Bricks já foram renderizados
    let primaryBrickRendered = false;
    let secondaryBrickRendered = false;
    
    // --- 2. LÓGICA DE NAVEGAÇÃO ENTRE ABAS (sem alterações) ---
    const navLinks = document.querySelectorAll('.profile-nav-link');
    const contentSections = document.querySelectorAll('.main-section');
    navLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const targetId = link.getAttribute('data-target');
            if (!targetId) return;
            navLinks.forEach(nav => nav.classList.remove('active'));
            link.classList.add('active');
            contentSections.forEach(section => section.classList.remove('active'));
            document.querySelector(targetId).classList.add('active');
        });
    });

    // --- 3. LÓGICA DO ACORDEÃO (com renderização dinâmica dos Bricks) ---
    const accordionHeaders = document.querySelectorAll('.accordion-header');
    accordionHeaders.forEach(header => {
        header.addEventListener('click', () => {
            const body = header.nextElementSibling;
            const wasActive = header.classList.contains('active');

            // Fecha todos os acordeões
            accordionHeaders.forEach(h => {
                h.classList.remove('active');
                h.nextElementSibling.classList.remove('active');
            });

            // Abre ou fecha o acordeão clicado
            if (!wasActive) {
                header.classList.add('active');
                body.classList.add('active');

                // Verifica se precisa renderizar um Brick
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
    
    // --- 4. FUNÇÕES DE LÓGICA DA API ---
    const profileContainer = document.querySelector('.profile-container');
    const SUBSCRIPTION_ID = profileContainer.dataset.subscriptionId;

    async function updateSubscriptionOnBackend(payload) {
        // Validação inicial
        if (!SUBSCRIPTION_ID || SUBSCRIPTION_ID.includes('SEU_ID')) {
            // --- MUDANÇA AQUI ---
            Swal.fire({
                icon: 'error',
                title: 'Erro de Configuração',
                text: 'O ID da assinatura é inválido. Por favor, atualize a página e tente novamente.'
            });
            return Promise.reject('ID da assinatura inválido.');
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
                throw new Error(errorData.message || 'Falha ao atualizar a assinatura.');
            }

            // --- MUDANÇA AQUI ---
            Swal.fire({
                icon: 'success',
                title: 'Sucesso!',
                text: 'Sua assinatura foi atualizada com sucesso.',
                timer: 2000,
                showConfirmButton: false
            });

            // Opcional: recarregar a página para ver as mudanças
            // setTimeout(() => location.reload(), 2000);

        } catch (error) {
            // --- MUDANÇA AQUI ---
            Swal.fire({
                icon: 'error',
                title: 'Oops...',
                text: error.message
            });
            // Rejeita a promessa para o Brick saber que houve erro.
            return Promise.reject(error);
        }
    }

    // Callbacks específicos para cada formulário
    function handlePrimaryCardUpdate(formData) {
        console.log("Enviando para troca de cartão primário:", formData.token);
        const payload = { card_token_id: formData.token };
        return updateSubscriptionOnBackend(payload);
    }

    function handleSecondaryCardUpdate(formData) {
        console.log("Enviando para troca de cartão secundário:", formData.token);
        const payload = { 
            card_token_id_secondary: formData.token,
            // O Brick já nos dá o ID do método de pagamento (ex: 'master', 'visa')
            payment_method_id_secondary: formData.payment_method_id 
        };
        return updateSubscriptionOnBackend(payload);
    }
    
    // Formulário de Reativação (permanece igual)
    document.getElementById('form-reactivate-subscription').addEventListener('submit', async (e) => {
        e.preventDefault();
        const submitButton = e.target.querySelector('button[type="submit"]');

        // --- MUDANÇA AQUI ---
        const result = await Swal.fire({
            title: 'Reativar Assinatura?',
            text: 'Você está prestes a reativar sua assinatura. A cobrança será retomada no próximo ciclo.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#28a745',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Sim, reativar!',
            cancelButtonText: 'Cancelar'
        });

        if (!result.isConfirmed) {
            return;
        }

        // Feedback visual
        submitButton.disabled = true;
        submitButton.textContent = 'Reativando...';

        try {
            const response = await fetch(`/api/user/subscription/reactivate`, {
                method: 'POST', // O endpoint espera POST
                headers: { 'Content-Type': 'application/json' }
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Não foi possível reativar a assinatura.');
            }

            await Swal.fire({
                icon: 'success',
                title: 'Reativada!',
                text: 'Sua assinatura foi reativada com sucesso.'
            });

            location.reload(); // Recarrega a página para mostrar o novo status

        } catch (error) {
            Swal.fire({
                icon: 'error',
                title: 'Erro!',
                text: error.message
            });
        } finally {
            submitButton.disabled = false;
            submitButton.textContent = 'Reativar Assinatura';
        }
    });


    // --- 5. FUNÇÃO GENÉRICA PARA CRIAR O BRICK ---
    async function createAndRenderCardBrick(containerId, onSubmitCallback) {
        const settings = {
            initialization: { amount: 1.00 }, // Valor simbólico
            customization: {
                visual: {
                    style: {
                        theme: 'bootstrap', // ou 'default', 'dark'
                    }
                }
            },
            callbacks: {
                onReady: () => console.log(`Brick em #${containerId} está pronto.`),
                onError: (error) => console.error(`Erro no Brick #${containerId}:`, error),
                onSubmit: (formData) => {
                    // Chama o callback específico passado como parâmetro
                    return new Promise((resolve, reject) => {
                        onSubmitCallback(formData)
                            .then(resolve)
                            .catch(reject);
                    });
                },
            },
        };
        // Limpa o contêiner antes de renderizar para evitar duplicatas
        document.getElementById(containerId).innerHTML = '';
        return await bricksBuilder.create('cardPayment', containerId, settings);
    }

    // =======================================================
    // LÓGICA DE REEMBOLSO
    // =======================================================

    const refundForm = document.getElementById('form-request-refund');

    // Se o formulário de reembolso não existir na página, não faz nada.
    if (!refundForm) {
        return;
    }

    const refundStep1 = document.getElementById('refund-step-1');
    const refundStep2 = document.getElementById('refund-step-2');
    const submitButton = refundForm.querySelector('button[type="submit"]');
    const originalButtonText = submitButton.textContent;

    // Adiciona o listener para o envio do formulário de reembolso
    refundForm.addEventListener('submit', async function (event) {
        event.preventDefault();
        const submitButton = refundForm.querySelector('button[type="submit"]');

        // --- MUDANÇA AQUI ---
        const result = await Swal.fire({
            title: 'Você tem certeza?',
            text: "Esta ação não pode ser desfeita. Seu acesso ao conteúdo será revogado.",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Sim, solicitar reembolso!',
            cancelButtonText: 'Cancelar'
        });

        if (!result.isConfirmed) {
            return;
        }

        // --- Feedback visual para o usuário ---
        submitButton.disabled = true;
        submitButton.textContent = 'Processando...';
        removeExistingErrors();

        try {
            // --- A requisição para o seu backend ---
            const response = await fetch('/api/profile/request-refund', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' }
            });

            // --- Tratamento da resposta do backend ---
            if (response.ok) {
                // SUCESSO: Esconde o passo 1 e mostra o passo 2
                refundStep1.style.display = 'none';
                refundStep2.style.display = 'block';
                // O passo 2 já contém a mensagem calmante, então não precisamos de outro Swal aqui.
            } else {
                const errorData = await response.json();
                // --- MUDANÇA AQUI ---
                Swal.fire({
                    icon: 'error',
                    title: 'Não foi possível processar',
                    text: errorData.message || 'Ocorreu um erro ao solicitar o reembolso.'
                });
                // Restaura o botão em caso de erro
                submitButton.disabled = false;
                submitButton.textContent = 'Entendo e Quero Solicitar o Reembolso';
            }

        } catch (error) {
            console.error('Erro de rede ao solicitar reembolso:', error);
            // --- MUDANÇA AQUI ---
            Swal.fire({
                icon: 'error',
                title: 'Erro de Conexão',
                text: 'Não foi possível conectar ao servidor. Verifique sua internet e tente novamente.'
            });
            // Restaura o botão em caso de erro
            submitButton.disabled = false;
            submitButton.textContent = 'Entendo e Quero Solicitar o Reembolso';
        }
    });

    /**
     * Exibe uma mensagem de erro dentro da seção de reembolso.
     * @param {string} message A mensagem de erro a ser exibida.
     */
    function showRefundError(message) {
        removeExistingErrors();
        const errorDiv = document.createElement('div');
        errorDiv.className = 'refund-error-message';
        errorDiv.textContent = message;

        // Insere a mensagem de erro antes do formulário
        refundForm.parentNode.insertBefore(errorDiv, refundForm);
    }

    /**
     * Remove mensagens de erro anteriores.
     */
    function removeExistingErrors() {
        const existingError = document.querySelector('.refund-error-message');
        if (existingError) {
            existingError.remove();
        }
    }

});
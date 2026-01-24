#!/bin/bash

# --- 1. VARIÁVEIS DE CAMINHO (Baseadas nas suas fotos) ---
# Usamos variáveis para não ter que digitar caminhos gigantes toda hora.
PROJETO_RAIZ="greg-company"
PASTA_BI="bi-dashboard"
PASTA_BACK="system-app/backend"
PASTA_FRONT="system-app/frontend"
ARQUIVO_LOG="./logs/sessao_sistema.log"

# --- 2. PREPARAÇÃO ---
mkdir -p "./logs"
echo "--- Sessão iniciada em: $(date) ---" > "$ARQUIVO_LOG"

# --- 3. VERIFICAÇÃO DE AMBIENTE (O seu FOR) ---
# Vamos conferir se o seu Linux Mint tem tudo pronto para o ecossistema.
echo "🔍 Verificando motores do sistema..."
REQUISITOS=("py" "dotnet" "npm")

for ITEM in "${REQUISITOS[@]}"; do
    # O comando 'command -v' é uma forma elegante de ver se um programa existe.
    command -v "$ITEM" > /dev/null 2>&1
    
    if [ $? -eq 0 ]; then
        echo "✅ $ITEM: Pronto para uso." | tee -a "$ARQUIVO_LOG"
    else
        echo "❌ $ITEM: Não encontrado. Verifique sua instalação." | tee -a "$ARQUIVO_LOG"
    fi
done

# --- 4. EXECUTANDO O SISTEMA BI ---
echo -e "\n🚀 Abrindo o Painel Administrativo da Greg Company...\n"

# Entramos na pasta que você mostrou na foto e rodamos o main.py
# O 'tee -a' permite que você veja o seu MENU e salve o que acontecer no LOG.
cd "$PASTA_BI" && py src/main.py | tee -a "../$ARQUIVO_LOG"

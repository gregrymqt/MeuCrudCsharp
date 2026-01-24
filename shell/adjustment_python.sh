#!/bin/bash

# --- 1. CONFIGURAÇÕES ---
BASE_PATH="bi-dashboard/src"
FOLDERS=("controllers" "services" "views" "data" "enums" "interfaces" "models")
# Usamos o caminho absoluto da pasta onde o script começou ($PWD)
LOG_PATH="$PWD/logs"
FILE_LOG="$LOG_PATH/ajuste_python.log"
ERRO_SCRIPT="src\."

# --- 2. GESTÃO DE DIRETÓRIOS ---
mkdir -p "$LOG_PATH"

# --- 3. LOOP DE LIMPEZA ---
cd "$BASE_PATH" || exit

for i in "${FOLDERS[@]}"; do
    echo "--- Relatório: Pasta $i ---" >> "$FILE_LOG"

    # Capturamos a lista de arquivos afetados em uma variável
    ARQUIVOS_AFETADOS=$(grep -rl "$ERRO_SCRIPT" "$i")

    # Usamos o 'wc -l' para contar quantos arquivos serão alterados
    TOTAL=$(echo "$ARQUIVOS_AFETADOS" | grep -c "$i")

    if [ "$TOTAL" -gt 0 ]; then
        echo "📄 Arquivos a serem corrigidos: $TOTAL" | tee -a "$FILE_LOG"
        echo "$ARQUIVOS_AFETADOS" >> "$FILE_LOG"

        # Executamos a correção
        find "$i" -name "*.py" -exec sed -i "s/$ERRO_SCRIPT//g" {} +
        echo "✅ Sucesso na pasta $i" | tee -a "$FILE_LOG"
    else
        echo "✔️  Pasta $i já estava limpa." | tee -a "$FILE_LOG"
    fi
done

echo "✨ Verifique o log em: $FILE_LOG"
#!/bin/bash

# --- 1. CONFIGURAÇÕES ---
# Ajuste o caminho para a pasta src do seu frontend
ROOT_DIR=$(cd "$(dirname "$0")/.." && pwd)
BASE_PATH="$ROOT_DIR/system-app/frontend/src"
LOG_PATH="$ROOT_DIR/logs"
FILE_LOG="$LOG_PATH/fix_sass.log"

mkdir -p "$LOG_PATH"
echo "--- Início da correção Sass: $(date) ---" > "$FILE_LOG"

# --- 2. FUNÇÃO DE CORREÇÃO ---
fix_scss_file() {
    local file=$1
    
    # 1. Verifica se o arquivo usa darken()
    if grep -q "darken(" "$file"; then
        echo "Updating: $file" | tee -a "$FILE_LOG"
        
        # Substitui darken(cor, X%) por color.adjust(cor, $lightness: -X%)
        # O regex captura o primeiro argumento e o número da porcentagem
        sed -i 's/darken(\([^,]*\),[[:space:]]*\([0-9]*\)%)/color.adjust(\1, $lightness: -\2%)/g' "$file"
        
        # 2. Verifica se o @use "sass:color" já existe, se não, adiciona no topo
        if ! grep -q '@use "sass:color"' "$file"; then
            # Adiciona o @use na primeira linha do arquivo
            sed -i '1i @use "sass:color";' "$file"
            echo "   + Added @use \"sass:color\"" >> "$FILE_LOG"
        fi
    fi
}

# --- 3. EXECUÇÃO ---
echo "🔍 Buscando arquivos .scss em $BASE_PATH..."

# Busca todos os arquivos .scss dentro da estrutura de features e componentes
find "$BASE_PATH" -name "*.scss" | while read -r scss_file; do
    fix_scss_file "$scss_file"
done

echo "✨ Processo concluído! Verifique o log em: $FILE_LOG"

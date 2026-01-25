import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  // 1. Carrega as variáveis de ambiente baseadas no modo (development, production, etc.)
  // O terceiro argumento '' diz para carregar TODAS as variáveis, não só as que começam com VITE_
  // (Mas por segurança no front, continue usando prefixo VITE_ nas que for usar no código)
  const env = loadEnv(mode, process.cwd(), '');

  return {
    plugins: [react()],

    // Configuração de alias para imports absolutos
    resolve: {
      alias: {
        'src': path.resolve(__dirname, './src')
      }
    },

    // Configuração do Servidor de Desenvolvimento
    server: {
      proxy: {
        '/api': {
          // Tenta pegar do .env, se não tiver, usa o hardcoded
          target: env.VITE_GENERAL_BASEURL || 'https://localhost:5045', 
          changeOrigin: true,
          secure: false,
        }
      }
    },

    // Configuração de Build (Produção)
    build: {
      // Aumenta o limite de aviso de tamanho de chunk (padrão é 500kb)
      // Como é um sistema corporativo, 1000kb ou 1500kb é aceitável
      chunkSizeWarningLimit: 1500,

      rollupOptions: {
        output: {
          // 2. Estratégia de Particionamento (Code Splitting)
          manualChunks(id) {
            // Separa bibliotecas de terceiros (node_modules) em um arquivo separado (vendor)
            if (id.includes('node_modules')) {
              
              // Opcional: Separar React e React-DOM em um chunk exclusivo (são muito usados)
              if (id.includes('react') || id.includes('react-dom') || id.includes('react-router-dom')) {
                return 'react-vendor';
              }

              // O resto das libs vai para o 'vendor'
              return 'vendor';
            }
          }
        }
      }
    },
    
    // (Opcional) Configuração para CSS/SCSS
    css: {
      preprocessorOptions: {
        scss: {
          // Injeta as variáveis e mixins em TODOS os arquivos .scss automaticamente
          additionalData: `@use "src/styles/_variables.scss" as *;`
        }
      }
    }
  };
});
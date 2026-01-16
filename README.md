# 💼 Greg Company - Integrated Business Suite

Este ecossistema integra uma plataforma de gestão de produtos (Full-stack) com um motor de inteligência de negócios (BI) automatizado. O projeto foi desenvolvido por Lucas Vicente De Souza, estudante de Desenvolvimento de Software Multiplataforma na FATEC.

<!-- Sugestão: Adicionar screenshots ou um GIF da aplicação em funcionamento torna o projeto muito mais atrativo. -->
<!-- 
## 📸 Screenshots

*(coloque aqui um screenshot da tela de produtos)*
*(coloque aqui um screenshot do dashboard de BI)*
-->

## 🚀 Tecnologias e Integrações

*   **Sistema Transacional (Backend):** ASP.NET 8 (C#) para APIs RESTful.
*   **Sistema Transacional (Frontend):** React com TypeScript.
*   **Banco de Dados:** SQL Server para persistência de dados.
*   **Cache:** Redis para caching de alta performance.
*   **Pagamentos:** Integração completa com MercadoPago (Checkout Pro, Webhooks, PIX e Assinaturas).
*   **Jobs em Background:** Hangfire para processamento de tarefas assíncronas (ex: renovação de assinaturas).
*   **Business Intelligence (BI):** Motor de ETL desenvolvido em Python.
*   **Visualização de Dados:** Integração com APIs da Rows e Notion para dashboards executivos.
*   **Containerização:** Docker e Docker Compose para orquestração do ambiente de desenvolvimento.

## 🏗️ Arquitetura

### Sistema Principal (C# & React)
A aplicação principal foca na escalabilidade, manutenibilidade e experiência do usuário:

*   **Backend (C#):** Implementa uma arquitetura limpa (Clean Architecture) com foco em APIs RESTful e princípios SOLID. Lida com regras de negócio complexas, autenticação, e integrações financeiras de forma segura.
*   **Frontend (React):** Estrutura baseada em componentes modulares e reutilizáveis, com gerenciamento de estado e hooks customizados para interagir com o backend.
*   **Infraestrutura:** Configuração de ambiente via Docker Compose para orquestração de containers (SQL Server, Redis), garantindo um setup de desenvolvimento rápido e consistente.

### Módulo de BI (Python)
O projeto de BI foi construído seguindo rigorosos padrões de Clean Code e Separação de Responsabilidades, garantindo que a lógica de dados seja independente da interface de saída.
---------------------------------------------------------

🐍 Arquitetura do BI-Dashboard (Python)
O projeto de BI foi construído seguindo rigorosos padrões de Clean Code e Separação de Responsabilidades, garantindo que a lógica de dados seja independente da interface de saída.

Estrutura de Pastas e Responsabilidades:
- controllers/: Gerencia o fluxo de execução, coordenando a captura de dados brutos da API e o acionamento dos serviços.

- services/: Camada onde reside a inteligência de negócio. Realiza o processo de ETL, limpando os dados e calculando métricas como Patrimônio Total e Alertas Críticos de Estoque.

- models/: Define as entidades de dados (ex: Product), garantindo tipagem e consistência durante o processamento.

- data/: Centraliza a comunicação com as APIs externas (Exporters), como a implementação do RowsExporter para envio de dados.

- views/: Responsável pela formatação da saída dos dados, seja para exibição no terminal ou estruturação de tabelas para o Rows e Notion.
----------------------------------------------------------

📊 Fluxo de Dados (ETL)
1. Extração: O script Python consome os dados brutos da plataforma Greg Company.

2. Transformação: O DataService processa os status (ex: Alertas Críticos) e agrega valores financeiros.

3. Carga: Os dados processados são enviados via API para o Rows e espelhados no Notion para visualização executiva.
----------------------------------------------------------
🛠️ Como Executar

### Pré-requisitos
*   [.NET 8 SDK](https://dotnet.microsoft.com/download)
*   [Node.js v20.x](https://nodejs.org/) (com npm ou yarn)
*   [Python 3.10+](https://www.python.org/downloads/)
*   [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### 1. Configuração do Ambiente
Clone o repositório e crie o arquivo de variáveis de ambiente.

```bash
git clone https://github.com/seu-usuario/greg-company-ecosystem.git
cd greg-company-ecosystem

# Crie um arquivo .env na raiz e adicione as chaves necessárias.
# Você pode usar o .env.example como base (recomendo criar um).
cp .env.example .env 
```

Preencha o `.env` com suas chaves de API (Rows, MercadoPago, etc.) e a string de conexão do banco.

### 2. Suba a Infraestrutura
Inicie os containers do SQL Server e Redis.

```bash
docker-compose up -d
```

### 3. Execute o Backend (API)
```bash
cd system-app/backend
dotnet run
```
A API estará disponível em `https://localhost:7035` (verifique o `launchSettings.json`). A documentação Swagger estará em `/swagger`.

### 4. Execute o Frontend (React App)
```bash
cd system-app/frontend
npm install
npm start
```
A aplicação estará rodando em `http://localhost:3000`.

### 5. Execute o Módulo de BI
```bash
cd ../../bi-dashboard # a partir da pasta frontend
pip install -r requirements.txt
python src/main.py
```
class RowsView:
    def run_rows_sync(self, api_controller, data_service, rows_exporter):
        """Orquestra o processo de envio de dados para o Dashboard do Rows/Notion."""
        print("\n" + "="*70)
        print(" SINCRONIZANDO DASHBOARD (ROWS.COM) ".center(70, " "))
        print("="*70)
        print("[PROCESSO] Extraindo dados da API...")

        # 1. Extração (Extract)
        # Pegamos 50 itens para que o gráfico no Notion tenha volume de dados
        raw_data = api_controller.get_products(limit=50, skip=0)

        if raw_data and "products" in raw_data:
            print("[PROCESSO] Aplicando regras de negócio e limpeza...")
            
            # 2. Transformação (Transform)
            clean_products = data_service.prepare_products(raw_data["products"])
            
            # 4. Sincronização (Sync)
            dashboard_metrics = data_service.get_dashboard_metrics(clean_products)

            print("[PROCESSO] Enviando para a nuvem do Rows.com...")
            
            # 3. Carga (Load)
            sucesso = rows_exporter.send_to_rows(clean_products, dashboard_metrics)

            if sucesso:
                print("\n✅ SUCESSO: O Dashboard no Notion já está atualizado!")
                print("Cores da Greg Company aplicadas com sucesso.")
            else:
                print("\n❌ ERRO: Falha na comunicação com a API do Rows.")
        else:
            print("\n❌ ERRO: Falha ao obter dados da API de produtos.")
            
        print("="*70 + "\n")
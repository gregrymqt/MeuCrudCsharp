class ExcelView:
    def run_export(self, api_controller, data_service, data_exporter):
        print("\n" + "="*70)
        print(" INICIANDO EXPORTAÇÃO PARA BI (EXCEL) ".center(70, " "))
        print("="*70)
        
        # 1. Busca os dados (Lógica da API agora na View)
        raw_data = api_controller.get_products(limit=50, skip=0)
        
        if raw_data and "products" in raw_data:
            # 2. Tratamento
            clean_products = data_service.prepare_products(raw_data["products"])
            
            # 3. Exportação
            sucesso = data_exporter.save_to_excel(clean_products, "relatorio_adm.xlsx")
            
            if sucesso:
                print(f"\n[OK] Arquivo salvo com sucesso!")
                print(f"Sua parceira já pode abrir o arquivo no notebook dela.")
            else:
                print("\n[ERRO] Falha ao gravar o arquivo. Verifique se o Excel está aberto.")
        else:
            print("\n[ERRO] Não foi possível conectar à API.")
        print("="*70 + "\n")
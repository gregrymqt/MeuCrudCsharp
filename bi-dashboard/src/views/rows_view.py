from datetime import datetime as dt
import time

class RowsView:
    def run_rows_sync(self, api_controller, data_service, rows_exporter):
        start_time = time.time()
        
        print("\n" + "═"*70)
        print(f" 📦 GREG COMPANY | AUTOMATION ENGINE v1.0 ".center(70, " "))
        print("═"*70)

        # 1. Extração
        print(f"[{dt.now().strftime('%H:%M:%S')}] 🔍 EXTRAÇÃO: Iniciando captura de produtos da API...")
        raw_data = api_controller.get_products(limit=50, skip=0)
        
        if raw_data and "products" in raw_data:
            # 2. Transformação
            print(f"[{dt.now().strftime('%H:%M:%S')}] ⚙️  PROCESSAMENTO: Aplicando regras de negócio e limpeza...")
            clean_products, stats = data_service.prepare_products(raw_data["products"])
            
            # Logs detalhados que ficam bem no print
            print(f"    ├─ Total processado: {stats['total']} itens")
            print(f"    ├─ Status OK: {stats['ok']} ✅")
            print(f"    ├─ Alertas Críticos: {stats['critico']} ⚠️")
            print(f"    |─ Esgotados: {stats['esgotado']} 🔴")
            print(f"    └─ A Repor: {stats['repor']} 🟡")

            # 3. Sincronização e Carga
            dashboard_metrics = data_service.get_dashboard_metrics(clean_products)
            print(f"[{dt.now().strftime('%H:%M:%S')}] ☁️  UPLOAD: Sincronizando com a nuvem do Rows.com...")
            
            sucesso = rows_exporter.send_to_rows(clean_products, dashboard_metrics)

            duration = round(time.time() - start_time, 2)
            if sucesso:
                print("\n" + "─"*70)
                print(f" ✨ SUCESSO: Dashboard Notion atualizado em {duration}s!")
                print(" Status: Operacional | Canal: Notion API")
                print("─"*70 + "\n")
            else:
                print(f"\n❌ ERRO: Falha crítica na comunicação após {duration}s.")
        else:
            print("\n❌ ERRO: API de origem não respondeu.")
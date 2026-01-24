from datetime import datetime as dt
import time
from interfaces.Iproduct_source import IProductSource
from interfaces.Idata_service import IDataService
from interfaces.Iproduct_exporter import IProductExporter

class RowsView:
    def __init__(self, source: IProductSource, service: IDataService, exporter: IProductExporter):
        # Injeção de Dependência via Construtor
        self.source = source
        self.service = service
        self.exporter = exporter

    def run_rows_sync(self):
        start_time = time.time()
        print("\n" + "="*70)
        print(f" GREG COMPANY | AUTOMATION ENGINE v1.1 ".center(70, " "))
        print("="*70)

        try:
            # 1. ETAPA: EXTRAÇÃO
            print(f"[{dt.now().strftime('%H:%M:%S')}] [EXTRACAO] Capturando dados...")
            raw_products = self.source.fetch_products(limit=50, skip=0)
            
            if not raw_products:
                raise ValueError("A fonte de dados retornou uma lista vazia.")

            # 2. ETAPA: TRANSFORMAÇÃO (Com try/except específico)
            try:
                print(f"[{dt.now().strftime('%H:%M:%S')}] [PROCESSAMENTO] Aplicando regras...")
                clean_products, stats = self.service.prepare_products(raw_products)
                
                print(f"    +- Total: {stats['total']} | OK: {stats.get('🟢 OK', 0)}")
                print(f"    +- Criticos: {stats.get('⚠️ CRÍTICO', 0)} | Esgotados: {stats.get('🔴 ESGOTADO', 0)}")

                metrics = self.service.get_dashboard_metrics(clean_products)
            except Exception as e:
                print(f"[ERRO] TRANSFORMACAO: Falha ao processar tipos ou calculos. Detalhes: {e}")
                return

            # 3. ETAPA: CARGA (LOAD)
            print(f"[{dt.now().strftime('%H:%M:%S')}] [UPLOAD] Sincronizando com Rows.com...")
            sucesso = self.exporter.send_to_rows(clean_products, metrics)

            if sucesso:
                duration = round(time.time() - start_time, 2)
                print("\n" + "-"*70)
                print(f" [SUCESSO] Automacao concluida em {duration}s!")
                print("-"*70 + "\n")
            else:
                print("\n[AVISO] O pipeline terminou, mas a API de destino reportou falha.")

        except ConnectionError as ce:
            print(f"\n[ERRO] REDE: Nao foi possivel alcançar o servidor. {ce}")
        except ValueError as ve:
            print(f"\n[ERRO] VALIDACAO: {ve}")
        except Exception as e:
            print(f"\n[ERRO CRITICO] NAO ESPERADO: {type(e).__name__} - {e}")
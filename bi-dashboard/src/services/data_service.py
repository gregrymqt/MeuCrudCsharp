from typing import List, Tuple
import numpy as np
from models.product_dto import ProductDTO
from models.cleaned_product_dto import CleanedProductDTO
from interfaces.Idata_service import IDataService
from enums.product_status import ProductStatus

class DataService(IDataService):
    def prepare_products(self, raw_products: List[ProductDTO]) -> Tuple[List[CleanedProductDTO], dict]:
        cleaned_data = []
        # Usar os valores do enum (strings) como chaves, não as instâncias
        stats = {status.value: 0 for status in ProductStatus}
        stats["total"] = 0

        for item in raw_products:
            match item.stock:
                case 0:
                    current_status = ProductStatus.ESGOTADO
                case s if s < 10:
                    current_status = ProductStatus.CRITICO
                case s if s < 20:
                    current_status = ProductStatus.REPOR
                case _:
                    current_status = ProductStatus.OK
            
            stats[current_status.value] += 1
            stats["total"] += 1
            
            cleaned_data.append(CleanedProductDTO(
                id=item.id,
                full_title=item.title or "Sem Nome",
                display_title=(item.title[:20] + "...") if item.title and len(item.title) > 20 else (item.title or "Sem Nome"),
                category=item.category.capitalize() if item.category else "Geral",
                brand=item.brand or "S/ Marca",
                price=item.price,
                stock=item.stock,
                status=current_status.value.capitalize(),
                total_stock_value=item.price * item.stock
            ))
            
        return cleaned_data, stats

    def get_dashboard_metrics(self, cleaned_products: List[CleanedProductDTO]) -> dict:
        precos = np.array([p.price for p in cleaned_products])
        estoques = np.array([p.stock for p in cleaned_products])
        status_array = np.array([p.status for p in cleaned_products])

        return {
            "total_value": float(np.sum(precos * estoques)),
            "critical_alerts": int(np.sum(np.char.find(status_array, "⚠️") != -1)),
            "unique_categories": len(np.unique([p.category for p in cleaned_products]))
        }

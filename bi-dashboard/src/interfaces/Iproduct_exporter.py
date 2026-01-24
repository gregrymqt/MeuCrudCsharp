from abc import ABC, abstractmethod
from typing import List, Dict
from models.cleaned_product_dto import CleanedProductDTO

class IProductExporter(ABC):
    @abstractmethod
    def send_to_rows(self, products: List[CleanedProductDTO], metrics: Dict[str, float]) -> bool:
        """Contrato obrigatório para envio de dados ao dashboard"""
        pass
from abc import ABC, abstractmethod
from typing import List
from models.cleaned_product_dto import CleanedProductDTO

class IExcelExporter(ABC):
    @abstractmethod
    def send_to_excel(self, products: List[CleanedProductDTO], filename: str) -> bool:
        """Contrato para exportação de dados para Excel"""
        pass
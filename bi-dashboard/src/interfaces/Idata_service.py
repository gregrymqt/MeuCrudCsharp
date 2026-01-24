from abc import ABC, abstractmethod
from typing import List, Tuple, Dict
from models.product_dto import ProductDTO
from models.cleaned_product_dto import CleanedProductDTO

class IDataService(ABC):
    @abstractmethod
    def prepare_products(self, raw_products: List[ProductDTO]) -> Tuple[List[CleanedProductDTO], Dict[str, int]]:
        """Aplica regras de negócio e retorna produtos limpos e estatísticas."""
        pass

    @abstractmethod
    def get_dashboard_metrics(self, cleaned_products: List[CleanedProductDTO]) -> Dict[str, float]:
        """Calcula métricas agregadas para o dashboard utilizando NumPy."""
        pass
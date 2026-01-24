from abc import ABC, abstractmethod
from typing import List
from models.product_dto import ProductDTO


class IProductSource(ABC):
    @abstractmethod
    def fetch_products(self, limit: int, skip: int) -> List[ProductDTO]:
        pass
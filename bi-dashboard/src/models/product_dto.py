from typing import Optional
from pydantic import BaseModel

class ProductDTO(BaseModel):
    id: int
    title: str
    brand: Optional[str] = "S/ Marca"
    category: str
    price: float
    stock: int

    @property
    def total_stock_value(self) -> float:
        return self.price * self.stock
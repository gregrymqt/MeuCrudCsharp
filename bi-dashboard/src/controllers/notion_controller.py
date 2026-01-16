import os
import requests
from dotenv import load_dotenv
from datetime import datetime as dt

load_dotenv()

class NotionController:
    def __init__(self):
        self.token = os.getenv("NOTION_TOKEN")
        self.block_id = os.getenv("NOTION_BLOCK_ID")
        self.headers = {
            "Authorization": f"Bearer {self.token}",
            "Notion-Version": "2022-06-28",
            "Content-Type": "application/json"
        }

    def update_status(self, mensagem, is_ok=True):
        """Atualiza o bloco de status no dashboard da Greg Company"""
        url = f"https://api.notion.com/v1/blocks/{self.block_id}"
        emoji = "🟢" if is_ok else "🔴"
        horario = dt.now().strftime("%H:%M:%S")
        
        payload = {
            "paragraph": {
                "rich_text": [{ "text": { "content": f"Status: {emoji} {mensagem} ({horario})" } }]
            }
        }
        
        try:
            response = requests.patch(url, headers=self.headers, json=payload)
            if response.status_code != 200:
                # Isso vai mostrar exatamente o que o Notion está reclamando
                print(f"Erro do Notion ({response.status_code}): {response.json()}")
            return response.status_code == 200
        except Exception as e:
            print(f"Erro de conexão: {e}")
            return False
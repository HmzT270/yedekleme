"""Fix event dates - move past events to future"""
import os
from dotenv import load_dotenv
from models.db_connector import DatabaseConnector
import json
from datetime import datetime, timedelta

load_dotenv()

with open('config.json', 'r', encoding='utf-8') as f:
    config = json.load(f)

db = DatabaseConnector(os.getenv('DB_CONNECTION_STRING'), config['database'])

from sqlalchemy import text

# Move all past events to future
future_date = datetime.now() + timedelta(days=3)  # 3 gün sonra

with db.engine.connect() as conn:
    # Update past events
    result = conn.execute(text("""
        UPDATE Events
        SET StartAt = :new_date
        WHERE StartAt < GETDATE()
        AND IsCancelled = 0
    """), {"new_date": future_date})
    
    conn.commit()
    
    print(f"✅ {result.rowcount} geçmiş etkinlik 3 gün sonraya taşındı!")
    print(f"Yeni tarih: {future_date}")

db.close()

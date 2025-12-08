"""Quick debug script to check club events"""
import os
from dotenv import load_dotenv
from models.db_connector import DatabaseConnector
import json

load_dotenv()

# Load config
with open('config.json', 'r', encoding='utf-8') as f:
    config = json.load(f)

# Connect to DB
db = DatabaseConnector(os.getenv('DB_CONNECTION_STRING'), config['database'])

# Check clubs 6 and 9
print("\n=== KULÜP 6 VE 9'UN ETKİNLİKLERİ ===\n")

from sqlalchemy import text

with db.engine.connect() as conn:
    # Get club names
    result = conn.execute(text("SELECT ClubId, Name FROM Clubs WHERE ClubId IN (6, 9)"))
    clubs = {row[0]: row[1] for row in result}
    
    print("Kulüpler:")
    for cid, name in clubs.items():
        print(f"  ClubId {cid}: {name}")
    
    print("\nEtkinlikler:")
    # Get events for these clubs
    result = conn.execute(text("""
        SELECT ClubId, EventId, Title, StartAt, IsCancelled, IsPublic
        FROM Events 
        WHERE ClubId IN (6, 9)
        ORDER BY ClubId, StartAt
    """))
    
    for row in result:
        club_id, event_id, title, start_at, cancelled, public = row
        status = "❌ İptal" if cancelled else ("🔒 Özel" if not public else "✅ Aktif")
        print(f"  {status} - Kulüp {club_id} ({clubs.get(club_id, 'Unknown')})")
        print(f"      EventId: {event_id}")
        print(f"      Başlık: {title}")
        print(f"      Tarih: {start_at}")
        print()

print("\n=== TÜM AKTİF ETKİNLİKLER ===\n")
with db.engine.connect() as conn:
    result = conn.execute(text("""
        SELECT e.EventId, e.Title, e.ClubId, c.Name as ClubName, e.StartAt
        FROM Events e
        JOIN Clubs c ON e.ClubId = c.ClubId
        WHERE e.IsCancelled = 0 AND e.IsPublic = 1
        ORDER BY e.StartAt
    """))
    
    for row in result:
        event_id, title, club_id, club_name, start_at = row
        marker = "⭐" if club_id in [6, 9] else "  "
        print(f"{marker} EventId {event_id}: {title}")
        print(f"   Kulüp: {club_name} (ID: {club_id})")
        print(f"   Tarih: {start_at}")
        print()

db.close()

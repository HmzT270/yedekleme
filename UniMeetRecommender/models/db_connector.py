"""
Database connector for UniMeet Recommender Service
Handles SQL Server connections and data extraction
"""
import os
import urllib
from typing import List, Dict, Optional, Tuple
from datetime import datetime, timedelta
import pandas as pd
from sqlalchemy import create_engine, text
from sqlalchemy.engine import Engine
from sqlalchemy.pool import QueuePool
from utils.logger import logger


class DatabaseConnector:
    """SQL Server database connector with connection pooling"""
    
    def __init__(self, connection_string: str, config: dict):
        """
        Initialize database connector
        
        Args:
            connection_string: SQL Server connection string
            config: Database configuration from config.json
        """
        self.config = config
        self.engine = self._create_engine(connection_string)
        logger.info("Database connector initialized", 
                   pool_size=config.get('pool_size', 5))
    
    def _create_engine(self, connection_string: str) -> Engine:
        """Create SQLAlchemy engine with connection pooling"""
        # Parse connection string for pyodbc
        params = urllib.parse.quote_plus(connection_string)
        db_url = f"mssql+pyodbc:///?odbc_connect={params}"
        
        engine = create_engine(
            db_url,
            poolclass=QueuePool,
            pool_size=self.config.get('pool_size', 5),
            pool_recycle=self.config.get('pool_recycle', 3600),
            echo=self.config.get('echo_sql', False),
            connect_args={'timeout': self.config.get('connection_timeout', 30)}
        )
        
        return engine
    
    def test_connection(self) -> bool:
        """Test database connection"""
        try:
            with self.engine.connect() as conn:
                conn.execute(text("SELECT 1"))
            logger.info("Database connection test successful")
            return True
        except Exception as e:
            logger.error(f"Database connection test failed: {str(e)}", exc_info=True)
            return False
    
    def get_user_followed_clubs(self, user_id: int) -> List[int]:
        """
        Get list of club IDs that a user follows
        
        Args:
            user_id: User ID
            
        Returns:
            List of club IDs
        """
        query = text("""
            SELECT ClubId 
            FROM ClubMembers 
            WHERE UserId = :user_id
        """)
        
        try:
            with self.engine.connect() as conn:
                result = conn.execute(query, {"user_id": user_id})
                club_ids = [row[0] for row in result]
            
            logger.debug(f"User {user_id} follows {len(club_ids)} clubs")
            return club_ids
        except Exception as e:
            logger.error(f"Error fetching followed clubs for user {user_id}: {str(e)}")
            return []
    
    def get_club_details(self, club_ids: Optional[List[int]] = None) -> pd.DataFrame:
        """
        Get club details including name, description, purpose
        
        Args:
            club_ids: Optional list of specific club IDs. If None, gets all clubs.
            
        Returns:
            DataFrame with club details
        """
        query = """
            SELECT 
                ClubId,
                Name,
                Description,
                Purpose,
                FoundedDate,
                ManagerId
            FROM Clubs
        """
        
        params = {}
        if club_ids:
            placeholders = ','.join([f':id{i}' for i in range(len(club_ids))])
            query += f" WHERE ClubId IN ({placeholders})"
            params = {f'id{i}': club_id for i, club_id in enumerate(club_ids)}
        
        try:
            with self.engine.connect() as conn:
                df = pd.read_sql(text(query), conn, params=params)
            
            # Fill NaN values
            df['Description'] = df['Description'].fillna('')
            df['Purpose'] = df['Purpose'].fillna('')
            
            logger.debug(f"Fetched {len(df)} club details")
            return df
        except Exception as e:
            logger.error(f"Error fetching club details: {str(e)}")
            return pd.DataFrame()
    
    def get_all_events(self, filters: Optional[Dict] = None) -> pd.DataFrame:
        """
        Get all events with optional filters
        
        Args:
            filters: Optional dict with keys like 'min_date', 'max_date', 'exclude_event_ids'
            
        Returns:
            DataFrame with event details
        """
        query = """
            SELECT 
                e.EventId,
                e.Title,
                e.Description,
                e.Location,
                e.StartAt,
                e.EndAt,
                e.Quota,
                e.ClubId,
                e.IsCancelled,
                e.IsPublic,
                e.CreatedByUserId,
                e.CreatedAt,
                c.Name as ClubName
            FROM Events e
            LEFT JOIN Clubs c ON e.ClubId = c.ClubId
            WHERE e.IsCancelled = 0 AND e.IsPublic = 1
        """
        
        params = {}
        
        if filters:
            if 'min_date' in filters and filters['min_date']:
                query += " AND e.StartAt >= :min_date"
                params['min_date'] = filters['min_date']
            
            if 'max_date' in filters and filters['max_date']:
                query += " AND e.StartAt <= :max_date"
                params['max_date'] = filters['max_date']
            
            if 'exclude_event_ids' in filters and filters['exclude_event_ids']:
                placeholders = ','.join([f':excl{i}' for i in range(len(filters['exclude_event_ids']))])
                query += f" AND e.EventId NOT IN ({placeholders})"
                params.update({f'excl{i}': eid for i, eid in enumerate(filters['exclude_event_ids'])})
        
        query += " ORDER BY e.StartAt"
        
        try:
            with self.engine.connect() as conn:
                df = pd.read_sql(text(query), conn, params=params)
            
            # Fill NaN values
            df['Description'] = df['Description'].fillna('')
            df['EndAt'] = df['EndAt'].fillna(df['StartAt'])
            
            logger.debug(f"Fetched {len(df)} events", filters=filters or {})
            return df
        except Exception as e:
            logger.error(f"Error fetching events: {str(e)}")
            return pd.DataFrame()
    
    def get_user_event_history(self, user_id: int, days_back: int = 365) -> pd.DataFrame:
        """
        Get user's event attendance and favorite history
        
        Args:
            user_id: User ID
            days_back: How many days back to look
            
        Returns:
            DataFrame with user's event interaction history
        """
        cutoff_date = datetime.utcnow() - timedelta(days=days_back)
        
        query = text("""
            SELECT DISTINCT
                e.EventId,
                e.ClubId,
                e.StartAt,
                CASE WHEN ea.UserId IS NOT NULL THEN 1 ELSE 0 END as Attended,
                CASE WHEN fe.UserId IS NOT NULL THEN 1 ELSE 0 END as Favorited,
                ea.CreatedAt as AttendedAt,
                fe.CreatedAt as FavoritedAt
            FROM Events e
            LEFT JOIN EventAttendees ea ON e.EventId = ea.EventId AND ea.UserId = :user_id
            LEFT JOIN FavoriteEvents fe ON e.EventId = fe.EventId AND fe.UserId = :user_id
            WHERE (ea.UserId IS NOT NULL OR fe.UserId IS NOT NULL)
              AND e.StartAt >= :cutoff_date
            ORDER BY e.StartAt DESC
        """)
        
        try:
            with self.engine.connect() as conn:
                df = pd.read_sql(query, conn, params={"user_id": user_id, "cutoff_date": cutoff_date})
            
            logger.debug(f"Fetched {len(df)} event interactions for user {user_id}")
            return df
        except Exception as e:
            logger.error(f"Error fetching event history for user {user_id}: {str(e)}")
            return pd.DataFrame()
    
    def get_user_favorites(self, user_id: int) -> List[int]:
        """
        Get list of event IDs that user has favorited
        
        Args:
            user_id: User ID
            
        Returns:
            List of event IDs
        """
        query = text("""
            SELECT EventId 
            FROM FavoriteEvents 
            WHERE UserId = :user_id
        """)
        
        try:
            with self.engine.connect() as conn:
                result = conn.execute(query, {"user_id": user_id})
                event_ids = [row[0] for row in result]
            
            logger.debug(f"User {user_id} has {len(event_ids)} favorited events")
            return event_ids
        except Exception as e:
            logger.error(f"Error fetching favorites for user {user_id}: {str(e)}")
            return []
    
    def get_club_member_counts(self) -> Dict[int, int]:
        """
        Get member count for each club
        
        Returns:
            Dict mapping ClubId to member count
        """
        query = text("""
            SELECT ClubId, COUNT(*) as MemberCount
            FROM ClubMembers
            GROUP BY ClubId
        """)
        
        try:
            with self.engine.connect() as conn:
                result = conn.execute(query)
                counts = {row[0]: row[1] for row in result}
            
            logger.debug(f"Fetched member counts for {len(counts)} clubs")
            return counts
        except Exception as e:
            logger.error(f"Error fetching club member counts: {str(e)}")
            return {}
    
    def get_club_event_counts(self, days_back: int = 30) -> Dict[int, int]:
        """
        Get recent event count for each club
        
        Args:
            days_back: How many days back to count
            
        Returns:
            Dict mapping ClubId to event count
        """
        cutoff_date = datetime.utcnow() - timedelta(days=days_back)
        
        query = text("""
            SELECT ClubId, COUNT(*) as EventCount
            FROM Events
            WHERE StartAt >= :cutoff_date
              AND IsCancelled = 0
            GROUP BY ClubId
        """)
        
        try:
            with self.engine.connect() as conn:
                result = conn.execute(query, {"cutoff_date": cutoff_date})
                counts = {row[0]: row[1] for row in result}
            
            logger.debug(f"Fetched event counts for {len(counts)} clubs (last {days_back} days)")
            return counts
        except Exception as e:
            logger.error(f"Error fetching club event counts: {str(e)}")
            return {}
    
    def close(self):
        """Close database connections"""
        if self.engine:
            self.engine.dispose()
            logger.info("Database connections closed")

"""
Feature engineering module for UniMeet Recommender Service
Extracts and computes features for recommendation scoring
"""
import re
from typing import List, Dict, Tuple, Optional
from datetime import datetime, timedelta, timezone
import numpy as np
import pandas as pd
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity
from utils.logger import logger


class FeatureEngine:
    """Feature extraction and engineering for recommendations"""
    
    def __init__(self, config: dict):
        """
        Initialize feature engine
        
        Args:
            config: Configuration dictionary from config.json
        """
        self.config = config
        self.content_config = config.get('content_settings', {})
        self.temporal_config = config.get('temporal_settings', {})
        
        # Initialize TF-IDF vectorizer
        self.vectorizer = self._create_vectorizer()
        self.club_vectors = None
        self.club_ids = None
        
        logger.info("Feature engine initialized")
    
    def _create_vectorizer(self) -> TfidfVectorizer:
        """Create TF-IDF vectorizer with Turkish stopwords"""
        stopwords = None
        if self.content_config.get('use_turkish_stopwords', True):
            stopwords = self.content_config.get('turkish_stopwords', [])
        
        vectorizer = TfidfVectorizer(
            max_features=self.content_config.get('tfidf_max_features', 200),
            stop_words=stopwords,
            ngram_range=(1, 2),  # Unigrams and bigrams
            min_df=1,
            lowercase=True,
            strip_accents='unicode'
        )
        
        return vectorizer
    
    def _preprocess_text(self, text: str) -> str:
        """Preprocess text for TF-IDF"""
        if pd.isna(text) or not text:
            return ""
        
        # Convert to lowercase
        text = str(text).lower()
        
        # Remove extra whitespace
        text = re.sub(r'\s+', ' ', text).strip()
        
        return text
    
    def fit_club_vectors(self, clubs_df: pd.DataFrame):
        """
        Fit TF-IDF vectorizer on club data and store vectors
        
        Args:
            clubs_df: DataFrame with club details (Name, Description, Purpose)
        """
        if clubs_df.empty:
            logger.warning("Empty clubs DataFrame provided to fit_club_vectors")
            return
        
        # Combine text fields - Name gets more weight (repeated 2x)
        clubs_df = clubs_df.copy()
        clubs_df['combined_text'] = (
            clubs_df['Name'].fillna('') + ' ' +
            clubs_df['Name'].fillna('') + ' ' +
            clubs_df['Description'].fillna('') + ' ' +
            clubs_df['Purpose'].fillna('')
        ).apply(self._preprocess_text)
        
        # Fit and transform
        try:
            self.club_vectors = self.vectorizer.fit_transform(clubs_df['combined_text'])
            self.club_ids = clubs_df['ClubId'].tolist()
            
            logger.info(f"Fitted TF-IDF vectors for {len(self.club_ids)} clubs",
                       vocab_size=len(self.vectorizer.vocabulary_))
        except Exception as e:
            logger.error(f"Error fitting club vectors: {str(e)}", exc_info=True)
            self.club_vectors = None
            self.club_ids = None
    
    def calculate_content_similarity(self, 
                                     user_club_ids: List[int],
                                     events_df: pd.DataFrame,
                                     clubs_df: pd.DataFrame) -> pd.DataFrame:
        """
        Calculate content similarity between user's clubs and events
        Now analyzes: club content + event title + event description
        
        Args:
            user_club_ids: List of club IDs the user follows
            events_df: DataFrame with events (Title, Description, Location, ClubId)
            clubs_df: DataFrame with all clubs
            
        Returns:
            DataFrame with EventId, content_similarity, and title_match_score columns
        """
        if events_df.empty or not user_club_ids:
            return pd.DataFrame({'EventId': [], 'content_similarity': [], 'title_match_score': []})
        
        # Ensure club vectors are fitted
        if self.club_vectors is None or self.club_ids is None:
            self.fit_club_vectors(clubs_df)
        
        if self.club_vectors is None:
            logger.warning("Club vectors not available, returning zero similarity")
            return pd.DataFrame({
                'EventId': events_df['EventId'],
                'content_similarity': 0.0,
                'title_match_score': 0.0
            })
        
        # Get user's club combined text for keyword extraction
        user_clubs_df = clubs_df[clubs_df['ClubId'].isin(user_club_ids)]
        user_interests_text = ' '.join(
            (user_clubs_df['Name'].fillna('') + ' ' +
             user_clubs_df['Description'].fillna('') + ' ' +
             user_clubs_df['Purpose'].fillna('')).tolist()
        )
        user_interests_text = self._preprocess_text(user_interests_text)
        
        # Get indices of user's clubs
        user_club_indices = [self.club_ids.index(cid) for cid in user_club_ids 
                           if cid in self.club_ids]
        
        if not user_club_indices:
            return pd.DataFrame({
                'EventId': events_df['EventId'],
                'content_similarity': 0.0,
                'title_match_score': 0.0
            })
        
        # Get user club vectors (average if multiple)
        user_vectors = self.club_vectors[user_club_indices]
        user_vector_avg = np.asarray(user_vectors.mean(axis=0))
        
        # Calculate similarity for each event
        similarities = []
        title_scores = []
        
        for _, event in events_df.iterrows():
            # Part 1: Club-to-Club similarity
            event_club_id = event['ClubId']
            club_sim = 0.0
            
            if event_club_id in self.club_ids:
                event_club_idx = self.club_ids.index(event_club_id)
                event_vector = self.club_vectors[event_club_idx]
                club_sim = cosine_similarity(user_vector_avg, event_vector)[0][0]
                club_sim = max(0.0, club_sim)
            
            # Part 2: Event content similarity (title + description)
            event_text = (
                str(event.get('Title', '')) + ' ' +
                str(event.get('Title', '')) + ' ' +  # Title gets more weight
                str(event.get('Description', '')) + ' ' +
                str(event.get('Location', ''))
            )
            event_text = self._preprocess_text(event_text)
            
            # Calculate keyword overlap and semantic similarity
            title_score = self._calculate_text_similarity(user_interests_text, event_text)
            
            # Combined similarity: 60% club similarity + 40% event content
            combined_sim = club_sim * 0.6 + title_score * 0.4
            
            similarities.append(combined_sim)
            title_scores.append(title_score)
        
        result_df = pd.DataFrame({
            'EventId': events_df['EventId'],
            'content_similarity': similarities,
            'title_match_score': title_scores
        })
        
        logger.debug(f"Calculated enhanced content similarity for {len(result_df)} events",
                    avg_similarity=np.mean(similarities),
                    avg_title_match=np.mean(title_scores))
        
        return result_df
    
    def _calculate_text_similarity(self, text1: str, text2: str) -> float:
        """
        Calculate similarity between two text strings using TF-IDF
        
        Args:
            text1: First text (e.g., user interests)
            text2: Second text (e.g., event description)
            
        Returns:
            Similarity score between 0 and 1
        """
        if not text1 or not text2:
            return 0.0
        
        try:
            # Create a temporary vectorizer for these two texts
            temp_vectorizer = TfidfVectorizer(
                max_features=100,
                ngram_range=(1, 2),
                lowercase=True,
                strip_accents='unicode'
            )
            
            vectors = temp_vectorizer.fit_transform([text1, text2])
            similarity = cosine_similarity(vectors[0:1], vectors[1:2])[0][0]
            
            # Also check for keyword overlap
            words1 = set(text1.split())
            words2 = set(text2.split())
            
            if words1 and words2:
                jaccard = len(words1 & words2) / len(words1 | words2)
            else:
                jaccard = 0.0
            
            # Combine cosine similarity (70%) and keyword overlap (30%)
            combined = similarity * 0.7 + jaccard * 0.3
            return max(0.0, min(1.0, combined))
            
        except Exception as e:
            logger.warning(f"Error calculating text similarity: {str(e)}")
            return 0.0
    
    def calculate_temporal_features(self, events_df: pd.DataFrame) -> pd.DataFrame:
        """
        Calculate temporal features for events
        
        Args:
            events_df: DataFrame with events (must have StartAt column)
            
        Returns:
            DataFrame with EventId and temporal features
        """
        if events_df.empty:
            return pd.DataFrame({
                'EventId': [],
                'days_until_event': [],
                'temporal_score': []
            })
        
        now = datetime.now(timezone.utc)
        
        # Calculate days until event
        events_df = events_df.copy()
        events_df['days_until_event'] = events_df['StartAt'].apply(
            lambda x: (x - now).total_seconds() / 86400 if pd.notna(x) else 999
        )
        
        # Temporal score: higher for upcoming events, decay over time
        decay_days = self.temporal_config.get('decay_days', 30)
        max_days_ahead = self.temporal_config.get('max_days_ahead', 90)
        recency_weight = self.temporal_config.get('recency_weight', 0.7)
        
        def temporal_score_fn(days_until):
            if days_until < 0:  # Past events
                return 0.0
            elif days_until > max_days_ahead:  # Too far in future
                return 0.1
            else:
                # Exponential decay: closer events get higher scores
                score = np.exp(-days_until / decay_days)
                return score * recency_weight + (1 - recency_weight) * 0.5
        
        events_df['temporal_score'] = events_df['days_until_event'].apply(temporal_score_fn)
        
        result_df = events_df[['EventId', 'days_until_event', 'temporal_score']].copy()
        
        logger.debug(f"Calculated temporal features for {len(result_df)} events",
                    avg_temporal_score=result_df['temporal_score'].mean())
        
        return result_df
    
    def calculate_user_affinity(self,
                               user_id: int,
                               events_df: pd.DataFrame,
                               user_club_ids: List[int],
                               user_history_df: pd.DataFrame) -> pd.DataFrame:
        """
        Calculate user affinity scores for events
        
        Args:
            user_id: User ID
            events_df: DataFrame with events
            user_club_ids: List of club IDs user follows
            user_history_df: User's past event interactions
            
        Returns:
            DataFrame with EventId and affinity features
        """
        if events_df.empty:
            return pd.DataFrame({
                'EventId': [],
                'is_following_club': [],
                'past_club_attendance': [],
                'user_affinity_score': []
            })
        
        events_df = events_df.copy()
        
        # Feature 1: Is user following the event's club?
        events_df['is_following_club'] = events_df['ClubId'].apply(
            lambda cid: 1.0 if cid in user_club_ids else 0.0
        )
        
        # Feature 2: Past attendance count for this club
        if not user_history_df.empty:
            club_attendance = user_history_df[user_history_df['Attended'] == 1].groupby('ClubId').size()
            events_df['past_club_attendance'] = events_df['ClubId'].apply(
                lambda cid: club_attendance.get(cid, 0)
            )
        else:
            events_df['past_club_attendance'] = 0
        
        # Normalize past attendance (0-1 scale)
        max_attendance = events_df['past_club_attendance'].max()
        if max_attendance > 0:
            events_df['past_club_attendance_norm'] = events_df['past_club_attendance'] / max_attendance
        else:
            events_df['past_club_attendance_norm'] = 0.0
        
        # Combined affinity score
        events_df['user_affinity_score'] = (
            events_df['is_following_club'] * 0.6 +
            events_df['past_club_attendance_norm'] * 0.4
        )
        
        result_df = events_df[[
            'EventId', 'is_following_club', 'past_club_attendance', 'user_affinity_score'
        ]].copy()
        
        logger.debug(f"Calculated user affinity for {len(result_df)} events",
                    avg_affinity=result_df['user_affinity_score'].mean())
        
        return result_df
    
    def calculate_popularity_features(self,
                                     events_df: pd.DataFrame,
                                     club_member_counts: Dict[int, int],
                                     club_event_counts: Dict[int, int]) -> pd.DataFrame:
        """
        Calculate popularity features for events based on club metrics
        
        Args:
            events_df: DataFrame with events
            club_member_counts: Dict mapping ClubId to member count
            club_event_counts: Dict mapping ClubId to recent event count
            
        Returns:
            DataFrame with EventId and popularity features
        """
        if events_df.empty:
            return pd.DataFrame({
                'EventId': [],
                'club_member_count': [],
                'club_event_count': [],
                'popularity_score': []
            })
        
        events_df = events_df.copy()
        
        # Map club metrics to events
        events_df['club_member_count'] = events_df['ClubId'].apply(
            lambda cid: club_member_counts.get(cid, 0)
        )
        events_df['club_event_count'] = events_df['ClubId'].apply(
            lambda cid: club_event_counts.get(cid, 0)
        )
        
        # Normalize to 0-1 scale
        max_members = max(club_member_counts.values()) if club_member_counts else 1
        max_events = max(club_event_counts.values()) if club_event_counts else 1
        
        events_df['member_norm'] = events_df['club_member_count'] / max_members
        events_df['event_norm'] = events_df['club_event_count'] / max_events
        
        # Combined popularity score
        events_df['popularity_score'] = (
            events_df['member_norm'] * 0.6 +
            events_df['event_norm'] * 0.4
        )
        
        result_df = events_df[[
            'EventId', 'club_member_count', 'club_event_count', 'popularity_score'
        ]].copy()
        
        logger.debug(f"Calculated popularity features for {len(result_df)} events",
                    avg_popularity=result_df['popularity_score'].mean())
        
        return result_df
    
    def combine_features(self, *feature_dfs: pd.DataFrame) -> pd.DataFrame:
        """
        Combine multiple feature DataFrames on EventId
        
        Args:
            *feature_dfs: Variable number of DataFrames with EventId column
            
        Returns:
            Combined DataFrame with all features
        """
        if not feature_dfs:
            return pd.DataFrame()
        
        # Start with first DataFrame
        combined = feature_dfs[0].copy()
        
        # Merge others
        for df in feature_dfs[1:]:
            if not df.empty:
                combined = combined.merge(df, on='EventId', how='left')
        
        # Fill any NaN values with 0
        combined = combined.fillna(0)
        
        logger.debug(f"Combined features for {len(combined)} events",
                    feature_count=len(combined.columns) - 1)  # -1 for EventId
        
        return combined

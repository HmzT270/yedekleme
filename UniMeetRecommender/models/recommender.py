"""
Hybrid Recommender System for UniMeet
Combines content-based, temporal, user affinity, and popularity features
"""
import json
import os
from typing import List, Dict, Optional, Tuple
from datetime import datetime, timezone
import pandas as pd
import numpy as np
from models.db_connector import DatabaseConnector
from models.feature_engine import FeatureEngine
from utils.logger import logger


class HybridRecommender:
    """Main recommendation engine combining multiple signals"""
    
    def __init__(self, config_path: str, db_connector: DatabaseConnector):
        """
        Initialize hybrid recommender
        
        Args:
            config_path: Path to config.json
            db_connector: Database connector instance
        """
        self.config = self._load_config(config_path)
        self.config_path = config_path
        self.db = db_connector
        self.feature_engine = FeatureEngine(self.config)
        
        # Cache for clubs data (refresh periodically)
        self._clubs_cache = None
        self._clubs_cache_time = None
        self._cache_ttl = 300  # 5 minutes
        
        logger.info("HybridRecommender initialized",
                   model_version=self.config['model']['version'])
    
    def _load_config(self, config_path: str) -> dict:
        """Load configuration from JSON file"""
        try:
            with open(config_path, 'r', encoding='utf-8') as f:
                config = json.load(f)
            logger.info(f"Loaded configuration from {config_path}")
            return config
        except Exception as e:
            logger.error(f"Error loading config: {str(e)}", exc_info=True)
            raise
    
    def reload_config(self):
        """Reload configuration from file"""
        self.config = self._load_config(self.config_path)
        self.feature_engine = FeatureEngine(self.config)
        logger.info("Configuration reloaded")
    
    def _get_clubs_data(self, force_refresh: bool = False) -> pd.DataFrame:
        """Get clubs data with caching"""
        now = datetime.now(timezone.utc)
        
        if (force_refresh or 
            self._clubs_cache is None or 
            self._clubs_cache_time is None or
            (now - self._clubs_cache_time).total_seconds() > self._cache_ttl):
            
            self._clubs_cache = self.db.get_club_details()
            self._clubs_cache_time = now
            logger.debug("Clubs cache refreshed")
        
        return self._clubs_cache
    
    def recommend(self, 
                  user_id: int, 
                  limit: int = 10,
                  filters: Optional[Dict] = None) -> Dict:
        """
        Generate event recommendations for a user
        
        Args:
            user_id: User ID
            limit: Maximum number of recommendations
            filters: Optional filters (min_date, max_date, exclude_event_ids)
            
        Returns:
            Dict with recommendations and metadata
        """
        start_time = datetime.now(timezone.utc)
        
        try:
            # Step 1: Get user's followed clubs
            user_club_ids = self.db.get_user_followed_clubs(user_id)
            
            logger.info(f"User {user_id} follows {len(user_club_ids)} clubs: {user_club_ids}")
            
            if not user_club_ids:
                logger.info(f"User {user_id} follows no clubs, using fallback")
                return self._fallback_recommendations(user_id, limit, filters)
            
            # Step 2: Get candidate events
            event_filters = filters or {}
            if 'min_date' not in event_filters:
                event_filters['min_date'] = datetime.now(timezone.utc)
            
            events_df = self.db.get_all_events(event_filters)
            
            # Step 2.5: Get user history and exclude attended events
            user_history_df = self.db.get_user_event_history(user_id)
            if not user_history_df.empty:
                attended_event_ids = user_history_df[user_history_df['Attended'] == 1]['EventId'].tolist()
                if attended_event_ids:
                    events_df = events_df[~events_df['EventId'].isin(attended_event_ids)]
                    logger.info(f"Filtered out {len(attended_event_ids)} attended events for user {user_id}")
            
            if events_df.empty:
                logger.info(f"No candidate events found for user {user_id}")
                return {
                    'recommendations': [],
                    'metadata': {
                        'model_version': self.config['model']['version'],
                        'computed_at': datetime.now(timezone.utc).isoformat(),
                        'total_candidates': 0,
                        'user_follows_clubs': len(user_club_ids)
                    }
                }
            
            # Step 3: Get clubs data
            clubs_df = self._get_clubs_data()
            
            # Step 4: Get popularity metrics
            club_member_counts = self.db.get_club_member_counts()
            club_event_counts = self.db.get_club_event_counts()
            
            # Step 6: Calculate features
            features = self._calculate_all_features(
                user_id=user_id,
                user_club_ids=user_club_ids,
                events_df=events_df,
                clubs_df=clubs_df,
                user_history_df=user_history_df,
                club_member_counts=club_member_counts,
                club_event_counts=club_event_counts
            )
            
            # Step 7: Score and rank
            scored_events = self._score_events(features, events_df)
            
            # Step 8: Select ONLY the best (top 1) recommendation
            if scored_events.empty:
                recommendations = scored_events
            else:
                recommendations = scored_events.head(1)
                logger.info(f"Selected best recommendation with score: {recommendations.iloc[0]['final_score']:.3f}")
            
            # Step 9: Format output
            result = self._format_recommendations(
                recommendations,
                events_df,
                user_club_ids,
                start_time
            )
            
            # Log
            latency_ms = (datetime.now(timezone.utc) - start_time).total_seconds() * 1000
            logger.log_request(
                user_id=user_id,
                action='recommend',
                latency_ms=latency_ms,
                result_count=len(result['recommendations'])
            )
            
            return result
            
        except Exception as e:
            logger.error(f"Error generating recommendations for user {user_id}: {str(e)}", 
                        exc_info=True)
            return self._fallback_recommendations(user_id, limit, filters)
    
    def _calculate_all_features(self,
                               user_id: int,
                               user_club_ids: List[int],
                               events_df: pd.DataFrame,
                               clubs_df: pd.DataFrame,
                               user_history_df: pd.DataFrame,
                               club_member_counts: Dict[int, int],
                               club_event_counts: Dict[int, int]) -> pd.DataFrame:
        """Calculate all features for events"""
        
        # Content similarity
        content_features = self.feature_engine.calculate_content_similarity(
            user_club_ids, events_df, clubs_df
        )
        
        # Temporal features
        temporal_features = self.feature_engine.calculate_temporal_features(events_df)
        
        # User affinity
        affinity_features = self.feature_engine.calculate_user_affinity(
            user_id, events_df, user_club_ids, user_history_df
        )
        
        # Popularity
        popularity_features = self.feature_engine.calculate_popularity_features(
            events_df, club_member_counts, club_event_counts
        )
        
        # Combine all features
        all_features = self.feature_engine.combine_features(
            content_features,
            temporal_features,
            affinity_features,
            popularity_features
        )
        
        return all_features
    
    def _score_events(self, features_df: pd.DataFrame, events_df: pd.DataFrame) -> pd.DataFrame:
        """
        Score events using weighted sum of features
        
        Args:
            features_df: DataFrame with all features
            events_df: Original events DataFrame
            
        Returns:
            DataFrame with EventId and final_score
        """
        weights = self.config['scoring_weights']
        
        # Calculate weighted score
        features_df = features_df.copy()
        
        # Handle missing features gracefully
        content_sim = features_df.get('content_similarity', 0)
        title_match = features_df.get('title_match_score', 0)
        temporal = features_df.get('temporal_score', 0)
        affinity = features_df.get('user_affinity_score', 0)
        popularity = features_df.get('popularity_score', 0)
        is_following = features_df.get('is_following_club', 0)
        
        # Enhanced scoring formula
        features_df['final_score'] = (
            content_sim * weights.get('content_similarity', 0.20) +
            title_match * weights.get('title_match', 0.15) +
            temporal * weights.get('temporal_score', 0.15) +
            affinity * weights.get('user_past_behavior', 0.15) +
            popularity * weights.get('club_popularity', 0.05) +
            is_following * weights.get('club_membership_match', 0.30)
        )
        
        # Boost score if event title has high match (indicates strong relevance)
        high_title_match_mask = features_df.get('title_match_score', 0) > 0.4
        features_df.loc[high_title_match_mask, 'final_score'] *= 1.15
        
        # Apply minimum threshold
        min_threshold = self.config['ranking_settings'].get('min_score_threshold', 0.05)
        before_filter = len(features_df)
        features_df = features_df[features_df['final_score'] >= min_threshold]
        after_filter = len(features_df)
        
        logger.info(f"Threshold filter: {before_filter} events -> {after_filter} events (min_score={min_threshold})")
        
        # Sort by score
        features_df = features_df.sort_values('final_score', ascending=False)
        
        # Log top scores for debugging
        if not features_df.empty:
            top_events = features_df.head(10)
            logger.info(f"Top 10 scored events:")
            for idx, row in top_events.iterrows():
                logger.info(f"  EventId={row['EventId']}, Score={row['final_score']:.3f}, "
                           f"Following={row.get('is_following_club', 0):.1f}, "
                           f"Content={row.get('content_similarity', 0):.3f}, "
                           f"Title={row.get('title_match_score', 0):.3f}")
        
        logger.debug(f"Scored {len(features_df)} events",
                    avg_score=features_df['final_score'].mean() if not features_df.empty else 0,
                    max_score=features_df['final_score'].max() if not features_df.empty else 0)
        
        return features_df
    
    def _select_diverse_recommendations(self,
                                       scored_events: pd.DataFrame,
                                       events_df: pd.DataFrame,
                                       limit: int) -> pd.DataFrame:
        """
        Select diverse recommendations to avoid clustering
        
        Args:
            scored_events: DataFrame with scored events
            events_df: Original events DataFrame
            limit: Number of recommendations to return
            
        Returns:
            DataFrame with selected recommendations
        """
        if scored_events.empty:
            return scored_events
        
        # Merge with events to get ClubId
        scored_with_clubs = scored_events.merge(
            events_df[['EventId', 'ClubId']], 
            on='EventId', 
            how='left'
        )
        
        # Simple diversity: limit events per club
        diversity_factor = self.config['ranking_settings'].get('diversity_factor', 0.2)
        max_per_club = max(1, int(limit * diversity_factor))
        
        selected = []
        club_counts = {}
        
        for _, row in scored_with_clubs.iterrows():
            club_id = row['ClubId']
            count = club_counts.get(club_id, 0)
            
            if count < max_per_club or len(selected) < limit // 2:
                selected.append(row)
                club_counts[club_id] = count + 1
            
            if len(selected) >= limit:
                break
        
        # If we don't have enough, fill with highest scored
        if len(selected) < limit:
            remaining = scored_with_clubs[
                ~scored_with_clubs['EventId'].isin([r['EventId'] for r in selected])
            ].head(limit - len(selected))
            selected.extend(remaining.to_dict('records'))
        
        result_df = pd.DataFrame(selected).head(limit)
        
        logger.debug(f"Selected {len(result_df)} diverse recommendations")
        
        return result_df
    
    def _format_recommendations(self,
                               recommendations: pd.DataFrame,
                               events_df: pd.DataFrame,
                               user_club_ids: List[int],
                               start_time: datetime) -> Dict:
        """Format recommendations for API response"""
        
        if recommendations.empty:
            return {
                'recommendations': [],
                'metadata': {
                    'model_version': self.config['model']['version'],
                    'computed_at': datetime.now(timezone.utc).isoformat(),
                    'total_candidates': len(events_df),
                    'computation_time_ms': (datetime.now(timezone.utc) - start_time).total_seconds() * 1000
                }
            }
        
        # Merge with events for full details
        recs_with_events = recommendations.merge(
            events_df, on='EventId', how='left'
        )
        
        formatted_recs = []
        for _, row in recs_with_events.iterrows():
            # Determine primary reason
            reason = self._generate_reason(row, user_club_ids)
            
            rec = {
                'eventId': int(row['EventId']),
                'score': float(row['final_score']),
                'reason': reason
            }
            
            formatted_recs.append(rec)
        
        return {
            'recommendations': formatted_recs,
            'metadata': {
                'model_version': self.config['model']['version'],
                'computed_at': datetime.now(timezone.utc).isoformat(),
                'total_candidates': len(events_df),
                'computation_time_ms': (datetime.now(timezone.utc) - start_time).total_seconds() * 1000,
                'user_follows_clubs': len(user_club_ids)
            }
        }
    
    def _generate_reason(self, row: pd.Series, user_club_ids: List[int]) -> Dict:
        """Generate explanation for recommendation"""
        
        # Get all feature scores
        is_following = row.get('is_following_club', 0) > 0
        content_sim = row.get('content_similarity', 0)
        title_match = row.get('title_match_score', 0)
        past_attendance = row.get('past_club_attendance', 0)
        temporal = row.get('temporal_score', 0)
        
        # Determine primary reason (in priority order)
        if is_following:
            primary = "club_membership"
            details = "You're a member of this club"
        elif title_match > 0.5:
            primary = "highly_relevant"
            details = f"Event content strongly matches your interests ({int(title_match*100)}% match)"
        elif past_attendance > 0:
            primary = "user_history"
            details = f"You've attended {int(past_attendance)} event(s) from this club"
        elif content_sim > 0.4:
            primary = "similar_content"
            details = f"Similar to clubs you follow ({int(content_sim*100)}% similarity)"
        elif title_match > 0.3:
            primary = "relevant_topic"
            details = f"Event topic matches your interests ({int(title_match*100)}% match)"
        elif temporal > 0.5:
            primary = "upcoming_soon"
            days_until = row.get('days_until_event', 0)
            if days_until < 1:
                details = "Happening today!"
            elif days_until < 3:
                details = f"Happening in {int(days_until)} day(s)"
            else:
                details = "Upcoming event"
        else:
            primary = "popular"
            details = "Popular event in your campus"
        
        return {
            'primary': primary,
            'details': details,
            'features': {
                'content_similarity': round(float(content_sim), 3),
                'title_match': round(float(title_match), 3),
                'temporal_score': round(float(temporal), 3),
                'user_affinity': round(float(row.get('user_affinity_score', 0)), 3),
                'popularity': round(float(row.get('popularity_score', 0)), 3)
            }
        }
    
    def _fallback_recommendations(self, 
                                 user_id: int, 
                                 limit: int,
                                 filters: Optional[Dict]) -> Dict:
        """
        Fallback recommendations when user has no followed clubs
        Returns most popular upcoming events
        """
        logger.info(f"Using fallback recommendations for user {user_id}")
        
        try:
            event_filters = filters or {}
            if 'min_date' not in event_filters:
                event_filters['min_date'] = datetime.now(timezone.utc)
            
            events_df = self.db.get_all_events(event_filters)
            
            if events_df.empty:
                return {
                    'recommendations': [],
                    'metadata': {
                        'model_version': self.config['model']['version'],
                        'computed_at': datetime.now(timezone.utc).isoformat(),
                        'fallback': True,
                        'reason': 'No events available'
                    }
                }
            
            # Sort by start date (upcoming first)
            events_df = events_df.sort_values('StartAt').head(limit)
            
            recommendations = []
            for _, event in events_df.iterrows():
                recommendations.append({
                    'eventId': int(event['EventId']),
                    'score': 0.5,  # Neutral score for fallback
                    'reason': {
                        'primary': 'fallback',
                        'details': 'Upcoming public event',
                        'features': {}
                    }
                })
            
            return {
                'recommendations': recommendations,
                'metadata': {
                    'model_version': self.config['model']['version'],
                    'computed_at': datetime.now(timezone.utc).isoformat(),
                    'fallback': True,
                    'total_candidates': len(events_df)
                }
            }
            
        except Exception as e:
            logger.error(f"Error in fallback recommendations: {str(e)}", exc_info=True)
            return {
                'recommendations': [],
                'metadata': {
                    'model_version': self.config['model']['version'],
                    'computed_at': datetime.now(timezone.utc).isoformat(),
                    'error': True
                }
            }
    
    def get_config(self) -> dict:
        """Get current configuration"""
        return self.config
    
    def update_config(self, new_config: dict):
        """
        Update configuration (only scoring_weights allowed for safety)
        
        Args:
            new_config: Dict with new configuration values
        """
        # Only allow updating scoring weights for safety
        if 'scoring_weights' in new_config:
            self.config['scoring_weights'].update(new_config['scoring_weights'])
            
            # Save to file
            try:
                with open(self.config_path, 'w', encoding='utf-8') as f:
                    json.dump(self.config, f, indent=2, ensure_ascii=False)
                
                logger.info("Configuration updated", 
                           new_weights=new_config['scoring_weights'])
            except Exception as e:
                logger.error(f"Error saving config: {str(e)}", exc_info=True)
                raise

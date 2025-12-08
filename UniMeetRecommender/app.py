"""
Flask API for UniMeet Recommendation Service
Provides REST endpoints for event recommendations
"""
import os
import time
from datetime import datetime, timezone
from functools import wraps
from typing import Dict, Optional
from flask import Flask, request, jsonify
from flask_cors import CORS
from dotenv import load_dotenv

from models.db_connector import DatabaseConnector
from models.recommender import HybridRecommender
from utils.logger import logger

# Load environment variables
load_dotenv()

# Initialize Flask app
app = Flask(__name__)

# CORS: Only allow .NET backend
CORS(app, resources={
    r"/api/*": {
        "origins": [
            "http://localhost:5062",
            "https://localhost:5062"
        ]
    }
})

# Global instances
db_connector: Optional[DatabaseConnector] = None
recommender: Optional[HybridRecommender] = None
request_stats = {
    'total_requests': 0,
    'total_latency_ms': 0,
    'last_request_time': None
}


def init_services():
    """Initialize database and recommender services"""
    global db_connector, recommender
    
    # Get configuration
    db_connection_string = os.getenv('DB_CONNECTION_STRING')
    if not db_connection_string:
        raise ValueError("DB_CONNECTION_STRING environment variable not set")
    
    config_path = os.path.join(os.path.dirname(__file__), 'config.json')
    
    # Load config to get DB settings
    import json
    with open(config_path, 'r', encoding='utf-8') as f:
        config = json.load(f)
    
    # Initialize database connector
    db_connector = DatabaseConnector(db_connection_string, config['database'])
    
    # Test connection
    if not db_connector.test_connection():
        raise ConnectionError("Failed to connect to database")
    
    # Initialize recommender
    recommender = HybridRecommender(config_path, db_connector)
    
    logger.info("Services initialized successfully")


def require_api_key(f):
    """Decorator to require API key for admin endpoints"""
    @wraps(f)
    def decorated_function(*args, **kwargs):
        api_key = request.headers.get('X-API-Key')
        expected_key = os.getenv('API_KEY')
        
        if not api_key or api_key != expected_key:
            logger.warning("Unauthorized API access attempt")
            return jsonify({'error': 'Unauthorized'}), 401
        
        return f(*args, **kwargs)
    return decorated_function


def update_stats(latency_ms: float):
    """Update request statistics"""
    request_stats['total_requests'] += 1
    request_stats['total_latency_ms'] += latency_ms
    request_stats['last_request_time'] = datetime.now(timezone.utc).isoformat()


# ===== API ENDPOINTS =====

@app.route('/api/v1/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    try:
        db_healthy = db_connector.test_connection() if db_connector else False
        
        return jsonify({
            'status': 'ok' if db_healthy else 'degraded',
            'version': recommender.config['model']['version'] if recommender else 'unknown',
            'timestamp': datetime.now(timezone.utc).isoformat(),
            'database': 'connected' if db_healthy else 'disconnected'
        }), 200 if db_healthy else 503
    except Exception as e:
        logger.error(f"Health check failed: {str(e)}")
        return jsonify({
            'status': 'error',
            'error': str(e)
        }), 500


@app.route('/api/v1/recommend', methods=['POST'])
def recommend():
    """
    Main recommendation endpoint
    
    Request body:
    {
        "userId": int,
        "limit": int (optional, default 10),
        "context": {
            "excludeEventIds": [int],
            "filters": {
                "minDate": "ISO datetime",
                "maxDate": "ISO datetime"
            }
        }
    }
    """
    start_time = time.time()
    
    try:
        # Parse request
        data = request.get_json()
        if not data:
            return jsonify({'error': 'Request body required'}), 400
        
        user_id = data.get('userId')
        if user_id is None:
            return jsonify({'error': 'userId is required'}), 400
        
        limit = data.get('limit', 10)
        limit = min(limit, recommender.config['ranking_settings']['max_limit'])
        
        # Parse filters
        context = data.get('context', {})
        filters = context.get('filters', {})
        
        # Convert date strings to datetime
        if 'minDate' in filters and filters['minDate']:
            try:
                filters['min_date'] = datetime.fromisoformat(filters['minDate'].replace('Z', '+00:00'))
            except:
                pass
        
        if 'maxDate' in filters and filters['maxDate']:
            try:
                filters['max_date'] = datetime.fromisoformat(filters['maxDate'].replace('Z', '+00:00'))
            except:
                pass
        
        if 'excludeEventIds' in context:
            filters['exclude_event_ids'] = context['excludeEventIds']
        
        # Generate recommendations
        result = recommender.recommend(user_id, limit, filters)
        
        # Update stats
        latency_ms = (time.time() - start_time) * 1000
        update_stats(latency_ms)
        
        return jsonify(result), 200
        
    except Exception as e:
        logger.error(f"Error in recommend endpoint: {str(e)}", exc_info=True)
        return jsonify({
            'error': 'Internal server error',
            'message': str(e)
        }), 500


@app.route('/api/v1/config', methods=['GET'])
def get_config():
    """Get current configuration (read-only view)"""
    try:
        config = recommender.get_config()
        
        # Return only safe parts
        safe_config = {
            'model': config['model'],
            'scoring_weights': config['scoring_weights'],
            'ranking_settings': config['ranking_settings']
        }
        
        return jsonify(safe_config), 200
    except Exception as e:
        logger.error(f"Error getting config: {str(e)}")
        return jsonify({'error': str(e)}), 500


@app.route('/api/v1/config', methods=['PUT'])
@require_api_key
def update_config():
    """
    Update configuration (admin only, requires API key)
    
    Request body:
    {
        "scoring_weights": {
            "club_membership_match": 0.30,
            ...
        }
    }
    """
    try:
        data = request.get_json()
        if not data:
            return jsonify({'error': 'Request body required'}), 400
        
        recommender.update_config(data)
        
        return jsonify({
            'status': 'updated',
            'timestamp': datetime.now(timezone.utc).isoformat()
        }), 200
        
    except Exception as e:
        logger.error(f"Error updating config: {str(e)}", exc_info=True)
        return jsonify({'error': str(e)}), 500


@app.route('/api/v1/reload-config', methods=['POST'])
@require_api_key
def reload_config():
    """Reload configuration from file (admin only)"""
    try:
        recommender.reload_config()
        
        return jsonify({
            'status': 'reloaded',
            'timestamp': datetime.now(timezone.utc).isoformat(),
            'version': recommender.config['model']['version']
        }), 200
        
    except Exception as e:
        logger.error(f"Error reloading config: {str(e)}", exc_info=True)
        return jsonify({'error': str(e)}), 500


@app.route('/api/v1/stats', methods=['GET'])
def get_stats():
    """Get service statistics"""
    avg_latency = (
        request_stats['total_latency_ms'] / request_stats['total_requests']
        if request_stats['total_requests'] > 0
        else 0
    )
    
    return jsonify({
        'total_requests': request_stats['total_requests'],
        'avg_latency_ms': round(avg_latency, 2),
        'last_request_time': request_stats['last_request_time'],
        'model_version': recommender.config['model']['version'] if recommender else 'unknown'
    }), 200


@app.errorhandler(404)
def not_found(e):
    """Handle 404 errors"""
    return jsonify({'error': 'Endpoint not found'}), 404


@app.errorhandler(500)
def internal_error(e):
    """Handle 500 errors"""
    logger.error(f"Internal server error: {str(e)}", exc_info=True)
    return jsonify({'error': 'Internal server error'}), 500


# ===== APPLICATION STARTUP =====

if __name__ == '__main__':
    try:
        # Initialize services
        logger.info("Starting UniMeet Recommendation Service...")
        init_services()
        
        # Get configuration
        host = os.getenv('HOST', '0.0.0.0')
        port = int(os.getenv('PORT', 5000))
        debug = os.getenv('FLASK_DEBUG', '0') == '1'
        
        logger.info(f"Server starting on {host}:{port}")
        
        # Run Flask app
        app.run(
            host=host,
            port=port,
            debug=debug
        )
        
    except Exception as e:
        logger.error(f"Failed to start application: {str(e)}", exc_info=True)
        raise
    finally:
        # Cleanup
        if db_connector:
            db_connector.close()

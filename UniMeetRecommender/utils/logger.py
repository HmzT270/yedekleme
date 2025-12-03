"""
Logger utility for UniMeet Recommender Service
Provides structured JSON logging
"""
import logging
import json
import sys
from datetime import datetime
from typing import Any, Dict, Optional


class StructuredLogger:
    """Structured JSON logger for the recommendation service"""
    
    def __init__(self, name: str = "recommender", level: str = "INFO"):
        self.logger = logging.getLogger(name)
        self.logger.setLevel(getattr(logging, level.upper(), logging.INFO))
        
        # Clear existing handlers
        self.logger.handlers.clear()
        
        # Console handler with JSON formatter
        handler = logging.StreamHandler(sys.stdout)
        handler.setFormatter(self._get_json_formatter())
        self.logger.addHandler(handler)
    
    def _get_json_formatter(self):
        """Custom JSON formatter"""
        class JsonFormatter(logging.Formatter):
            def format(self, record):
                log_obj = {
                    "timestamp": datetime.utcnow().isoformat() + "Z",
                    "level": record.levelname,
                    "message": record.getMessage(),
                    "module": record.module,
                    "function": record.funcName,
                }
                
                # Add extra fields if present
                if hasattr(record, 'user_id'):
                    log_obj['userId'] = record.user_id
                if hasattr(record, 'latency_ms'):
                    log_obj['latency_ms'] = record.latency_ms
                if hasattr(record, 'result_count'):
                    log_obj['result_count'] = record.result_count
                if hasattr(record, 'action'):
                    log_obj['action'] = record.action
                if hasattr(record, 'extra'):
                    log_obj.update(record.extra)
                
                return json.dumps(log_obj, ensure_ascii=False)
        
        return JsonFormatter()
    
    def info(self, message: str, **kwargs):
        """Log info message with optional extra fields"""
        extra_dict = {'extra': kwargs} if kwargs else {}
        self.logger.info(message, extra=extra_dict)
    
    def warning(self, message: str, **kwargs):
        """Log warning message with optional extra fields"""
        extra_dict = {'extra': kwargs} if kwargs else {}
        self.logger.warning(message, extra=extra_dict)
    
    def error(self, message: str, exc_info: bool = False, **kwargs):
        """Log error message with optional extra fields"""
        extra_dict = {'extra': kwargs} if kwargs else {}
        self.logger.error(message, exc_info=exc_info, extra=extra_dict)
    
    def debug(self, message: str, **kwargs):
        """Log debug message with optional extra fields"""
        extra_dict = {'extra': kwargs} if kwargs else {}
        self.logger.debug(message, extra=extra_dict)
    
    def log_request(self, user_id: int, action: str, latency_ms: float, 
                    result_count: int, **kwargs):
        """Log a request with standard fields"""
        self.logger.info(
            f"Request completed: {action}",
            extra={
                'user_id': user_id,
                'action': action,
                'latency_ms': latency_ms,
                'result_count': result_count,
                'extra': kwargs
            }
        )


# Global logger instance
logger = StructuredLogger()

"""
Hermes Python SDK - Universal client for Monado Blade platform
Async/sync APIs, streaming support, task queue integration
"""

import asyncio
import os
import time
import logging
import json
from typing import Any, Dict, Optional, Callable, AsyncIterator
from dataclasses import dataclass
from enum import Enum
from datetime import timedelta
import httpx
from functools import wraps


logger = logging.getLogger(__name__)


class ErrorCode(Enum):
    """Typed error codes - used across all language SDKs"""
    OK = "OK"
    SDK_NOT_SUPPORTED = "SDK_NOT_SUPPORTED"
    CIRCUIT_BREAKER_OPEN = "CIRCUIT_BREAKER_OPEN"
    TIMEOUT = "TIMEOUT"
    AUTHENTICATION_FAILED = "AUTHENTICATION_FAILED"
    VALIDATION_FAILED = "VALIDATION_FAILED"
    RESOURCE_NOT_FOUND = "NOT_FOUND"
    INTERNAL_ERROR = "INTERNAL_ERROR"


class HermesException(Exception):
    """Universal SDK Exception - used across all SDKs"""
    
    def __init__(
        self,
        error_code: str,
        message: str,
        request_id: Optional[str] = None,
        context: Optional[Dict[str, Any]] = None
    ):
        self.error_code = error_code
        self.message = message
        self.request_id = request_id
        self.context = context or {}
        super().__init__(f"[{error_code}] {message}")


@dataclass
class SDKConfig:
    """Universal SDK Configuration - all Python SDKs inherit from this"""
    
    api_key: str
    api_secret: str = ""
    endpoint: str = "https://api.monado-blade.io"
    region: str = "us-east-1"
    
    # Authentication
    use_mtls: bool = True
    tls_cert_path: Optional[str] = None
    tls_key_path: Optional[str] = None
    
    # Networking
    connect_timeout: float = 10.0
    request_timeout: float = 30.0
    max_pool_size: int = 100
    enable_compression: bool = True
    
    # Retry Policy
    max_retries: int = 3
    initial_retry_delay: float = 0.1  # seconds
    retry_backoff_multiplier: float = 2.0
    max_retry_delay: float = 10.0
    
    # Circuit Breaker
    enable_circuit_breaker: bool = True
    circuit_breaker_threshold: int = 10
    circuit_breaker_timeout: float = 60.0
    
    # Logging & Metrics
    enable_debug_logging: bool = False
    enable_metrics: bool = True
    metrics_endpoint: Optional[str] = None
    
    @classmethod
    def from_env(cls, prefix: str = "MONADO_") -> "SDKConfig":
        """Load configuration from environment variables"""
        return cls(
            api_key=os.getenv(f"{prefix}API_KEY", ""),
            api_secret=os.getenv(f"{prefix}API_SECRET", ""),
            endpoint=os.getenv(f"{prefix}ENDPOINT", "https://api.monado-blade.io"),
            region=os.getenv(f"{prefix}REGION", "us-east-1"),
            use_mtls=os.getenv(f"{prefix}USE_MTLS", "true").lower() == "true",
        )


class CircuitBreaker:
    """Circuit breaker pattern - prevents cascading failures"""
    
    class State(Enum):
        CLOSED = "closed"
        OPEN = "open"
        HALF_OPEN = "half_open"
    
    def __init__(
        self,
        name: str,
        failure_threshold: int = 10,
        open_timeout: float = 60.0
    ):
        self.name = name
        self.failure_threshold = failure_threshold
        self.open_timeout = open_timeout
        self.state = self.State.CLOSED
        self.failure_count = 0
        self.last_failure_time = 0.0
        self.opened_time = 0.0
    
    async def execute(self, coro, *args, **kwargs):
        """Execute with circuit breaker protection"""
        if self.state == self.State.OPEN:
            if time.time() - self.opened_time > self.open_timeout:
                self.state = self.State.HALF_OPEN
                self.failure_count = 0
            else:
                raise HermesException(
                    ErrorCode.CIRCUIT_BREAKER_OPEN.value,
                    f"Circuit breaker '{self.name}' is open"
                )
        
        try:
            result = await coro
            self.failure_count = 0
            if self.state == self.State.HALF_OPEN:
                self.state = self.State.CLOSED
            return result
        except Exception as e:
            self.failure_count += 1
            self.last_failure_time = time.time()
            
            if self.failure_count >= self.failure_threshold:
                self.state = self.State.OPEN
                self.opened_time = time.time()
            raise


class RetryPolicy:
    """Retry policy with exponential backoff"""
    
    def __init__(
        self,
        max_retries: int = 3,
        initial_delay: float = 0.1,
        backoff_multiplier: float = 2.0,
        max_delay: float = 10.0,
        should_retry: Optional[Callable[[Exception, int], bool]] = None
    ):
        self.max_retries = max_retries
        self.initial_delay = initial_delay
        self.backoff_multiplier = backoff_multiplier
        self.max_delay = max_delay
        self.should_retry = should_retry or self._default_should_retry
    
    async def execute(self, coro_fn: Callable[[], Any]) -> Any:
        """Execute with retry logic"""
        last_exception = None
        
        for attempt in range(self.max_retries + 1):
            try:
                return await coro_fn()
            except Exception as ex:
                last_exception = ex
                
                if attempt >= self.max_retries or not self.should_retry(ex, attempt):
                    raise
                
                delay = self._calculate_delay(attempt)
                logger.debug(f"Retry attempt {attempt + 1}/{self.max_retries} after {delay:.3f}s")
                await asyncio.sleep(delay)
        
        raise last_exception
    
    def _calculate_delay(self, attempt_number: int) -> float:
        """Calculate delay with jitter"""
        import random
        delay = self.initial_delay * (self.backoff_multiplier ** attempt_number)
        jitter = random.uniform(0, 0.1 * delay)
        return min(delay + jitter, self.max_delay)
    
    @staticmethod
    def _default_should_retry(ex: Exception, attempt: int) -> bool:
        """Default retry logic - transient errors only"""
        if isinstance(ex, asyncio.TimeoutError):
            return True
        if isinstance(ex, HermesException):
            # Retry on circuit breaker or transient errors
            return ex.error_code in [
                ErrorCode.CIRCUIT_BREAKER_OPEN.value,
                "HTTP_429",  # Too many requests
                "HTTP_503",  # Service unavailable
            ]
        return False


class HermesClient:
    """
    Universal Hermes Python SDK Client
    
    Example:
        client = HermesClient(api_key="xxx")
        
        # Query LLM
        response = await client.llm.complete(
            provider="claude",
            model="claude-3-opus",
            prompt="What is machine learning?"
        )
        
        # Run distributed task
        result = await client.tasks.execute(task)
        
        # Stream responses
        async for chunk in client.llm.stream(prompt="..."):
            print(chunk.text)
    """
    
    def __init__(self, config: Optional[SDKConfig] = None):
        self.config = config or SDKConfig.from_env()
        self._circuit_breaker = CircuitBreaker(
            "hermes-client",
            self.config.circuit_breaker_threshold,
            self.config.circuit_breaker_timeout
        )
        self._retry_policy = RetryPolicy(
            self.config.max_retries,
            self.config.initial_retry_delay,
            self.config.retry_backoff_multiplier,
            self.config.max_retry_delay
        )
        self._http_client = httpx.AsyncClient(
            timeout=httpx.Timeout(self.config.request_timeout),
            limits=httpx.Limits(max_keepalive_connections=self.config.max_pool_size)
        )
        
        # SDK sub-clients
        self.llm = LLMClient(self)
        self.tasks = TaskClient(self)
        self.ai_hub = AIHubClient(self)
    
    async def request(
        self,
        method: str,
        path: str,
        **kwargs
    ) -> Dict[str, Any]:
        """Universal request method with retry & circuit breaker"""
        
        async def _execute():
            url = f"{self.config.endpoint}{path}"
            headers = {
                "Authorization": f"Bearer {self.config.api_key}",
                "X-SDK-Version": "1.0.0",
                "User-Agent": "HermesSDK/1.0.0"
            }
            
            response = await self._http_client.request(
                method,
                url,
                headers=headers,
                **kwargs
            )
            response.raise_for_status()
            return response.json()
        
        try:
            return await self._circuit_breaker.execute(
                self._retry_policy.execute(_execute)
            )
        except HermesException:
            raise
        except Exception as ex:
            raise HermesException(
                ErrorCode.INTERNAL_ERROR.value,
                f"Request failed: {str(ex)}"
            )
    
    async def close(self):
        """Close HTTP client"""
        await self._http_client.aclose()
    
    async def __aenter__(self):
        return self
    
    async def __aexit__(self, exc_type, exc_val, exc_tb):
        await self.close()


class LLMClient:
    """LLM API Client - unified interface to multiple providers"""
    
    def __init__(self, parent: HermesClient):
        self.parent = parent
    
    async def complete(
        self,
        provider: str,
        model: str,
        prompt: str,
        temperature: float = 0.7,
        max_tokens: int = 1024,
        **kwargs
    ) -> Dict[str, Any]:
        """Complete text with specified provider"""
        
        return await self.parent.request(
            "POST",
            "/v1/llm/complete",
            json={
                "provider": provider,
                "model": model,
                "prompt": prompt,
                "temperature": temperature,
                "max_tokens": max_tokens,
                **kwargs
            }
        )
    
    async def stream(
        self,
        provider: str = "claude",
        model: str = "claude-3-opus",
        prompt: str = "",
        **kwargs
    ) -> AsyncIterator[Dict[str, Any]]:
        """Stream responses from LLM"""
        
        async with self.parent._http_client.stream(
            "POST",
            f"{self.parent.config.endpoint}/v1/llm/stream",
            json={
                "provider": provider,
                "model": model,
                "prompt": prompt,
                **kwargs
            },
            headers={
                "Authorization": f"Bearer {self.parent.config.api_key}",
            }
        ) as response:
            async for line in response.aiter_lines():
                if line:
                    yield json.loads(line)
    
    async def embeddings(
        self,
        provider: str,
        model: str,
        texts: list[str],
        **kwargs
    ) -> Dict[str, Any]:
        """Get embeddings for text"""
        
        return await self.parent.request(
            "POST",
            "/v1/llm/embeddings",
            json={
                "provider": provider,
                "model": model,
                "texts": texts,
                **kwargs
            }
        )


class TaskClient:
    """Task Execution Client - distributed task runner"""
    
    def __init__(self, parent: HermesClient):
        self.parent = parent
    
    async def execute(
        self,
        task_name: str,
        input_data: Optional[Dict[str, Any]] = None,
        resources: Optional[Dict[str, int]] = None,
        timeout: int = 3600,
        **kwargs
    ) -> Dict[str, Any]:
        """Execute distributed task"""
        
        return await self.parent.request(
            "POST",
            "/v1/tasks/execute",
            json={
                "name": task_name,
                "input": input_data or {},
                "resources": resources or {"cpu": 1, "memory": 512},
                "timeout": timeout,
                **kwargs
            }
        )
    
    async def get_status(self, task_id: str) -> Dict[str, Any]:
        """Get task execution status"""
        
        return await self.parent.request(
            "GET",
            f"/v1/tasks/{task_id}/status"
        )
    
    async def list_tasks(self, limit: int = 10) -> Dict[str, Any]:
        """List tasks"""
        
        return await self.parent.request(
            "GET",
            "/v1/tasks",
            params={"limit": limit}
        )


class AIHubClient:
    """AI Learning Hub Client - model training & optimization"""
    
    def __init__(self, parent: HermesClient):
        self.parent = parent
    
    async def train_model(
        self,
        dataset_id: str,
        model_type: str,
        hyperparameters: Optional[Dict[str, Any]] = None,
        **kwargs
    ) -> Dict[str, Any]:
        """Train a model in the AI Hub"""
        
        return await self.parent.request(
            "POST",
            "/v1/ai-hub/train",
            json={
                "dataset_id": dataset_id,
                "model_type": model_type,
                "hyperparameters": hyperparameters or {},
                **kwargs
            }
        )
    
    async def optimize(
        self,
        model_id: str,
        optimization_target: str = "latency",
        **kwargs
    ) -> Dict[str, Any]:
        """Optimize model performance"""
        
        return await self.parent.request(
            "POST",
            "/v1/ai-hub/optimize",
            json={
                "model_id": model_id,
                "target": optimization_target,
                **kwargs
            }
        )


# Synchronous wrapper for blocking code
class HermesClientSync:
    """Synchronous wrapper around async HermesClient"""
    
    def __init__(self, config: Optional[SDKConfig] = None):
        self.config = config or SDKConfig.from_env()
        self._loop = None
        self._client = None
    
    def _ensure_loop(self):
        """Ensure event loop exists"""
        try:
            self._loop = asyncio.get_event_loop()
        except RuntimeError:
            self._loop = asyncio.new_event_loop()
            asyncio.set_event_loop(self._loop)
    
    def __enter__(self):
        self._ensure_loop()
        self._client = HermesClient(self.config)
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        if self._client:
            self._loop.run_until_complete(self._client.close())
    
    def llm_complete(self, provider: str, model: str, prompt: str, **kwargs) -> Dict[str, Any]:
        """Blocking LLM completion"""
        self._ensure_loop()
        if not self._client:
            self._client = HermesClient(self.config)
        
        return self._loop.run_until_complete(
            self._client.llm.complete(provider, model, prompt, **kwargs)
        )
    
    def task_execute(self, task_name: str, **kwargs) -> Dict[str, Any]:
        """Blocking task execution"""
        self._ensure_loop()
        if not self._client:
            self._client = HermesClient(self.config)
        
        return self._loop.run_until_complete(
            self._client.tasks.execute(task_name, **kwargs)
        )


__all__ = [
    "HermesClient",
    "HermesClientSync",
    "HermesException",
    "SDKConfig",
    "ErrorCode",
    "CircuitBreaker",
    "RetryPolicy",
]

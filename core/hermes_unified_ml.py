# hermes_unified_ml.py
# Unified ML orchestrator for Hermes: C++, C#, Python backends

import time
import numpy as np
from core.hermes_ml_algorithms import MLAlgorithms
from core.hermes_cpp_ml_algorithms import HermesCppML
from core.hermes_csharp_learning import HermesCSharpML

class UnifiedML:
    def __init__(self):
        self.backends = {
            'python': MLAlgorithms(),
            'cpp': HermesCppML(),
            'csharp': HermesCSharpML(),
        }
        self.best_backend = {}

    def _benchmark(self, func_dict, *args, repeat=3):
        times = {}
        for name, func in func_dict.items():
            try:
                start = time.time()
                for _ in range(repeat):
                    func(*args)
                times[name] = (time.time() - start) / repeat
            except Exception:
                times[name] = float('inf')
        return min(times, key=times.get)

    def mean(self, data):
        funcs = {
            'cpp': lambda d: self.backends['cpp'].mean(d),
            'csharp': lambda d: self.backends['csharp'].mean(d),
            'python': lambda d: np.mean(d),
        }
        if 'mean' not in self.best_backend:
            self.best_backend['mean'] = self._benchmark(funcs, data)
        return funcs[self.best_backend['mean']](data)

    def variance(self, data):
        funcs = {
            'cpp': lambda d: self.backends['cpp'].variance(d),
            'csharp': lambda d: self.backends['csharp'].variance(d),
            'python': lambda d: np.var(d),
        }
        if 'variance' not in self.best_backend:
            self.best_backend['variance'] = self._benchmark(funcs, data)
        return funcs[self.best_backend['variance']](data)

    def kmeans(self, data, n_clusters=3):
        funcs = {
            'cpp': lambda d, n: self.backends['cpp'].kmeans_stub(d, n),
            'csharp': lambda d, n: self.backends['csharp'].kmeans_stub(d, n),
            'python': lambda d, n: MLAlgorithms().kmeans(np.array(d).reshape(-1, 1), n),
        }
        if 'kmeans' not in self.best_backend:
            self.best_backend['kmeans'] = self._benchmark(funcs, data, n_clusters)
        return funcs[self.best_backend['kmeans']](data, n_clusters)

    def linear_regression(self, X, y):
        funcs = {
            'cpp': lambda X, y: self.backends['cpp'].linear_regression_stub(X, y),
            'csharp': lambda X, y: self.backends['csharp'].linear_regression_stub(X, y),
            'python': lambda X, y: MLAlgorithms().linear_regression(np.array(X).reshape(-1, 1), y),
        }
        if 'linear_regression' not in self.best_backend:
            self.best_backend['linear_regression'] = self._benchmark(funcs, X, y)
        return funcs[self.best_backend['linear_regression']](X, y)

    def decision_tree(self, X, y):
        funcs = {
            'cpp': lambda X, y: self.backends['cpp'].decision_tree_stub(X, y),
            'csharp': lambda X, y: self.backends['csharp'].decision_tree_stub(X, y),
            'python': lambda X, y: MLAlgorithms().decision_tree(np.array(X).reshape(-1, 1), y),
        }
        if 'decision_tree' not in self.best_backend:
            self.best_backend['decision_tree'] = self._benchmark(funcs, X, y)
        return funcs[self.best_backend['decision_tree']](X, y)

# Example usage:
# uml = UnifiedML()
# print(uml.mean([1.0, 2.0, 3.0]))

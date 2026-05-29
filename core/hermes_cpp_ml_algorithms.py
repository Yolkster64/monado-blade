# hermes_cpp_ml_algorithms.py
# Python bindings for Hermes C++ ML algorithms

import ctypes
import numpy as np
import os

DLL_PATH = os.path.join(os.path.dirname(__file__), 'hermes_cpp_ml_algorithms.dll')

class HermesCppML:
    def __init__(self):
        self.lib = ctypes.CDLL(DLL_PATH)
        self.lib.mean.restype = ctypes.c_double
        self.lib.mean.argtypes = [np.ctypeslib.ndpointer(dtype=np.float64), ctypes.c_int]
        self.lib.variance.restype = ctypes.c_double
        self.lib.variance.argtypes = [np.ctypeslib.ndpointer(dtype=np.float64), ctypes.c_int]
        self.lib.kmeans_stub.restype = ctypes.c_int
        self.lib.kmeans_stub.argtypes = [np.ctypeslib.ndpointer(dtype=np.float64), ctypes.c_int, ctypes.c_int]
        self.lib.linear_regression_stub.restype = ctypes.c_double
        self.lib.linear_regression_stub.argtypes = [np.ctypeslib.ndpointer(dtype=np.float64), np.ctypeslib.ndpointer(dtype=np.float64), ctypes.c_int]
        self.lib.decision_tree_stub.restype = ctypes.c_int
        self.lib.decision_tree_stub.argtypes = [np.ctypeslib.ndpointer(dtype=np.float64), np.ctypeslib.ndpointer(dtype=np.float64), ctypes.c_int]

    def mean(self, data):
        arr = np.ascontiguousarray(data, dtype=np.float64)
        return self.lib.mean(arr, len(arr))

    def variance(self, data):
        arr = np.ascontiguousarray(data, dtype=np.float64)
        return self.lib.variance(arr, len(arr))

    def kmeans_stub(self, data, n_clusters):
        arr = np.ascontiguousarray(data, dtype=np.float64)
        return self.lib.kmeans_stub(arr, len(arr), n_clusters)

    def linear_regression_stub(self, X, y):
        arr_X = np.ascontiguousarray(X, dtype=np.float64)
        arr_y = np.ascontiguousarray(y, dtype=np.float64)
        return self.lib.linear_regression_stub(arr_X, arr_y, len(arr_y))

    def decision_tree_stub(self, X, y):
        arr_X = np.ascontiguousarray(X, dtype=np.float64)
        arr_y = np.ascontiguousarray(y, dtype=np.float64)
        return self.lib.decision_tree_stub(arr_X, arr_y, len(arr_y))

# Example usage:
# cppml = HermesCppML()
# print(cppml.mean([1.0, 2.0, 3.0]))

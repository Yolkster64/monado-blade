# hermes_csharp_learning.py
# Python bindings for Hermes C# learning module
import ctypes
import numpy as np
import os

DLL_PATH = os.path.join(os.path.dirname(__file__), 'HermesCSharpLearning.dll')

class HermesCSharpML:
    def __init__(self):
        self.lib = ctypes.CDLL(DLL_PATH)
        self.lib.Mean.restype = ctypes.c_double
        self.lib.Mean.argtypes = [np.ctypeslib.ndpointer(dtype=np.float64), ctypes.c_int]
        self.lib.Variance.restype = ctypes.c_double
        self.lib.Variance.argtypes = [np.ctypeslib.ndpointer(dtype=np.float64), ctypes.c_int]
        self.lib.KMeansStub.restype = ctypes.c_int
        self.lib.KMeansStub.argtypes = [np.ctypeslib.ndpointer(dtype=np.float64), ctypes.c_int, ctypes.c_int]
        self.lib.LinearRegressionStub.restype = ctypes.c_double
        self.lib.LinearRegressionStub.argtypes = [np.ctypeslib.ndpointer(dtype=np.float64), np.ctypeslib.ndpointer(dtype=np.float64), ctypes.c_int]
        self.lib.DecisionTreeStub.restype = ctypes.c_int
        self.lib.DecisionTreeStub.argtypes = [np.ctypeslib.ndpointer(dtype=np.float64), np.ctypeslib.ndpointer(dtype=np.float64), ctypes.c_int]

    def mean(self, data):
        arr = np.ascontiguousarray(data, dtype=np.float64)
        return self.lib.Mean(arr, len(arr))

    def variance(self, data):
        arr = np.ascontiguousarray(data, dtype=np.float64)
        return self.lib.Variance(arr, len(arr))

    def kmeans_stub(self, data, n_clusters):
        arr = np.ascontiguousarray(data, dtype=np.float64)
        return self.lib.KMeansStub(arr, len(arr), n_clusters)

    def linear_regression_stub(self, X, y):
        arr_X = np.ascontiguousarray(X, dtype=np.float64)
        arr_y = np.ascontiguousarray(y, dtype=np.float64)
        return self.lib.LinearRegressionStub(arr_X, arr_y, len(arr_y))

    def decision_tree_stub(self, X, y):
        arr_X = np.ascontiguousarray(X, dtype=np.float64)
        arr_y = np.ascontiguousarray(y, dtype=np.float64)
        return self.lib.DecisionTreeStub(arr_X, arr_y, len(arr_y))

# Example usage:
# csharp = HermesCSharpLearning()
# result = csharp.train_skill([1.0, 2.0, 3.0])
# print(result)

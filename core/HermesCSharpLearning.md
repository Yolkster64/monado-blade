# Hermes C# Learning & ML Algorithms

This module provides a C# DLL with multiple ML algorithms for Hermes agents, callable from Python via ctypes. Implemented algorithms:
- Mean, Variance
- KMeans Clustering (stub)
- Linear Regression (stub)
- Decision Trees (stub)

Planned:
- SVM
- Neural Network (simple MLP)

### C# Function Signatures
- double Mean(double[] data, int len)
- double Variance(double[] data, int len)
- int KMeansStub(double[] data, int len, int nClusters)
- double LinearRegressionStub(double[] X, double[] y, int len)
- int DecisionTreeStub(double[] X, double[] y, int len)

Each function is exported for Python bindings.

---

## Next Steps
- Implement full algorithms for KMeans, Linear Regression, Decision Trees
- Build DLL and test Python interop
- Add usage examples and benchmarks

# hermes_ml_algorithms.py
# Additional ML algorithms for Hermes agents

import numpy as np
from sklearn.cluster import KMeans
from sklearn.linear_model import LinearRegression
from sklearn.tree import DecisionTreeClassifier
from sklearn.neural_network import MLPClassifier

class MLAlgorithms:
    def kmeans(self, data, n_clusters=3):
        return KMeans(n_clusters=n_clusters).fit(data)
    def linear_regression(self, X, y):
        return LinearRegression().fit(X, y)
    def decision_tree(self, X, y):
        return DecisionTreeClassifier().fit(X, y)
    def neural_net(self, X, y):
        return MLPClassifier().fit(X, y)

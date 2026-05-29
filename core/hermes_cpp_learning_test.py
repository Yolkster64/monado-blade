# hermes_cpp_learning_test.py
# Test for Hermes C++ learning integration

from hermes_cpp_learning import HermesCppLearning
import numpy as np

def test_train_skill():
    cpp = HermesCppLearning()
    data = np.array([1.0, 2.0, 3.0])
    result = cpp.train_skill(data)
    print(f"Result: {result}")

if __name__ == "__main__":
    test_train_skill()

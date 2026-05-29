# hermes_hybrid_training_test.py
# Test for hybrid training module

from hermes_hybrid_training import HybridTrainer

class DummyTrainer:
    def train(self, data):
        return sum(data)

if __name__ == "__main__":
    agent = object()
    local = DummyTrainer()
    cloud = DummyTrainer()
    hybrid = HybridTrainer(agent, local, cloud)
    data = [1, 2, 3]
    print('Local:', hybrid.train(data, method='local'))
    print('Cloud:', hybrid.train(data, method='cloud'))
    print('Ensemble:', hybrid.train(data, method='ensemble'))

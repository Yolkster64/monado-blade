# hermes_hybrid_training.py
# Hybrid training for Hermes agents

class HybridTrainer:
    def __init__(self, agent, local_trainer, cloud_trainer=None):
        self.agent = agent
        self.local_trainer = local_trainer
        self.cloud_trainer = cloud_trainer

    def train(self, data, method='auto'):
        if method == 'local' or (method == 'auto' and not self.cloud_trainer):
            return self.local_trainer.train(data)
        elif method == 'cloud' and self.cloud_trainer:
            return self.cloud_trainer.train(data)
        elif method == 'ensemble' and self.cloud_trainer:
            local_result = self.local_trainer.train(data)
            cloud_result = self.cloud_trainer.train(data)
            return self._combine_results(local_result, cloud_result)
        else:
            raise ValueError('Invalid training method or missing trainer')

    def _combine_results(self, local_result, cloud_result):
        # Simple ensemble: average results
        return (local_result + cloud_result) / 2

# Example usage:
# hybrid = HybridTrainer(agent, local_trainer, cloud_trainer)
# result = hybrid.train(data, method='ensemble')

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.AI
{
    /// <summary>
    /// Predicts system performance metrics and identifies potential issues before they occur.
    /// </summary>
    public class PerformancePredictor
    {
        private readonly List<PerformanceSnapshot> _history = new();
        private readonly Dictionary<string, PredictionModel> _models = new();
        private readonly int _minHistorySize = 50;
        private bool _isEnabled = true;

        public class PerformanceSnapshot
        {
            public DateTime Timestamp { get; set; }
            public double CpuUsage { get; set; }
            public double MemoryUsage { get; set; }
            public double DiskIoUsage { get; set; }
            public double NetworkUsage { get; set; }
            public double AverageResponseTime { get; set; }
            public int ThroughputRps { get; set; }
            public int ErrorCount { get; set; }
        }

        public class PredictionModel
        {
            public string MetricName { get; set; } = string.Empty;
            public double Intercept { get; set; }
            public List<double> Coefficients { get; set; } = new();
            public double RSquared { get; set; }
            public DateTime TrainedAt { get; set; }
            public int DataPointsUsed { get; set; }
        }

        public class Prediction
        {
            public string MetricName { get; set; } = string.Empty;
            public double PredictedValue { get; set; }
            public double Confidence { get; set; }
            public DateTime PredictionTime { get; set; }
            public double UpperBound { get; set; }
            public double LowerBound { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        public void RecordSnapshot(PerformanceSnapshot snapshot)
        {
            if (!_isEnabled) return;

            lock (_history)
            {
                _history.Add(snapshot);
                if (_history.Count > 1000)
                    _history.RemoveAt(0);
            }
        }

        public async Task TrainModels()
        {
            if (!_isEnabled || _history.Count < _minHistorySize)
                return;

            await Task.Run(() =>
            {
                lock (_history)
                {
                    TrainLoadPredictionModel();
                    TrainResponseTimeModel();
                    TrainErrorRateModel();
                    TrainResourceUsageModel();
                }
            });
        }

        private void TrainLoadPredictionModel()
        {
            var model = new PredictionModel { MetricName = "LoadPrediction", TrainedAt = DateTime.UtcNow };
            var throughputs = _history.Select(s => (double)s.ThroughputRps).ToList();
            var cpuValues = _history.Select(s => s.CpuUsage).ToList();
            var memoryValues = _history.Select(s => s.MemoryUsage).ToList();

            if (throughputs.Count > 10)
            {
                var trend = CalculateLinearTrend(throughputs);
                model.Intercept = trend.Intercept;
                model.Coefficients.Add(trend.Slope);
                model.RSquared = CalculateRSquared(throughputs);
                model.DataPointsUsed = throughputs.Count;
                _models["LoadPrediction"] = model;
            }
        }

        private void TrainResponseTimeModel()
        {
            var model = new PredictionModel { MetricName = "ResponseTime", TrainedAt = DateTime.UtcNow };
            var responseTimes = _history.Select(s => s.AverageResponseTime).ToList();
            var loads = _history.Select(s => (double)s.ThroughputRps).ToList();

            if (responseTimes.Count > 10)
            {
                var correlation = CalculateCorrelation(responseTimes, loads);
                model.Intercept = responseTimes.Average();
                model.Coefficients.Add(correlation);
                model.RSquared = Math.Pow(correlation, 2);
                model.DataPointsUsed = responseTimes.Count;
                _models["ResponseTime"] = model;
            }
        }

        private void TrainErrorRateModel()
        {
            var model = new PredictionModel { MetricName = "ErrorRate", TrainedAt = DateTime.UtcNow };
            var errors = _history.Select(s => (double)s.ErrorCount).ToList();
            var cpus = _history.Select(s => s.CpuUsage).ToList();

            if (errors.Count > 10)
            {
                var correlation = CalculateCorrelation(errors, cpus);
                model.Intercept = errors.Average();
                model.Coefficients.Add(correlation);
                model.RSquared = Math.Pow(correlation, 2);
                model.DataPointsUsed = errors.Count;
                _models["ErrorRate"] = model;
            }
        }

        private void TrainResourceUsageModel()
        {
            var model = new PredictionModel { MetricName = "ResourceUsage", TrainedAt = DateTime.UtcNow };
            var cpuValues = _history.Select(s => s.CpuUsage).ToList();
            var memoryValues = _history.Select(s => s.MemoryUsage).ToList();

            if (cpuValues.Count > 10)
            {
                var correlation = CalculateCorrelation(cpuValues, memoryValues);
                model.Intercept = cpuValues.Average();
                model.Coefficients.Add(correlation);
                model.RSquared = Math.Pow(correlation, 2);
                model.DataPointsUsed = cpuValues.Count;
                _models["ResourceUsage"] = model;
            }
        }

        public List<Prediction> PredictNext(int minutesAhead = 5)
        {
            var predictions = new List<Prediction>();

            if (!_isEnabled || _history.Count < _minHistorySize)
                return predictions;

            lock (_history)
            {
                var lastSnapshot = _history.Last();

                if (_models.TryGetValue("LoadPrediction", out var loadModel))
                {
                    var loadPred = PredictValue(loadModel, minutesAhead);
                    predictions.Add(new Prediction
                    {
                        MetricName = "Throughput (RPS)",
                        PredictedValue = Math.Max(0, loadPred),
                        Confidence = loadModel.RSquared,
                        PredictionTime = DateTime.UtcNow.AddMinutes(minutesAhead),
                        UpperBound = loadPred * 1.2,
                        LowerBound = Math.Max(0, loadPred * 0.8),
                        Status = loadPred > lastSnapshot.ThroughputRps ? "Increasing Load" : "Decreasing Load"
                    });
                }

                if (_models.TryGetValue("ResponseTime", out var rtModel))
                {
                    var rtPred = PredictValue(rtModel, minutesAhead);
                    predictions.Add(new Prediction
                    {
                        MetricName = "Response Time (ms)",
                        PredictedValue = Math.Max(0, rtPred),
                        Confidence = rtModel.RSquared,
                        PredictionTime = DateTime.UtcNow.AddMinutes(minutesAhead),
                        UpperBound = rtPred * 1.3,
                        LowerBound = Math.Max(0, rtPred * 0.7),
                        Status = rtPred > lastSnapshot.AverageResponseTime * 1.1 ? "Degradation Predicted" : "Stable"
                    });
                }

                if (_models.TryGetValue("ErrorRate", out var errModel))
                {
                    var errPred = PredictValue(errModel, minutesAhead);
                    predictions.Add(new Prediction
                    {
                        MetricName = "Error Rate",
                        PredictedValue = Math.Max(0, errPred),
                        Confidence = errModel.RSquared,
                        PredictionTime = DateTime.UtcNow.AddMinutes(minutesAhead),
                        UpperBound = errPred * 1.5,
                        LowerBound = Math.Max(0, errPred * 0.5),
                        Status = errPred > lastSnapshot.ErrorCount ? "Errors Increasing" : "Normal"
                    });
                }
            }

            return predictions;
        }

        private double PredictValue(PredictionModel model, int minutesAhead)
        {
            var prediction = model.Intercept;

            foreach (var coef in model.Coefficients)
            {
                prediction += coef * (minutesAhead / 5.0);
            }

            return prediction;
        }

        public bool IsFailureLikely(double confidenceThreshold = 0.7)
        {
            if (_history.Count < _minHistorySize)
                return false;

            var predictions = PredictNext(5);
            var errorPred = predictions.FirstOrDefault(p => p.MetricName == "Error Rate");

            if (errorPred == null)
                return false;

            return errorPred.Confidence > confidenceThreshold && errorPred.PredictedValue > 10;
        }

        public List<string> GetEarlyWarnings()
        {
            var warnings = new List<string>();
            var predictions = PredictNext(10);

            var loadPred = predictions.FirstOrDefault(p => p.MetricName == "Throughput (RPS)");
            if (loadPred != null && loadPred.PredictedValue > 10000)
                warnings.Add("High throughput spike predicted - consider scaling resources");

            var rtPred = predictions.FirstOrDefault(p => p.MetricName == "Response Time (ms)");
            if (rtPred != null && rtPred.PredictedValue > 500)
                warnings.Add("Response time degradation expected - check database queries");

            var errPred = predictions.FirstOrDefault(p => p.MetricName == "Error Rate");
            if (errPred != null && errPred.PredictedValue > 5)
                warnings.Add("Error rate increase predicted - review recent changes");

            return warnings;
        }

        private (double Intercept, double Slope) CalculateLinearTrend(List<double> values)
        {
            if (values.Count < 2)
                return (values.FirstOrDefault(), 0);

            var n = values.Count;
            var x = Enumerable.Range(0, n).Select(i => (double)i).ToList();
            var y = values;

            var xMean = x.Average();
            var yMean = y.Average();

            var slope = x.Zip(y).Sum(pair => (pair.First - xMean) * (pair.Second - yMean)) /
                       x.Sum(xi => Math.Pow(xi - xMean, 2));

            var intercept = yMean - slope * xMean;

            return (intercept, slope);
        }

        private double CalculateCorrelation(List<double> x, List<double> y)
        {
            if (x.Count != y.Count || x.Count == 0)
                return 0;

            var xMean = x.Average();
            var yMean = y.Average();

            var covariance = x.Zip(y).Sum(pair => (pair.First - xMean) * (pair.Second - yMean)) / x.Count;
            var xStdDev = Math.Sqrt(x.Sum(xi => Math.Pow(xi - xMean, 2)) / x.Count);
            var yStdDev = Math.Sqrt(y.Sum(yi => Math.Pow(yi - yMean, 2)) / y.Count);

            if (xStdDev == 0 || yStdDev == 0)
                return 0;

            return covariance / (xStdDev * yStdDev);
        }

        private double CalculateRSquared(List<double> values)
        {
            if (values.Count < 3)
                return 0;

            var trend = CalculateLinearTrend(values);
            var predictions = Enumerable.Range(0, values.Count)
                .Select(i => trend.Intercept + trend.Slope * i)
                .ToList();

            var mean = values.Average();
            var ssTotal = values.Sum(v => Math.Pow(v - mean, 2));
            var ssResidual = values.Zip(predictions).Sum(pair => Math.Pow(pair.First - pair.Second, 2));

            return 1 - (ssResidual / ssTotal);
        }

        public void EnableDisable(bool enabled) => _isEnabled = enabled;

        public int GetHistorySize() => _history.Count;
    }
}

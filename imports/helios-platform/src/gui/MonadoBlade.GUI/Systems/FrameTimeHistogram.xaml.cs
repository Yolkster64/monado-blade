using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MonadoBlade.GUI.Systems
{
    /// <summary>
    /// Frame time histogram for analyzing frame timing distribution and percentiles.
    /// </summary>
    public partial class FrameTimeHistogram : UserControl
    {
        private List<double> frameTimesMs = new List<double>();
        private int[] histogram = new int[50]; // 50ms bins

        public FrameTimeHistogram()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Add a frame time sample in milliseconds.
        /// </summary>
        public void AddFrameTime(double frameTimeMs)
        {
            frameTimesMs.Add(frameTimeMs);
            
            // Keep last 300 samples (5 seconds at 60 FPS)
            if (frameTimesMs.Count > 300)
                frameTimesMs.RemoveAt(0);

            // Add to histogram bin
            int bin = Math.Min(49, (int)(frameTimeMs / 1.0)); // 1ms bins
            if (bin >= 0 && bin < 50)
                histogram[bin]++;

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            Dispatcher.Invoke(() =>
            {
                if (frameTimesMs.Count == 0) return;

                // Calculate percentiles
                var sorted = frameTimesMs.OrderBy(x => x).ToList();
                double p50 = GetPercentile(sorted, 0.50);
                double p95 = GetPercentile(sorted, 0.95);
                double p99 = GetPercentile(sorted, 0.99);
                double max = sorted[sorted.Count - 1];
                double avg = sorted.Average();

                // Update percentile displays
                P50Text.Text = p50.ToString("F2");
                P95Text.Text = p95.ToString("F2");
                P99Text.Text = p99.ToString("F2");
                MaxText.Text = max.ToString("F2");
                AvgFrameTimeText.Text = $"{avg:F2} ms";

                // Count outliers (> P99 + 5ms)
                double outlierThreshold = p99 + 5;
                int outliers = frameTimesMs.Count(x => x > outlierThreshold);
                OutliersText.Text = outliers.ToString();

                // Detect frame locking (when frame times are very consistent)
                bool isLocked = DetectFrameLocking(sorted);
                LockedText.Text = isLocked ? "Yes" : "No";
                LockedText.Foreground = new SolidColorBrush(isLocked ? 
                    Color.FromRgb(46, 204, 113) : Color.FromRgb(52, 152, 219));

                // Draw histogram
                DrawHistogram();
            });
        }

        private double GetPercentile(List<double> sorted, double percentile)
        {
            if (sorted.Count == 0) return 0;
            int index = (int)((sorted.Count - 1) * percentile);
            return sorted[Math.Max(0, Math.Min(sorted.Count - 1, index))];
        }

        private bool DetectFrameLocking(List<double> sorted)
        {
            if (sorted.Count < 30) return false;

            // Check if standard deviation is very low (frames are locked)
            double avg = sorted.Average();
            double variance = sorted.Sum(x => Math.Pow(x - avg, 2)) / sorted.Count;
            double stdDev = Math.Sqrt(variance);

            // If std dev is less than 5% of average, frame time is locked
            return stdDev < (avg * 0.05);
        }

        private void DrawHistogram()
        {
            HistogramCanvas.Children.Clear();

            double canvasWidth = HistogramCanvas.ActualWidth;
            double canvasHeight = HistogramCanvas.ActualHeight;
            double barWidth = canvasWidth / 50;

            if (barWidth <= 0) return;

            int maxCount = histogram.Max();
            if (maxCount == 0) maxCount = 1;

            for (int i = 0; i < 50; i++)
            {
                double barHeight = (histogram[i] / (double)maxCount) * canvasHeight;
                double x = i * barWidth;
                double y = canvasHeight - barHeight;

                // Color based on frame time range
                Color barColor = i < 16 ? Colors.Lime :      // 0-16ms (60fps)
                                i < 33 ? Colors.Yellow :     // 16-33ms (30fps)
                                Colors.Red;                  // 33+ms (slow)

                var rect = new Rectangle
                {
                    Width = Math.Max(1, barWidth - 1),
                    Height = barHeight,
                    Fill = new SolidColorBrush(Color.FromArgb(200, barColor.R, barColor.G, barColor.B)),
                    Stroke = new SolidColorBrush(barColor),
                    StrokeThickness = 0.5
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                HistogramCanvas.Children.Add(rect);
            }

            // Draw reference lines
            // 60 FPS line (16.67ms)
            double line60fps = (16.67 / 50.0) * canvasWidth;
            HistogramCanvas.Children.Add(new Line
            {
                X1 = line60fps, Y1 = 0, X2 = line60fps, Y2 = canvasHeight,
                Stroke = new SolidColorBrush(Color.FromArgb(100, 46, 204, 113)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 2, 2 }
            });

            // 30 FPS line (33.33ms)
            double line30fps = (33.33 / 50.0) * canvasWidth;
            HistogramCanvas.Children.Add(new Line
            {
                X1 = line30fps, Y1 = 0, X2 = line30fps, Y2 = canvasHeight,
                Stroke = new SolidColorBrush(Color.FromArgb(100, 231, 76, 60)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 2, 2 }
            });
        }

        public List<double> GetFrameTimes() => new List<double>(frameTimesMs);
        public double GetAverageFrameTime() => frameTimesMs.Count > 0 ? frameTimesMs.Average() : 0;
        public double GetP95FrameTime() => GetPercentile(frameTimesMs.OrderBy(x => x).ToList(), 0.95);
    }
}

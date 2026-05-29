using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MonadoBlade.GUI.Systems
{
    /// <summary>
    /// Memory Profiler UI component for monitoring memory usage and GC statistics.
    /// </summary>
    public partial class MemoryProfiler : UserControl
    {
        private List<long> memoryHistory = new List<long>();
        private long peakMemory;
        private long lastGcCollections;

        public MemoryProfiler()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Updates memory profiler - call periodically (e.g., every 500ms).
        /// </summary>
        public void UpdateMemory(long workingSetMB, long managedMB, long nativeMB, long gcCollections)
        {
            Dispatcher.Invoke(() =>
            {
                // Update text displays
                WorkingSetText.Text = $"{workingSetMB} MB";
                ManagedText.Text = $"{managedMB} MB";
                NativeText.Text = $"{nativeMB} MB";
                GcCollectionsText.Text = gcCollections.ToString();

                // Track peak memory
                if (workingSetMB > peakMemory)
                    peakMemory = workingSetMB;
                PeakMemoryText.Text = $"{peakMemory} MB";

                // Add to history
                memoryHistory.Add(workingSetMB);
                if (memoryHistory.Count > 60)
                    memoryHistory.RemoveAt(0);

                // Update distribution bar
                long total = workingSetMB;
                if (total > 0)
                {
                    ManagedColumn.Width = new GridLength(managedMB, GridUnitType.Star);
                    NativeColumn.Width = new GridLength(nativeMB, GridUnitType.Star);
                    FreeColumn.Width = new GridLength(Math.Max(0, 1024 - total), GridUnitType.Star);
                }

                // Update graph
                DrawMemoryHistoryGraph();

                // Check alerts
                UpdateAlerts(workingSetMB);
            });
        }

        private void DrawMemoryHistoryGraph()
        {
            MemoryHistoryCanvas.Children.Clear();

            if (memoryHistory.Count < 2) return;

            double canvasWidth = MemoryHistoryCanvas.ActualWidth;
            double canvasHeight = MemoryHistoryCanvas.ActualHeight;
            double maxMemory = 1500; // 1.5 GB max display
            double pixelPerMb = canvasHeight / maxMemory;
            double pixelPerSample = canvasWidth / memoryHistory.Count;

            // Draw grid lines
            var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(50, 100, 100, 100)), 1);
            for (int i = 0; i <= 6; i++)
            {
                double y = (canvasHeight / 6) * i;
                MemoryHistoryCanvas.Children.Add(new Line
                {
                    X1 = 0, Y1 = y, X2 = canvasWidth, Y2 = y,
                    Stroke = gridPen.Brush,
                    StrokeThickness = gridPen.Thickness
                });
            }

            // Draw memory line graph
            for (int i = 0; i < memoryHistory.Count - 1; i++)
            {
                double x1 = pixelPerSample * i;
                double y1 = canvasHeight - (memoryHistory[i] * pixelPerMb);
                double x2 = pixelPerSample * (i + 1);
                double y2 = canvasHeight - (memoryHistory[i + 1] * pixelPerMb);

                var color = memoryHistory[i + 1] > 900 ? Colors.Red :
                           memoryHistory[i + 1] > 700 ? Colors.Orange :
                           memoryHistory[i + 1] > 500 ? Colors.Yellow : Colors.Lime;

                var line = new Line
                {
                    X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = 2
                };
                MemoryHistoryCanvas.Children.Add(line);
            }

            // Draw current point
            if (memoryHistory.Count > 0)
            {
                double currentX = canvasWidth - pixelPerSample;
                double currentY = canvasHeight - (memoryHistory[memoryHistory.Count - 1] * pixelPerMb);
                var ellipse = new Ellipse
                {
                    Width = 6, Height = 6,
                    Fill = new SolidColorBrush(Colors.Cyan),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(ellipse, currentX - 3);
                Canvas.SetTop(ellipse, currentY - 3);
                MemoryHistoryCanvas.Children.Add(ellipse);
            }
        }

        private void UpdateAlerts(long workingSetMB)
        {
            if (workingSetMB > 1024) // Over 1GB
            {
                AlertBorder.Background = new SolidColorBrush(Color.FromArgb(40, 231, 76, 60));
                AlertBorder.Visibility = Visibility.Visible;
                AlertText.Text = $"⚠️ High memory usage: {workingSetMB} MB";
            }
            else
            {
                AlertBorder.Visibility = Visibility.Collapsed;
            }
        }

        public long GetCurrentMemory() => memoryHistory.Count > 0 ? memoryHistory[memoryHistory.Count - 1] : 0;
        public long GetPeakMemory() => peakMemory;
        public List<long> GetMemoryHistory() => new List<long>(memoryHistory);
    }
}

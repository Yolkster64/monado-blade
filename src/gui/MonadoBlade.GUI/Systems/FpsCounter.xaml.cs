using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MonadoBlade.GUI.Systems
{
    /// <summary>
    /// FPS Counter UI component for real-time frame rate monitoring.
    /// </summary>
    public partial class FpsCounter : UserControl
    {
        private Stopwatch frameTimer = new Stopwatch();
        private List<double> fpsHistory = new List<double>();
        private double currentFps;
        private int frameCount;
        private DateTime lastSecond;

        public FpsCounter()
        {
            InitializeComponent();
            lastSecond = DateTime.Now;
            frameTimer.Start();
        }

        /// <summary>
        /// Updates FPS counter - call this once per frame.
        /// </summary>
        public void UpdateFrame()
        {
            frameCount++;
            double elapsedMs = frameTimer.Elapsed.TotalMilliseconds;

            // Check if one second has passed
            if (DateTime.Now.Subtract(lastSecond).TotalMilliseconds >= 1000)
            {
                currentFps = frameCount;
                fpsHistory.Add(currentFps);

                // Keep last 60 samples
                if (fpsHistory.Count > 60)
                    fpsHistory.RemoveAt(0);

                UpdateDisplay();
                DrawHistoryGraph();

                frameCount = 0;
                lastSecond = DateTime.Now;
                frameTimer.Restart();
            }
        }

        private void UpdateDisplay()
        {
            Dispatcher.Invoke(() =>
            {
                // Current FPS
                CurrentFpsText.Text = currentFps.ToString("F1");

                // Average FPS
                double avgFps = 0;
                foreach (var fps in fpsHistory)
                    avgFps += fps;
                avgFps /= fpsHistory.Count;
                AvgFpsText.Text = avgFps.ToString("F1");

                // Min/Max FPS
                double minFps = double.MaxValue, maxFps = 0;
                foreach (var fps in fpsHistory)
                {
                    if (fps < minFps) minFps = fps;
                    if (fps > maxFps) maxFps = fps;
                }
                MinMaxFpsText.Text = $"{minFps:F0} / {maxFps:F0}";

                // Frame time in milliseconds
                double frameTimeMs = 1000.0 / (currentFps > 0 ? currentFps : 1);
                FrameTimeText.Text = frameTimeMs.ToString("F2");
            });
        }

        private void DrawHistoryGraph()
        {
            Dispatcher.Invoke(() =>
            {
                FpsHistoryCanvas.Children.Clear();

                if (fpsHistory.Count < 2) return;

                double canvasWidth = FpsHistoryCanvas.ActualWidth;
                double canvasHeight = FpsHistoryCanvas.ActualHeight;
                double maxFps = 120;
                double pixelPerFps = canvasHeight / maxFps;
                double pixelPerFrame = canvasWidth / fpsHistory.Count;

                // Draw grid lines
                var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(50, 100, 100, 100)), 1);
                for (int i = 0; i <= 6; i++)
                {
                    double y = (canvasHeight / 6) * i;
                    FpsHistoryCanvas.Children.Add(new Line
                    {
                        X1 = 0, Y1 = y, X2 = canvasWidth, Y2 = y,
                        Stroke = gridPen.Brush,
                        StrokeThickness = gridPen.Thickness
                    });
                }

                // Draw FPS line graph
                for (int i = 0; i < fpsHistory.Count - 1; i++)
                {
                    double x1 = pixelPerFrame * i;
                    double y1 = canvasHeight - (fpsHistory[i] * pixelPerFps);
                    double x2 = pixelPerFrame * (i + 1);
                    double y2 = canvasHeight - (fpsHistory[i + 1] * pixelPerFps);

                    var color = fpsHistory[i + 1] >= 60 ? Colors.Lime : 
                               fpsHistory[i + 1] >= 30 ? Colors.Yellow : Colors.Red;

                    var line = new Line
                    {
                        X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                        Stroke = new SolidColorBrush(color),
                        StrokeThickness = 2
                    };
                    FpsHistoryCanvas.Children.Add(line);
                }

                // Draw current FPS point
                double currentX = canvasWidth - pixelPerFrame;
                double currentY = canvasHeight - (currentFps * pixelPerFps);
                var ellipse = new Ellipse
                {
                    Width = 6, Height = 6,
                    Fill = new SolidColorBrush(Colors.Cyan),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(ellipse, currentX - 3);
                Canvas.SetTop(ellipse, currentY - 3);
                FpsHistoryCanvas.Children.Add(ellipse);
            });
        }

        public double GetCurrentFps() => currentFps;
        public List<double> GetFpsHistory() => new List<double>(fpsHistory);
        public double GetAverageFps()
        {
            if (fpsHistory.Count == 0) return 0;
            double sum = 0;
            foreach (var fps in fpsHistory) sum += fps;
            return sum / fpsHistory.Count;
        }
    }
}

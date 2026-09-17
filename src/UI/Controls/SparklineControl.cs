using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace IdeaMemoryManager.UI.Controls
{
    /// <summary>
    /// High-performance double-buffered micro sparkline displaying real-time memory fluctuations
    /// and visual cliff-drops after deep garbage collection.
    /// </summary>
    public class SparklineControl : Control
    {
        private readonly List<double> _points = new();
        private const int MaxPoints = 40;

        public SparklineControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.UserPaint | 
                     ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.ResizeRedraw, true);

            Height = 40;
            BackColor = Color.FromArgb(24, 25, 28);
        }

        public void AddDataPoint(double valueGb)
        {
            _points.Add(valueGb);
            if (_points.Count > MaxPoints)
            {
                _points.RemoveAt(0);
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = Width;
            int h = Height;

            // Draw soft border
            using var borderPen = new Pen(Color.FromArgb(45, 48, 54), 1);
            g.DrawRectangle(borderPen, 0, 0, w - 1, h - 1);

            if (_points.Count < 2) return;

            // Compute bounds
            double max = 0.5;
            foreach (var val in _points)
            {
                if (val > max) max = val;
            }
            max *= 1.15; // 15% headroom

            var pts = new PointF[_points.Count];
            float stepX = (float)(w - 12) / (MaxPoints - 1);
            float startX = 6 + (MaxPoints - _points.Count) * stepX;

            for (int i = 0; i < _points.Count; i++)
            {
                float x = startX + i * stepX;
                float ratio = (float)(_points[i] / max);
                if (ratio > 1f) ratio = 1f;
                float y = (h - 6) - ratio * (h - 12);
                pts[i] = new PointF(x, y);
            }

            // Fill area gradient
            using (var path = new GraphicsPath())
            {
                path.AddLines(pts);
                path.AddLine(pts[^1], new PointF(pts[^1].X, h - 2));
                path.AddLine(new PointF(pts[^1].X, h - 2), new PointF(pts[0].X, h - 2));
                path.CloseFigure();

                using var fillBrush = new LinearGradientBrush(
                    new PointF(0, 0), new PointF(0, h),
                    Color.FromArgb(50, 88, 166, 255),
                    Color.FromArgb(5, 88, 166, 255));
                g.FillPath(fillBrush, path);
            }

            // Draw line
            using var linePen = new Pen(Color.FromArgb(88, 166, 255), 1.8f);
            linePen.LineJoin = LineJoin.Round;
            g.DrawLines(linePen, pts);

            // Draw pulsing dot at the latest point
            PointF latest = pts[^1];
            using var dotBrush = new SolidBrush(Color.FromArgb(56, 239, 125));
            using var dotGlow = new SolidBrush(Color.FromArgb(60, 56, 239, 125));
            g.FillEllipse(dotGlow, latest.X - 4, latest.Y - 4, 8, 8);
            g.FillEllipse(dotBrush, latest.X - 2, latest.Y - 2, 4, 4);
        }
    }
}
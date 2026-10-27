using System.Drawing;
using System.Drawing.Drawing2D;

namespace BlackHoleSim;

// A small 2D interface: controls, simulation updates, and drawing.
public sealed class MainForm : Form
{
    private const float StepDistance = 0.08f;
    private const int StepsPerFrame = 7;
    private const float EscapeRadius = 30f;
    private const int MaxPointsPerRay = 3000;

    private readonly List<Photon> _photons = new();
    private readonly DrawingPanel _canvas = new() { Dock = DockStyle.Fill };
    private readonly TrackBar _massSlider = new()
    {
        Minimum = 5, Maximum = 20, Value = 10, Width = 130,
        TickStyle = TickStyle.None
    };
    private readonly Label _massLabel = new()
    {
        AutoSize = true, Margin = new Padding(5, 8, 5, 0)
    };
    private readonly Button _pauseButton = new() { Text = "Pause", AutoSize = true };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 30 };

    // In our units, "mass" means the Schwarzschild radius (event horizon).
    private float Mass => _massSlider.Value / 10f;

    public MainForm()
    {
        Text = "BlackHoleSim 2D";
        ClientSize = new Size(1050, 650);
        StartPosition = FormStartPosition.CenterScreen;

        var fireButton = new Button { Text = "Fire 19 rays", AutoSize = true };
        var clearButton = new Button { Text = "Clear", AutoSize = true };
        var instructions = new Label
        {
            Text = "Click the canvas to launch one ray to the right.",
            AutoSize = true,
            ForeColor = Color.Gainsboro,
            Margin = new Padding(16, 8, 0, 0)
        };

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 64,
            WrapContents = false,
            BackColor = Color.FromArgb(28, 31, 39),
            Padding = new Padding(10, 8, 10, 6)
        };
        foreach (Control control in new Control[]
                 { fireButton, clearButton, _pauseButton, _massSlider, _massLabel, instructions })
            toolbar.Controls.Add(control);

        _canvas.BackColor = Color.FromArgb(10, 14, 24);
        Controls.Add(_canvas);
        Controls.Add(toolbar);

        fireButton.Click += (_, _) => FireRays();
        clearButton.Click += (_, _) => { _photons.Clear(); _canvas.Invalidate(); };
        _pauseButton.Click += (_, _) => TogglePause();
        _massSlider.ValueChanged += (_, _) =>
        {
            UpdateMassLabel();
            FireRays(); // Restart: each ray should experience one fixed mass.
        };
        _canvas.MouseClick += (_, e) => AddRayAtMouse(e.Location);
        _canvas.Paint += DrawScene;
        _timer.Tick += (_, _) => UpdateSimulation();

        UpdateMassLabel();
        FireRays();
        _timer.Start();
    }

    private void UpdateMassLabel()
    {
        _massLabel.Text = $"Mass (r_s): {Mass:0.0}";
    }

    private void FireRays()
    {
        _photons.Clear();

        // The same initial direction, different vertical offsets.
        // Some photons will be captured; others will bend and escape.
        for (int i = -9; i <= 9; i++)
            _photons.Add(new Photon(-17f, i * 0.85f));

        _canvas.Invalidate();
    }

    private void AddRayAtMouse(Point screenPosition)
    {
        PointF point = ScreenToWorld(screenPosition);
        float radius = MathF.Sqrt(point.X * point.X + point.Y * point.Y);

        if (radius <= Mass || _photons.Count >= 70)
            return; // Can't launch from inside the horizon.

        _photons.Add(new Photon(point.X, point.Y));
        _canvas.Invalidate();
    }

    private void TogglePause()
    {
        _timer.Enabled = !_timer.Enabled;
        _pauseButton.Text = _timer.Enabled ? "Pause" : "Resume";
    }

    // One timer tick: move every active photon several small steps.
    private void UpdateSimulation()
    {
        foreach (Photon photon in _photons)
        {
            if (!photon.IsMoving)
                continue;

            for (int i = 0; i < StepsPerFrame; i++)
            {
                if (!Physics.Step(photon, Mass, StepDistance))
                {
                    photon.IsMoving = false;
                    break;
                }

                float radius = MathF.Sqrt(photon.X * photon.X + photon.Y * photon.Y);

                if (radius <= Mass || radius > EscapeRadius)
                {
                    photon.IsMoving = false;
                    break;
                }

                photon.Path.Add(new PointF(photon.X, photon.Y));
                if (photon.Path.Count >= MaxPointsPerRay)
                {
                    photon.IsMoving = false;
                    break;
                }
            }
        }

        _canvas.Invalidate();
    }

    // All drawing happens through regular WinForms Graphics methods.
    private void DrawScene(object? sender, PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        float scale = GetScale();

        using var rayPen = new Pen(Color.FromArgb(80, 210, 255), 1.8f);
        using var horizonPen = new Pen(Color.OrangeRed, 2f);
        using var photonBrush = new SolidBrush(Color.White);
        using var horizonBrush = new SolidBrush(Color.Black);

        foreach (Photon photon in _photons)
        {
            if (photon.Path.Count >= 2)
            {
                var points = new PointF[photon.Path.Count];
                for (int i = 0; i < points.Length; i++)
                    points[i] = WorldToScreen(photon.Path[i]);

                graphics.DrawLines(rayPen, points);
            }

            if (photon.IsMoving)
            {
                PointF head = WorldToScreen(new PointF(photon.X, photon.Y));
                graphics.FillEllipse(photonBrush, head.X - 2.5f, head.Y - 2.5f, 5f, 5f);
            }
        }

        // This circle is the EVENT HORIZON (r_s), not the larger
        // apparent black-hole shadow from the original 3D renderer.
        float r = Mass * scale;
        float cx = _canvas.ClientSize.Width / 2f;
        float cy = _canvas.ClientSize.Height / 2f;
        graphics.FillEllipse(horizonBrush, cx - r, cy - r, 2f * r, 2f * r);
        graphics.DrawEllipse(horizonPen, cx - r, cy - r, 2f * r, 2f * r);
    }

    // The centre of the panel represents (0, 0) in world coordinates.
    private float GetScale() => MathF.Min(
        _canvas.ClientSize.Width / 40f,
        _canvas.ClientSize.Height / 25f);

    private PointF WorldToScreen(PointF p)
    {
        float scale = GetScale();
        return new PointF(_canvas.Width / 2f + p.X * scale,
                          _canvas.Height / 2f - p.Y * scale);
    }

    private PointF ScreenToWorld(Point p)
    {
        float scale = GetScale();
        return new PointF((p.X - _canvas.Width / 2f) / scale,
                          (_canvas.Height / 2f - p.Y) / scale);
    }

    // A double-buffered panel avoids flickering as the paths are redrawn.
    private sealed class DrawingPanel : Panel
    {
        public DrawingPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }
}

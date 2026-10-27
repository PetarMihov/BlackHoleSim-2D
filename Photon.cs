using System.Drawing;

namespace BlackHoleSim;

// Stores one light ray's position and mathematical state.
internal sealed class Photon
{
    public float X;
    public float Y;

    // Polar coordinates used by the black-hole equation:
    // u = 1 / radius; w = du / d(angle).
    public float U;
    public float W;
    public float Angle;
    public float AngleDirection;

    public bool IsRadial;
    public bool IsMoving = true;

    // Every point visited by this photon is kept for drawing its path.
    public List<PointF> Path { get; } = new();

    public Photon(float x, float y)
    {
        X = x;
        Y = y;
        Path.Add(new PointF(x, y));

        // All photons start moving to the RIGHT: direction = (1, 0).
        float radius = MathF.Sqrt(x * x + y * y);
        U = 1f / radius;
        Angle = MathF.Atan2(y, x);

        // Components of the starting direction relative to the radius.
        float radialSpeed = x / radius;
        float angularSpeed = -y / radius;

        // For a purely radial ray, the spatial path is a straight line.
        IsRadial = MathF.Abs(angularSpeed) < 0.0001f;
        if (!IsRadial)
        {
            W = -radialSpeed / (radius * angularSpeed);
            AngleDirection = MathF.Sign(angularSpeed);
        }
    }
}

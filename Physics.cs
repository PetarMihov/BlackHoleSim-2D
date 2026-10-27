namespace BlackHoleSim;

// Only the mathematical update of a light ray lives in this file.
internal static class Physics
{
    // The Schwarzschild light-ray equation is:
    //     d²u/d(angle)² + u = 1.5 * mass * u²
    // with u = 1 / radius and mass = Schwarzschild radius (r_s).
    //
    // We write it as two first-order equations:
    //     du/d(angle) = w
    //     dw/d(angle) = 1.5 * mass * u² - u
    private static float Acceleration(float u, float mass)
    {
        return 1.5f * mass * u * u - u;
    }

    // Moves one photon a short distance. Returns false when its
    // mathematical position reaches infinity (u <= 0) or is invalid.
    public static bool Step(Photon photon, float mass, float distanceStep)
    {
        if (photon.IsRadial)
        {
            photon.X += distanceStep; // It was launched to the right.
            return true;
        }

        float u = photon.U;
        float w = photon.W;

        // Convert a small travel-distance step into an angular step.
        // The sign matters because rays above and below the hole
        // orbit in opposite directions.
        float h = photon.AngleDirection * distanceStep * u * u
                  / MathF.Sqrt(w * w + u * u);

        // Runge-Kutta 4 (RK4): four estimates of the next change.
        float k1u = w;
        float k1w = Acceleration(u, mass);

        float k2u = w + h * k1w / 2f;
        float k2w = Acceleration(u + h * k1u / 2f, mass);

        float k3u = w + h * k2w / 2f;
        float k3w = Acceleration(u + h * k2u / 2f, mass);

        float k4u = w + h * k3w;
        float k4w = Acceleration(u + h * k3u, mass);

        photon.U += h * (k1u + 2f * k2u + 2f * k3u + k4u) / 6f;
        photon.W += h * (k1w + 2f * k2w + 2f * k3w + k4w) / 6f;
        photon.Angle += h;

        if (!float.IsFinite(photon.U) || photon.U <= 0f)
            return false;

        // Convert polar position (radius, angle) back into x/y.
        float radius = 1f / photon.U;
        photon.X = radius * MathF.Cos(photon.Angle);
        photon.Y = radius * MathF.Sin(photon.Angle);

        return float.IsFinite(photon.X) && float.IsFinite(photon.Y);
    }
}

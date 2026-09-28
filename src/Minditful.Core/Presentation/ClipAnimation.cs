using Minditful.Core.Engine;

namespace Minditful.Core.Presentation;

/// <summary>Biến đổi của Milo tại 1 thời điểm. Gốc (OriginX, OriginY) tính theo tỉ lệ khung 150×150.</summary>
public readonly record struct MiloTransform(double Tx, double Ty, double Rot, double Sx, double Sy, double OriginX, double OriginY)
{
    public static MiloTransform Hidden => new(0, 175, 0, 1, 1, .5, .5);
}

/// <summary>Keyframes của từng clip — chép từ CSS "Milo clips (board 00 / 17)" của prototype.</summary>
public static class ClipAnimation
{
    private readonly record struct Key(double At, double Tx = 0, double Ty = 0, double Rot = 0, double Sx = 1, double Sy = 1);

    private sealed record Anim(double Duration, bool Loop, (double, double, double, double) Ease, double Ox, double Oy, Key[] Keys, bool Steps = false);

    private static readonly (double, double, double, double) EaseOut = (0, 0, .58, 1);
    private static readonly (double, double, double, double) EaseIn = (.42, 0, 1, 1);
    private static readonly (double, double, double, double) EaseInOut = (.42, 0, .58, 1);

    private static readonly Anim Hop = new(1.1, true, EaseOut, .5, 1,
        [new(0), new(.15, Ty: -16), new(.30, Sx: 1.06, Sy: .94), new(.40), new(1)]);

    private static Anim ClimbIn(double dur) => new(dur, false, (.3, .7, .4, 1), .5, .5,
        [new(0, Ty: 170), new(.55, Ty: -10), new(.75, Ty: 4), new(1)]);

    private static readonly Anim BigBounce = new(.5, false, EaseOut, .5, 1,
        [new(0), new(.35, Sx: 1.15, Sy: .9), new(.6, Sx: .95, Sy: 1.08), new(1)]);

    private static readonly Anim Wave = new(.7, true, EaseInOut, .5, .9,
        [new(0), new(.25, Rot: -6), new(.75, Rot: 6), new(1)]);

    private static Anim Bob(double dur) => new(dur, true, EaseInOut, .5, .5, [new(0), new(.5, Ty: -4), new(1)]);

    private static readonly Dictionary<Clip, Anim> Anims = new()
    {
        [Clip.HangPull] = new(3.4, false, EaseOut, .5, 0,
        [
            new(0, Ty: 160), new(.20, Ty: 92), new(.27, Ty: 96, Rot: -3), new(.34, Ty: 94, Rot: 3), new(.42, Ty: 95, Rot: -2),
            new(.53, Ty: 95), new(.63, Ty: 70), new(.67, Ty: 74), new(.79, Ty: 26), new(.83, Ty: 32), new(.93, Ty: -8), new(.97, Ty: 3), new(1),
        ]),
        [Clip.Hello] = Hop,
        [Clip.Celebrate] = Hop,
        [Clip.ClimbIn] = ClimbIn(1.2),
        [Clip.PeekIn] = ClimbIn(1.8),
        [Clip.JumpIn] = new(1.5, false, (.3, .6, .4, 1), .5, 1,
        [
            new(0, Tx: 260, Ty: 40, Rot: 25), new(.45, Tx: 90, Ty: -110, Rot: -12), new(.72, Ty: 6, Sx: 1.14, Sy: .86),
            new(.86, Ty: -6, Sx: .95, Sy: 1.06), new(1),
        ]),
        [Clip.StandUp] = BigBounce,
        [Clip.Stretch] = BigBounce,
        [Clip.Greet] = Wave,
        [Clip.Thanks] = Wave,
        [Clip.Reminder] = new(2.4, true, EaseInOut, .5, .9, [new(0, Rot: -2.5), new(.5, Rot: 2.5), new(1, Rot: -2.5)]),
        [Clip.Point] = new(1.4, true, EaseInOut, .5, .9, [new(0), new(.5, Tx: -6, Rot: -5), new(1)]),
        [Clip.Breathe] = new(12, true, EaseInOut, .5, 1, [new(0), new(.33, Sx: 1.05, Sy: 1.05), new(.66, Sx: 1.05, Sy: 1.05), new(1)]),
        [Clip.Idle] = Bob(3),
        [Clip.IdleTired] = Bob(4.5),
        [Clip.LookAround] = new(4, true, EaseInOut, .5, .5, [new(0), new(.30), new(.35, Sx: -1), new(.65, Sx: -1), new(.70), new(1)], Steps: true),
        [Clip.HoverPeek] = new(.3, false, EaseOut, .5, .5, [new(0, Ty: 175), new(1, Ty: 78)]),
        [Clip.ClimbOut] = new(4.2, false, EaseIn, .5, 0,
        [
            new(0), new(.07, Rot: -6), new(.14, Rot: 6), new(.21, Rot: -4), new(.28), new(.36, Ty: -8), new(.48, Ty: 95),
            new(.56, Ty: 92, Rot: 3), new(.64, Ty: 95, Rot: -3), new(.72, Ty: 95, Rot: 2), new(1, Ty: 175),
        ]),
        [Clip.ClimbOutShort] = new(2.6, false, EaseIn, .5, 0,
            [new(0), new(.25, Ty: 95), new(.40, Ty: 95, Rot: -8), new(.55, Ty: 95), new(1, Ty: 175)]),
        [Clip.ClimbOutFast] = new(.7, false, EaseIn, .5, .5, [new(0), new(1, Ty: 175)]),
        [Clip.RunToCar] = new(1.8, false, EaseIn, .5, .5, [new(0), new(.15, Tx: 10, Rot: 4), new(1, Tx: -320, Rot: -6)]),
        [Clip.DigExhausted] = new(1.8, false, EaseIn, .5, 1,
            [new(0), new(.1, Rot: -8), new(.2, Rot: 8), new(.3, Rot: -8), new(.4), new(1, Ty: 175)]),
    };

    public static MiloTransform Evaluate(Clip clip, double elapsed)
    {
        if (!Anims.TryGetValue(clip, out var a)) return clip == Clip.Gone ? MiloTransform.Hidden : new MiloTransform(0, 0, 0, 1, 1, .5, .5);
        var p = a.Loop ? elapsed % a.Duration / a.Duration : Math.Clamp(elapsed / a.Duration, 0, 1);
        var keys = a.Keys;
        var i = 0;
        while (i < keys.Length - 2 && p >= keys[i + 1].At) i++;
        var k0 = keys[i];
        var k1 = keys[i + 1];
        var local = k1.At > k0.At ? Math.Clamp((p - k0.At) / (k1.At - k0.At), 0, 1) : 1;
        var f = a.Steps ? (local >= 1 ? 1 : 0) : Bezier(a.Ease, local);
        return new MiloTransform(
            Lerp(k0.Tx, k1.Tx, f), Lerp(k0.Ty, k1.Ty, f), Lerp(k0.Rot, k1.Rot, f),
            Lerp(k0.Sx, k1.Sx, f), Lerp(k0.Sy, k1.Sy, f), a.Ox, a.Oy);
    }

    private static double Lerp(double a, double b, double f) => a + (b - a) * f;

    /// <summary>cubic-bezier(x1,y1,x2,y2) của CSS: tìm t theo x bằng Newton rồi trả y.</summary>
    private static double Bezier((double X1, double Y1, double X2, double Y2) c, double x)
    {
        if (x <= 0) return 0;
        if (x >= 1) return 1;
        static double B(double t, double p1, double p2) => 3 * (1 - t) * (1 - t) * t * p1 + 3 * (1 - t) * t * t * p2 + t * t * t;
        static double D(double t, double p1, double p2) => 3 * (1 - t) * (1 - t) * p1 + 6 * (1 - t) * t * (p2 - p1) + 3 * t * t * (1 - p2);
        var t = x;
        for (var n = 0; n < 8; n++)
        {
            var err = B(t, c.X1, c.X2) - x;
            if (Math.Abs(err) < 1e-5) break;
            var d = D(t, c.X1, c.X2);
            if (Math.Abs(d) < 1e-6) break;
            t = Math.Clamp(t - err / d, 0, 1);
        }
        return B(t, c.Y1, c.Y2);
    }
}

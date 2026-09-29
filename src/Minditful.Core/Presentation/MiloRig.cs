using Minditful.Core.Engine;

namespace Minditful.Core.Presentation;

/// <summary>Một bộ phận của SVG Milo (class của nhóm &lt;g&gt;) xoay quanh khớp và dịch chuyển.</summary>
public readonly record struct PartMove(string Part, double Angle, double Dx = 0, double Dy = 0, double ScaleY = 1);

/// <summary>Clip dạng chuỗi khung như tài liệu §12 ("11 khung · 8fps").</summary>
public sealed record Rig(string Name, int Frames, double Fps, bool Loop = true, bool PingPong = false);

/// <summary>
/// Clip "Cần vẽ" ở §12: dựng từ 8 tư thế SVG có sẵn bằng cách xoay tay, chân, đuôi, đầu, tai và chớp mắt quanh khớp.
/// Toạ độ theo hệ của SVG (trục đối xứng x = 120). Tay/chân/tai phải là bản lật của bên trái nên góc đổi dấu.
/// </summary>
public static class MiloRig
{
    /// <summary>Khớp của từng bộ phận trong hệ toạ độ SVG.</summary>
    public static readonly IReadOnlyDictionary<string, (double X, double Y)> Pivots = new Dictionary<string, (double, double)>
    {
        ["armL"] = (99, 146), ["armR"] = (141, 146),
        ["legL"] = (104, 188), ["legR"] = (136, 188),
        ["tail"] = (148, 182),
        ["head"] = (120, 140),
        ["earL"] = (100, 70), ["earR"] = (140, 70),
        ["eyes"] = (120, 96), ["look"] = (120, 96),
    };

    public static readonly Rig Idle = new("idle", 7, 6, PingPong: true);
    public static readonly Rig IdleTired = new("idleTired", 7, 5, PingPong: true);
    public static readonly Rig Greet = new("greet", 7, 7);
    public static readonly Rig Reminder = new("reminder", 9, 6);
    public static readonly Rig Point = new("point", 7, 5);
    public static readonly Rig Breathe = new("breathe", 8, 4, PingPong: true);
    public static readonly Rig Celebrate = new("celebrate", 8, 8);
    public static readonly Rig Stretch = new("stretch", 6, 5, Loop: false);
    public static readonly Rig Climb = new("climb", 11, 8);
    public static readonly Rig Run = new("run", 9, 10);
    public static readonly Rig Dig = new("dig", 9, 8);
    public static readonly Rig Look = new("look", 8, 2);
    public static readonly Rig Peek = new("peek", 3, 10, Loop: false);
    // Clip hài
    public static readonly Rig Slay = new("slay", 8, 6);
    public static readonly Rig SideEye = new("sideeye", 8, 2);
    public static readonly Rig Confused = new("confused", 8, 3, PingPong: true);
    public static readonly Rig Faint = new("faint", 11, 3, Loop: false);
    public static readonly Rig Vibe = new("vibe", 8, 6);
    public static readonly Rig Loading = new("loading", 10, 1.6, Loop: false);
    public static readonly Rig Cobweb = new("cobweb", 11, 2, Loop: false);
    public static readonly Rig Sip = new("sip", 11, 3);
    public static readonly Rig Zombie = new("zombie", 8, 3, PingPong: true);
    /// <summary>Ngủ trên chóp đuôi lúc bạn tập trung: thở chậm, đầu gục, tai cụp.</summary>
    public static readonly Rig Sleep = new("sleep", 8, 1.6, PingPong: true);

    public static Rig For(Clip clip, bool tired) => clip switch
    {
        Clip.HangPull or Clip.ClimbIn or Clip.PeekIn or Clip.ClimbOut or Clip.ClimbOutShort or Clip.ClimbOutFast => Climb,
        Clip.JumpIn or Clip.StandUp => tired ? IdleTired : Idle,
        Clip.Hello or Clip.Celebrate => Celebrate,
        Clip.Stretch => Stretch,
        Clip.Greet or Clip.Thanks => Greet,
        Clip.Reminder => Reminder,
        Clip.Point => Point,
        Clip.Breathe => Breathe,
        Clip.RunToCar => Run,
        Clip.DigExhausted => Dig,
        Clip.LookAround => Look,
        Clip.HoverPeek => Peek,
        Clip.IdleTired => IdleTired,
        Clip.Slay => Slay,
        Clip.SideEye => SideEye,
        Clip.Confused => Confused,
        Clip.Faint => Faint,
        Clip.Vibe => Vibe,
        Clip.Loading => Loading,
        Clip.Cobweb => Cobweb,
        Clip.ThisIsFine => Sip,
        Clip.Zombie => Zombie,
        _ => tired ? IdleTired : Idle,
    };

    public static int FrameIndex(Rig rig, double elapsed)
    {
        var i = (int)Math.Floor(Math.Max(0, elapsed) * rig.Fps);
        if (!rig.Loop) return Math.Min(i, rig.Frames - 1);
        if (!rig.PingPong) return i % rig.Frames;
        var period = rig.Frames * 2 - 2;
        var k = i % period;
        return k < rig.Frames ? k : period - k;
    }

    /// <summary>Chớp mắt 4.5 giây/lần; mệt thì nhắm lâu hơn (~300ms, §9.4).</summary>
    public static bool Blink(double seconds, bool tired) => seconds % 4.5 < (tired ? 0.3 : 0.15);

    /// <summary>Tư thế các bộ phận ở khung <paramref name="frame"/> của clip.</summary>
    public static IReadOnlyList<PartMove> Frame(Rig rig, int frame, bool blink)
    {
        var p = rig.Frames <= 1 ? 0 : (double)frame / (rig.Loop && !rig.PingPong ? rig.Frames : rig.Frames - 1);
        var s = Math.Sin(2 * Math.PI * p);
        var c = Math.Cos(2 * Math.PI * p);
        var moves = new List<PartMove>();
        void Sym(string left, string right, double a) { moves.Add(new(left, a)); moves.Add(new(right, -a)); }

        switch (rig.Name)
        {
            case "idle":
                moves.Add(new("tail", 6 * s));
                moves.Add(new("head", 1.5 * s));
                Sym("earL", "earR", frame == 3 ? -6 : 0); // giật tai 1 nhịp
                break;
            case "idleTired":
                moves.Add(new("tail", 3 * s, 0, 4));
                moves.Add(new("head", 4 + 1.5 * s, 0, 3)); // đầu gục
                Sym("earL", "earR", -14);                   // tai cụp
                break;
            case "greet":
                moves.Add(new("armR", -(110 + 18 * s)));    // giơ tay phải vẫy (bên phải là bản lật → góc âm)
                moves.Add(new("tail", 10 * s));
                moves.Add(new("head", 3 * s));
                break;
            case "reminder":
                moves.Add(new("head", 5 * s));
                moves.Add(new("tail", 9 * s));
                Sym("earL", "earR", 4 * c);
                break;
            case "point":
                moves.Add(new("armL", 75 + 8 * s));         // chỉ tay sang trái (phía thẻ)
                moves.Add(new("head", -4));
                moves.Add(new("tail", 6 * s));
                break;
            case "breathe":
                moves.Add(new("head", 0, 0, -3 * p));        // ngẩng lên theo nhịp hít
                Sym("armL", "armR", 10 * p);
                moves.Add(new("tail", 4 * s));
                break;
            case "celebrate":
                Sym("armL", "armR", 120 + 25 * s);          // hai tay giơ cao, nhún theo nhịp
                moves.Add(new("tail", 16 * s));
                Sym("earL", "earR", 6 * c);
                break;
            case "stretch":
                Sym("armL", "armR", 150 * p);               // vươn vai dần lên
                moves.Add(new("head", 0, 0, -4 * p));
                moves.Add(new("tail", 10 * p));
                break;
            case "climb":
                moves.Add(new("armL", 70 + 40 * s));          // tay trái với lên
                moves.Add(new("armR", -(70 - 40 * s)));       // tay phải so le
                moves.Add(new("legL", 0, 0, -10 * Math.Max(0, s)));  // co chân đạp lên
                moves.Add(new("legR", 0, 0, -10 * Math.Max(0, -s)));
                moves.Add(new("tail", 12 * c));
                break;
            case "run":
                moves.Add(new("legL", 8 * s, 0, -12 * Math.Max(0, s)));   // nâng gối xen kẽ
                moves.Add(new("legR", 8 * s, 0, -12 * Math.Max(0, -s)));
                moves.Add(new("armL", 20 + 25 * s));
                moves.Add(new("armR", -(20 - 25 * s)));
                moves.Add(new("tail", -20 + 8 * s));         // đuôi bay ra sau
                moves.Add(new("head", -3));
                break;
            case "dig":
                moves.Add(new("armL", 35 + 35 * s));
                moves.Add(new("armR", -(35 - 35 * s)));
                moves.Add(new("head", 8, 0, 4));
                Sym("earL", "earR", -12);
                break;
            case "look":
                var dx = frame < 3 ? -4 : frame < 6 ? 4 : 0;
                moves.Add(new("look", 0, dx));
                moves.Add(new("head", dx * 1.2));
                break;
            case "peek":
                Sym("earL", "earR", 8 - 4 * frame);         // vểnh tai khi ngóc đầu
                break;
            case "sleep":
                moves.Add(new("head", 12 + 2 * p, 0, 4 + 2 * p));   // đầu gục, nhấp nhô theo nhịp thở
                Sym("earL", "earR", -18);
                Sym("armL", "armR", 6);
                moves.Add(new("tail", 6 * s));
                break;
            // ---------- clip hài ----------
            case "slay":
                moves.Add(new("armR", -(150 + 10 * s)));    // tay phải chỉ thẳng lên trời
                moves.Add(new("armL", 28));                 // tay trái chống hông
                moves.Add(new("head", 7 + 2 * s));          // hất mặt
                moves.Add(new("tail", 18 * s));
                Sym("earL", "earR", 5 * c);
                break;
            case "sideeye":
                moves.Add(new("look", 0, frame < 2 ? -2 : -6)); // liếc dần sang phía thẻ
                moves.Add(new("eyes", 0, 0, 0, .55));       // nheo mắt
                moves.Add(new("head", -6));
                moves.Add(new("earL", -10));
                moves.Add(new("tail", 3 * s));
                blink = false;
                break;
            case "confused":
                moves.Add(new("head", 9 * s));              // nghiêng đầu qua lại
                moves.Add(new("look", 0, 3 * s, -1));
                moves.Add(new("armR", -(160 + 6 * s)));     // tay phải (vẽ sau đầu nên không bị khuất) gãi đầu
                moves.Add(new("earL", -8 + 6 * s));
                moves.Add(new("earR", 4));
                break;
            case "faint":
                // 0–3: giật mình, tay khua; 4+: mắt nhắm, tay buông (thân ngã do ClipAnimation)
                if (frame < 4)
                {
                    Sym("armL", "armR", 60 + 40 * Math.Sin(frame * 2.1));
                    Sym("earL", "earR", 10);
                }
                else
                {
                    Sym("armL", "armR", 15);
                    Sym("earL", "earR", -16);
                    moves.Add(new("eyes", 0, 0, 0, .12));
                    blink = false;
                }
                break;
            case "vibe":
                moves.Add(new("armL", 120 + 30 * s));       // hai tay đánh nhịp so le
                moves.Add(new("armR", -(40 + 30 * c)));
                moves.Add(new("legL", 0, 0, -8 * Math.Max(0, s)));
                moves.Add(new("legR", 0, 0, -8 * Math.Max(0, -s)));
                moves.Add(new("head", 7 * s));
                moves.Add(new("tail", 20 * s));
                Sym("earL", "earR", 6 * c);
                break;
            case "loading":
                // Mới đầu gục đầu, tai cụp, mắt lim dim; tải xong thì tỉnh hẳn
                moves.Add(new("head", 8 * (1 - p), 0, 4 * (1 - p)));
                Sym("earL", "earR", -16 * (1 - p));
                if (p < .8) moves.Add(new("eyes", 0, 0, 0, .35 + .6 * p));
                if (frame == rig.Frames - 1) moves.Add(new("tail", 14));
                blink = false;
                break;
            case "cobweb":
                // Đứng im phủ bụi (8 khung), rồi rũ mình (3 khung cuối)
                if (frame < 8)
                {
                    moves.Add(new("eyes", 0, 0, 0, .3));
                    Sym("earL", "earR", -12);
                    moves.Add(new("head", 5, 0, 3));
                    blink = false;
                }
                else
                {
                    var k = frame % 2 == 0 ? 1 : -1;
                    moves.Add(new("head", 10 * k));
                    Sym("armL", "armR", 30 + 20 * k);
                    moves.Add(new("tail", 25 * k));
                    Sym("earL", "earR", 12 * k);
                }
                break;
            case "sip":
                // Nhấp ngụm cà phê mỗi 4 giây, mặt vẫn cười "ổn mà"
                var up = frame is >= 4 and <= 7;
                moves.Add(new("armR", up ? 100 : 35));       // cầm ly trước ngực → đưa lên miệng
                moves.Add(new("head", up ? -5 : 0));
                moves.Add(new("tail", 3 * s));
                if (up) moves.Add(new("eyes", 0, 0, 0, .15));
                break;
            case "zombie":
                Sym("armL", "armR", 82 + 4 * s);            // hai tay đưa thẳng ra
                moves.Add(new("head", 10 + 4 * s, 0, 3));   // đầu ngoẹo
                Sym("earL", "earR", -18);
                moves.Add(new("legL", 0, 0, -6 * Math.Max(0, s)));
                moves.Add(new("legR", 0, 0, -6 * Math.Max(0, -s)));
                moves.Add(new("tail", 4 * s, 0, 4));
                break;
        }
        if (blink) moves.Add(new("eyes", 0, 0, 0, 0.12));
        return moves;
    }
}

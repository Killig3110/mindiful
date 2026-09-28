using Minditful.Core.Engine;
using Minditful.Integrations;

namespace Minditful.App.Services;

/// <summary>
/// Một phiên Milo chạy trên desktop thật. Cả 3 môi trường dùng chung overlay, chỉ khác nguồn thời gian và dữ liệu:
/// Demo = đồng hồ + dữ liệu + tín hiệu theo kịch bản ngày mẫu; Sandbox/Production = đồng hồ thật, Graph/Azure Boards, tín hiệu Windows.
/// </summary>
internal interface IMiloSession : IDisposable
{
    AppEnvironment Env { get; }
    MiloEngine Engine { get; }
    /// <summary>Ẩn Milo khỏi share màn hình.</summary>
    bool ContentProtection { get; }
    /// <summary>Overlay gọi mỗi khung hình (khi Milo hiện) hoặc 4 lần/giây (khi ẩn). <paramref name="realDt"/> = giây thật đã trôi.</summary>
    void Pump(double realDt);
}

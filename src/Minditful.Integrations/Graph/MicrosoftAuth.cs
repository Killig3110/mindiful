using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Minditful.Integrations.Graph;

/// <summary>Đăng nhập Microsoft (MSAL public client) cho Graph và — nếu cấu hình — Azure DevOps.</summary>
public sealed class MicrosoftAuth
{
    public const string AzureDevOpsScope = "499b84ac-1321-427f-aa17-267ca6975798/.default";

    private readonly GraphOptions _opt;
    private readonly string _cacheDir;
    private readonly Func<PublicClientApplicationBuilder, PublicClientApplicationBuilder>? _customize;
    private IPublicClientApplication? _app;
    private bool _useOsAccount;

    /// <summary>Quyền thực sự được cấp (có thể ít hơn khi tenant chưa admin consent — mục 14).</summary>
    public IReadOnlySet<string> GrantedScopes { get; private set; } = new HashSet<string>();
    public string? UserName { get; private set; }

    /// <param name="customize">App WPF gắn WAM broker + cửa sổ cha ở đây (cần net8.0-windows).</param>
    public MicrosoftAuth(GraphOptions opt, string cacheDir, Func<PublicClientApplicationBuilder, PublicClientApplicationBuilder>? customize = null)
    {
        _opt = opt;
        _cacheDir = cacheDir;
        _customize = customize;
    }

    public bool IsConfigured => _opt.Enabled && !string.IsNullOrWhiteSpace(_opt.ClientId) && !_opt.ClientId.StartsWith('<');

    private async Task<IPublicClientApplication> AppAsync()
    {
        if (_app is not null) return _app;
        var b = PublicClientApplicationBuilder.Create(_opt.ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, _opt.TenantId)
            .WithRedirectUri(_opt.RedirectUri);
        if (_customize is not null)
        {
            b = _customize(b);
            _useOsAccount = _opt.UseBroker;
        }
        _app = b.Build();
        Directory.CreateDirectory(_cacheDir);
        var props = new StorageCreationPropertiesBuilder("msal.cache", _cacheDir).Build();
        var helper = await MsalCacheHelper.CreateAsync(props);
        helper.RegisterCache(_app.UserTokenCache);
        return _app;
    }

    /// <summary>Lấy token Graph. <paramref name="interactive"/> = cho phép bật cửa sổ đăng nhập.</summary>
    public Task<string> GraphTokenAsync(bool interactive = false, CancellationToken ct = default) =>
        TokenAsync(_opt.Scopes.Select(s => s.Contains('/') ? s : "https://graph.microsoft.com/" + s).ToArray(), interactive, trackScopes: true, ct);

    public Task<string> AzureDevOpsTokenAsync(bool interactive = false, CancellationToken ct = default) =>
        TokenAsync([AzureDevOpsScope], interactive, trackScopes: false, ct);

    private async Task<string> TokenAsync(string[] scopes, bool interactive, bool trackScopes, CancellationToken ct)
    {
        if (!IsConfigured) throw new InvalidOperationException("Chưa cấu hình ClientId cho Microsoft Graph trong appsettings.json.");
        var app = await AppAsync();
        AuthenticationResult result;
        try
        {
            var account = _useOsAccount ? PublicClientApplication.OperatingSystemAccount : (await app.GetAccountsAsync()).FirstOrDefault();
            result = await app.AcquireTokenSilent(scopes, account).ExecuteAsync(ct);
        }
        catch (MsalUiRequiredException) when (interactive)
        {
            result = await app.AcquireTokenInteractive(scopes).WithPrompt(Prompt.SelectAccount).ExecuteAsync(ct);
        }
        if (trackScopes)
        {
            GrantedScopes = result.Scopes.Select(s => s.Split('/').Last()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            UserName = result.Account?.Username;
        }
        return result.AccessToken;
    }

    public bool Has(string scope) => GrantedScopes.Contains(scope);

    /// <summary>Quyền đã khai trong cấu hình nhưng token chưa có (chưa consent hoặc admin chưa duyệt).</summary>
    public IReadOnlyList<string> MissingScopes =>
        GrantedScopes.Count == 0 ? [] : _opt.Scopes.Where(s => !Has(s.Split('/').Last())).ToList();

    /// <summary>Đã từng đăng nhập trên máy này (còn tài khoản trong cache) — nếu chưa thì lần mở đầu cần mở trình duyệt.</summary>
    public async Task<bool> HasCachedAccountAsync()
    {
        if (!IsConfigured) return false;
        if (_opt.UseBroker) return true; // WAM dùng tài khoản Windows, lấy im lặng được
        return (await (await AppAsync()).GetAccountsAsync()).Any();
    }

    public async Task SignOutAsync()
    {
        var app = await AppAsync();
        foreach (var a in await app.GetAccountsAsync()) await app.RemoveAsync(a);
        GrantedScopes = new HashSet<string>();
        UserName = null;
    }
}

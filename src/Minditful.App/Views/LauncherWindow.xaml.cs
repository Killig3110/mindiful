using System.Windows;
using Minditful.Integrations;

namespace Minditful.App.Views;

public partial class LauncherWindow : Window
{
    public AppEnvironment? Choice { get; private set; }
    public bool RememberChoice => Remember.IsChecked == true;

    internal LauncherWindow(MinditfulOptions opt)
    {
        InitializeComponent();
        SandboxState.Text = Describe(opt.Sandbox);
        ProdState.Text = Describe(opt.Production);
    }

    private static string Describe(ConnectionOptions c)
    {
        var graph = c.Graph.Enabled && !string.IsNullOrWhiteSpace(c.Graph.ClientId) && !c.Graph.ClientId.StartsWith('<');
        var ado = c.AzureDevOps.Enabled && !c.AzureDevOps.Organization.StartsWith('<') && c.AzureDevOps.Organization.Length > 0;
        return (graph ? "✓ Microsoft Graph đã cấu hình" : "✗ Chưa có ClientId Graph") + " · " +
               (ado ? $"✓ Azure DevOps {c.AzureDevOps.Organization}" : "✗ Chưa cấu hình Azure DevOps");
    }

    private void Pick(AppEnvironment env)
    {
        Choice = env;
        DialogResult = true;
    }

    private void Demo_Click(object sender, RoutedEventArgs e) => Pick(AppEnvironment.Demo);
    private void Sandbox_Click(object sender, RoutedEventArgs e) => Pick(AppEnvironment.Sandbox);
    private void Prod_Click(object sender, RoutedEventArgs e) => Pick(AppEnvironment.Production);
}

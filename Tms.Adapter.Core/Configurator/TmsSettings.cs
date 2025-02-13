namespace Tms.Adapter.Core.Configurator;

public class TmsSettings
{
    private string _url = null!;

    public string Url
    {
        get => _url.TrimEnd('/');
        set => _url = value;
    }

    public string PrivateToken { get; set; } = null!;
    public string ProjectId { get; set; } = null!;
    public string ConfigurationId { get; set; } = null!;
    public string TestRunId { get; set; } = null!;
    public string? TestRunName { get; set; }
    public bool AutomaticCreationTestCases { get; set; }
    public bool AutomaticUpdationLinksToTestCases { get; set; }
    public bool CertValidation { get; set; }
    public bool IgnoreParameters { get; set; }
    public bool IsDebug { get; set; }
}
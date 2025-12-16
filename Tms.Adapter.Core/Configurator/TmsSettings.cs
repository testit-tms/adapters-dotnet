namespace Tms.Adapter.Core.Configurator;

public class TmsSettings
{
    private string _url = string.Empty;

    public string Url
    {
        get => _url.TrimEnd('/');
        set => _url = value;
    }

    public string PrivateToken { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string ConfigurationId { get; set; } = string.Empty;
    public string TestRunId { get; set; } = string.Empty;
    public string TestRunName { get; set; } = string.Empty;
    public bool AutomaticCreationTestCases { get; set; }
    public bool AutomaticUpdationLinksToTestCases { get; set; }
    public bool CertValidation { get; set; }
    public bool IgnoreParameters { get; set; }
    public bool IsDebug { get; set; }
}
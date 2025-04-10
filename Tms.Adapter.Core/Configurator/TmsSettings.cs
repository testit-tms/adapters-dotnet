namespace Tms.Adapter.Core.Configurator;

public class TmsSettings
{
    private string _url;

    public string Url
    {
        get => _url.TrimEnd('/');
        set => _url = value;
    }

    public string PrivateToken { get; set; }
    public string ProjectId { get; set; }
    public string ConfigurationId { get; set; }
    public string TestRunId { get; set; }
    public string TestRunName { get; set; }
    public bool AutomaticCreationTestCases { get; set; }
    public bool AutomaticUpdationLinksToTestCases { get; set; }
    public bool CertValidation { get; set; }
    public bool IgnoreParameters { get; set; }
    public bool IsDebug { get; set; }
}
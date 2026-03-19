using TestIT.ApiClient.Model;
using TmsRunner.Entities.AutoTest;
using AutoTest = TmsRunner.Entities.AutoTest.AutoTest;
using AutoTestStep = TmsRunner.Entities.AutoTest.AutoTestStep;
using AutoTestStepResult = TmsRunner.Entities.AutoTest.AutoTestStepResult;

namespace TmsRunner.Utils;

public static class Converter
{
    public static AutoTestCreateApiModel ConvertAutoTestDtoToPostModel(AutoTest autotest, string? projectId)
    {
        var links = autotest.Links?.Select(l =>
            new LinkCreateApiModel(
                l.Title!,
                l.Url!,
                l.Description!,
                Enum.Parse<LinkType>(l.Type.ToString()!))
        ).ToList();

        return new AutoTestCreateApiModel(externalId: autotest.ExternalId ?? string.Empty, name: autotest.Name ?? string.Empty)
        {
            ExternalId = autotest.ExternalId ?? string.Empty,
            Links = links!,
            ProjectId = new Guid(projectId ?? string.Empty),
            Namespace = autotest.Namespace ?? string.Empty,
            Classname = autotest.Classname ?? string.Empty,
            Steps = ConvertStepsToModel(autotest.Steps) ?? [],
            Setup = ConvertStepsToModel(autotest.Setup) ?? [],
            Teardown = ConvertStepsToModel(autotest.Teardown) ?? [],
            Title = autotest.Title ?? string.Empty,
            Description = autotest.Description ?? string.Empty,
            Labels = ConvertLabelsToModel(autotest.Labels) ?? [],
            Tags = autotest.Tags ?? []
        };
    }

    public static AutoTestUpdateApiModel ConvertAutoTestDtoToPutModel(AutoTest autotest, string? projectId)
    {
        var links = autotest.Links?.Select(l =>
            new LinkUpdateApiModel(
                title: l.Title!,
                url: l.Url!,
                description: l.Description!,
                type: Enum.Parse<LinkType>(l.Type.ToString()!))
        ).ToList();


        return new AutoTestUpdateApiModel(externalId: autotest.ExternalId ?? string.Empty, name: autotest.Name ?? string.Empty)
        {
            Links = links ?? [],
            ProjectId = new Guid(projectId ?? string.Empty),
            Name = autotest.Name ?? string.Empty,
            Namespace = autotest.Namespace ?? string.Empty,
            Classname = autotest.Classname ?? string.Empty,
            Steps = ConvertStepsToModel(autotest.Steps) ?? [],
            Setup = ConvertStepsToModel(autotest.Setup) ?? [],
            Teardown = ConvertStepsToModel(autotest.Teardown) ?? [],
            Title = autotest.Title ?? string.Empty,
            Description = autotest.Description ?? string.Empty,
            Labels = ConvertLabelsToModel(autotest.Labels) ?? [],
            Tags = autotest.Tags ?? [],
            IsFlaky = autotest.IsFlaky
        };
    }
    
    // None = 0,
    // Passed = 1,
    // Failed = 2,
    // Skipped = 3,
    // NotFound = 4,
    private static TestStatusType MapToStatusType(string status)
    {
        switch (status)
        {
            case "Passed": return TestStatusType.Succeeded;
            case "Failed": return TestStatusType.Failed;
            case "Skipped": return TestStatusType.Incomplete;
            case "Blocked": return TestStatusType.Incomplete;
            case "InProgress": return TestStatusType.InProgress;
            default: return TestStatusType.Incomplete;
        }
    }

    public static AutoTestResultsForTestRunModel ConvertResultToModel(AutoTestResult result, string? configurationId)
    {
        var links = result.Links?.Select(l =>
            new LinkPostModel(
                l.Title!,
                l.Url!,
                l.Description!,
                Enum.TryParse<LinkType>(l.Type?.ToString(), true, out var res) ? res : null)
        ).ToList();

        return new AutoTestResultsForTestRunModel(
            autoTestExternalId: result.ExternalId ?? string.Empty)
        {
            StatusType = MapToStatusType(result.Outcome?.ToString() ?? string.Empty), 
            ConfigurationId = new Guid(configurationId ?? string.Empty),
            Links = links ?? [],
            Message = result.Message ?? string.Empty,
            Traces = result.Traces ?? string.Empty,
            StartedOn = result.StartedOn,
            CompletedOn = result.CompletedOn,
            Duration = result.Duration,
            Attachments = result.Attachments?.Select(a => new AttachmentPutModel(a)).ToList() ?? [],
            Parameters = result.Parameters ?? [],
            StepResults = ConvertResultStepToModel(result.StepResults),
            SetupResults = ConvertResultStepToModel(result.SetupResults),
            TeardownResults = ConvertResultStepToModel(result.TeardownResults)
        };
    }

    private static List<LabelApiModel>? ConvertLabelsToModel(IEnumerable<string>? labels)
    {
        return labels?.Select(l => new LabelApiModel(l)).ToList();
    }

    private static List<AttachmentPutModelAutoTestStepResultsModel> ConvertResultStepToModel(IEnumerable<AutoTestStepResult>? autotests)
    {
        return autotests?.Select(s => new AttachmentPutModelAutoTestStepResultsModel
        {
            Title = s.Title ?? string.Empty,
            Description = s.Description ?? string.Empty,
            StartedOn = s.StartedOn,
            CompletedOn = s.CompletedOn,
            Duration = s.Duration,
            Attachments = s.Attachments?.Select(a => new AttachmentPutModel(a)).ToList() ?? [],
            Parameters = s.Parameters ?? [],
            StepResults = ConvertResultStepToModel(s.Steps),
            Outcome = Enum.Parse<AvailableTestResultOutcome>(s.Outcome ?? string.Empty)
        }).ToList() ?? [];
    }

    private static List<AutoTestStepApiModel>? ConvertStepsToModel(IEnumerable<AutoTestStep>? autotestSteps)
    {
        return autotestSteps?.Select(s => new AutoTestStepApiModel(
                s.Title ?? string.Empty,
                s.Description  ?? string.Empty,
                ConvertStepsToModel(s.Steps) ?? [])).ToList();
    }

    public static TestResultsFilterApiModel BuildTestResultsFilterApiModel(string testRunId, string configurationId)
    {
        return new TestResultsFilterApiModel
        {
            TestRunIds = [new Guid(testRunId)],
            ConfigurationIds = [new Guid(configurationId)],
            // TODO: change to statusTypes
            StatusCodes = ["InProgress"]
        };
    }

    public static UpdateEmptyTestRunApiModel BuildUpdateEmptyTestRunApiModel(TestRunV2ApiResult testRun)
    {
        return new UpdateEmptyTestRunApiModel(name: testRun.Name)
        {
            Id = testRun.Id,
            Description = testRun.Description,
            LaunchSource = testRun.LaunchSource,
            Attachments = testRun.Attachments.Select(attachment => new AssignAttachmentApiModel(id: attachment.Id)).ToList(),
            Links = testRun.Links.Select(link => new UpdateLinkApiModel(
                id: link.Id,
                title: link.Title,
                url: link.Url,
                description: link.Description,
                type: link.Type,
                hasInfo: link.HasInfo
                )).ToList(),
        };
    }
}
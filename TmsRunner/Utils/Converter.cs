using System.Collections.Generic;
using System.Xml.Linq;
using TestIT.ApiClient.Model;
using TmsRunner.Entities.AutoTest;
using AutoTest = TmsRunner.Entities.AutoTest.AutoTest;
using AutoTestStep = TmsRunner.Entities.AutoTest.AutoTestStep;
using AutoTestStepResult = TmsRunner.Entities.AutoTest.AutoTestStepResult;

namespace TmsRunner.Utils;

public static class Converter
{
    public static AutoTestPostModel ConvertAutoTestDtoToPostModel(AutoTest autotest, string? projectId)
    {
        var links = autotest.Links?.Select(l =>
            new LinkPostModel(
                l.Title,
                l.Url,
                l.Description,
                Enum.Parse<LinkType>(l.Type.ToString()!))
        ).ToList();

        return new AutoTestPostModel(externalId: autotest.ExternalId ?? string.Empty, name: autotest.Name ?? string.Empty)
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
            Labels = ConvertLabelsToModel(autotest.Labels) ?? []
        };
    }

    public static AutoTestPutModel ConvertAutoTestDtoToPutModel(AutoTest autotest, string? projectId)
    {
        var links = autotest.Links?.Select(l =>
            new LinkPutModel(
                title: l.Title,
                url: l.Url,
                description: l.Description,
                type: Enum.Parse<LinkType>(l.Type.ToString()!))
        ).ToList();


        return new AutoTestPutModel(externalId: autotest.ExternalId ?? string.Empty, name: autotest.Name ?? string.Empty)
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
            IsFlaky = autotest.IsFlaky
        };
    }

    public static AutoTestResultsForTestRunModel ConvertResultToModel(AutoTestResult autotest, string? configurationId)
    {
        var links = autotest.Links?.Select(l =>
            new LinkPostModel(
                l.Title,
                l.Url,
                l.Description,
                Enum.TryParse<LinkType>(l.Type?.ToString(), true, out var result) ? result : null)
        ).ToList();

        return new AutoTestResultsForTestRunModel(
            autoTestExternalId: autotest.ExternalId ?? string.Empty)
        {
            StatusCode = autotest.Outcome?.ToString() ?? string.Empty,
            ConfigurationId = new Guid(configurationId ?? string.Empty),
            Links = links ?? [],
            Message = autotest.Message ?? string.Empty,
            Traces = autotest.Traces ?? string.Empty,
            StartedOn = autotest.StartedOn,
            CompletedOn = autotest.CompletedOn,
            Duration = autotest.Duration,
            Attachments = autotest.Attachments?.Select(a => new AttachmentPutModel(a)).ToList() ?? [],
            Parameters = autotest.Parameters ?? [],
            StepResults = ConvertResultStepToModel(autotest.StepResults),
            SetupResults = ConvertResultStepToModel(autotest.SetupResults),
            TeardownResults = ConvertResultStepToModel(autotest.TeardownResults)
        };
    }

    private static List<LabelPostModel>? ConvertLabelsToModel(IEnumerable<string>? labels)
    {
        return labels?.Select(l => new LabelPostModel(l)).ToList();
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

    private static List<AutoTestStepModel>? ConvertStepsToModel(IEnumerable<AutoTestStep>? autotestSteps)
    {
        return autotestSteps?.Select(s => new AutoTestStepModel(
                s.Title ?? string.Empty,
                s.Description ?? string.Empty,
                ConvertStepsToModel(s.Steps) ?? [])).ToList();
    }

    public static TestResultsFilterApiModel BuildTestResultsFilterApiModel(string testRunId, string configurationId)
    {
        return new TestResultsFilterApiModel
        {
            TestRunIds = [new Guid(testRunId)],
            ConfigurationIds = [new Guid(configurationId)],
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
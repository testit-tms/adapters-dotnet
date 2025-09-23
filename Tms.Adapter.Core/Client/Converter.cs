using TestIT.ApiClient.Model;
using Tms.Adapter.Core.Models;
using Link = Tms.Adapter.Core.Models.Link;
using LinkType = TestIT.ApiClient.Model.LinkType;
using StepResult = Tms.Adapter.Core.Models.StepResult;

namespace Tms.Adapter.Core.Client;

public static class Converter
{
    public static AutoTestPostModel ConvertAutoTestDtoToPostModel(TestContainer result, ClassContainer container,
        string projectId)
    {
        return new AutoTestPostModel(externalId: result.ExternalId, name: result.DisplayName)
        {
            ExternalId = result.ExternalId,
            Links = ConvertLinksToPostModel(result.Links),
            ProjectId = new Guid(projectId),
            Namespace = result.Namespace,
            Classname = result.ClassName,
            Steps = ConvertStepsToModel(result.Steps),
            Setup = ConvertFixturesToModel(container.Befores),
            Teardown = ConvertFixturesToModel(container.Afters),
            Title = result.Title,
            Description = result.Description,
            Labels = ConvertLabelsToPostModel(result.Labels),
            ExternalKey = result.ExternalKey,
        };
    }

    public static AutoTestPutModel ConvertAutoTestDtoToPutModel(TestContainer result, ClassContainer container,
        string projectId)
    {
        return new AutoTestPutModel(externalId: result.ExternalId, name: result.DisplayName)
        {
            ExternalId = result.ExternalId,
            Links = ConvertLinksToPutModel(result.Links),
            ProjectId = new Guid(projectId),
            Namespace = result.Namespace,
            Classname = result.ClassName,
            Steps = ConvertStepsToModel(result.Steps),
            Setup = ConvertFixturesToModel(container.Befores),
            Teardown = ConvertFixturesToModel(container.Afters),
            Title = result.Title,
            Description = result.Description,
            Labels = ConvertLabelsToPostModel(result.Labels),
            ExternalKey = result.ExternalKey,
        };
    }

    public static AutoTestResultsForTestRunModel ConvertResultToModel(TestContainer result, ClassContainer container,
        string configurationId)
    {
        return new AutoTestResultsForTestRunModel(
            autoTestExternalId: result.ExternalId)
        {
            StatusCode = result.Status.ToString(),
            ConfigurationId = new Guid(configurationId),
            Links = ConvertLinksToPostModel(result.ResultLinks),
            Message = result.Message,
            Traces = result.Trace,
            StartedOn = DateTimeOffset.FromUnixTimeMilliseconds(container.Start).UtcDateTime,
            CompletedOn = DateTimeOffset.FromUnixTimeMilliseconds(container.Stop).UtcDateTime,
            Duration = result.Stop - result.Start,
            Attachments = result.Attachments.Select(a => new AttachmentPutModel(new Guid(a))).ToList(),
            Parameters = result.Parameters,
            StepResults = ConvertResultStepToModel(result.Steps),
            SetupResults = ConvertResultFixtureToModel(container.Befores),
            TeardownResults = ConvertResultFixtureToModel(container.Afters)
        };
    }

    private static List<AttachmentPutModelAutoTestStepResultsModel> ConvertResultStepToModel(
        IEnumerable<StepResult> dtos)
    {
        return dtos
            .Select(s => new AttachmentPutModelAutoTestStepResultsModel
            {
                Title = s.DisplayName,
                Description = s.Description,
                StartedOn = DateTimeOffset.FromUnixTimeMilliseconds(s.Start).UtcDateTime,
                CompletedOn = DateTimeOffset.FromUnixTimeMilliseconds(s.Stop).UtcDateTime,
                Duration = s.Stop - s.Start,
                Attachments = s.Attachments.Select(a => new AttachmentPutModel(new Guid(a))).ToList(),
                Parameters = s.Parameters,
                StepResults = ConvertResultStepToModel(s.Steps),
                Outcome = Enum.Parse<AvailableTestResultOutcome>(s.Status.ToString())
            }).ToList();
    }

    private static List<AttachmentPutModelAutoTestStepResultsModel> ConvertResultFixtureToModel(
        IEnumerable<FixtureResult> dtos)
    {
        return dtos
            .Select(s => new AttachmentPutModelAutoTestStepResultsModel
            {
                Title = s.DisplayName,
                Description = s.Description,
                StartedOn = DateTimeOffset.FromUnixTimeMilliseconds(s.Start).UtcDateTime,
                CompletedOn = DateTimeOffset.FromUnixTimeMilliseconds(s.Stop).UtcDateTime,
                Duration = s.Stop - s.Start,
                Attachments = s.Attachments.Select(a => new AttachmentPutModel(new Guid(a))).ToList(),
                Parameters = s.Parameters,
                StepResults = ConvertResultStepToModel(s.Steps),
                Outcome = Enum.Parse<AvailableTestResultOutcome>(s.Status.ToString())
            }).ToList();
    }

    private static List<LinkPostModel> ConvertLinksToPostModel(IEnumerable<Link> links)
    {
        return links.Select(l =>
            new LinkPostModel(url: l.Url)
            {
                Title = l.Title,
                Description = l.Description,
                Type = l.Type != null
                    ? (LinkType?)Enum.Parse(typeof(LinkType), l.Type.ToString())
                    : null
            }
        ).ToList();
    }

    private static List<LinkPutModel> ConvertLinksToPutModel(IEnumerable<Link> links)
    {
        return links.Select(l =>
            new LinkPutModel(url: l.Url)
            {
                Title = l.Title,
                Description = l.Description,
                Type = l.Type != null
                    ? (LinkType?)Enum.Parse(typeof(LinkType), l.Type.ToString())
                    : null
            }
        ).ToList();
    }

    private static List<LabelPostModel> ConvertLabelsToPostModel(IEnumerable<string> labels)
    {
        return labels.Select(l =>
                new LabelPostModel(l))
            .ToList();
    }

    private static List<AutoTestStepModel> ConvertStepsToModel(IEnumerable<StepResult> stepDtos)
    {
        return stepDtos
            .Select(s => new AutoTestStepModel(
                s.DisplayName,
                s.Description,
                ConvertStepsToModel(s.Steps))).ToList();
    }

    private static List<AutoTestStepModel> ConvertFixturesToModel(IEnumerable<FixtureResult> fixtures)
    {
        return fixtures
            .Select(s => new AutoTestStepModel(
                s.DisplayName,
                s.Description,
                ConvertStepsToModel(s.Steps))).ToList();
    }
}
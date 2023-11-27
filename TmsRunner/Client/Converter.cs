using TestIT.ApiClient.Model;
using TmsRunner.Models;

namespace TmsRunner.Client;

public class Converter
{
    public static CreateAutoTestRequest ConvertAutoTestDtoToPostModel(AutoTest dto, string projectId)
    {
        var links = dto.Links?.Select(l =>
            new LinkPostModel(
                l.Title,
                l.Url,
                l.Description,
                Enum.Parse<LinkType>(l.Type.ToString()!))
        ).ToList();

        return new CreateAutoTestRequest(externalId: dto.ExternalId, name: dto.Name)
        {
            ExternalId = dto.ExternalId,
            Links = links!,
            ProjectId = new Guid(projectId),
            Namespace = dto.Namespace,
            Classname = dto.Classname,
            Steps = ConvertStepsToModel(dto.Steps),
            Setup = ConvertStepsToModel(dto.Setup),
            Teardown = ConvertStepsToModel(dto.Teardown),
            Title = dto.Title,
            Description = dto.Description,
            Labels = ConvertLabelsToModel(dto.Labels)
        };
    }

    public static UpdateAutoTestRequest ConvertAutoTestDtoToPutModel(AutoTest dto, string projectId)
    {
        var links = dto.Links.Select(l =>
            new LinkPutModel(
                title: l.Title,
                url: l.Url,
                description: l.Description,
                type: Enum.Parse<LinkType>(l.Type.ToString()!))
        ).ToList();


        return new UpdateAutoTestRequest(externalId: dto.ExternalId, name: dto.Name)
        {
            Links = links,
            ProjectId = new Guid(projectId), Name = dto.Name,
            Namespace = dto.Namespace,
            Classname = dto.Classname,
            Steps = ConvertStepsToModel(dto.Steps),
            Setup = ConvertStepsToModel(dto.Setup),
            Teardown = ConvertStepsToModel(dto.Teardown),
            Title = dto.Title,
            Description = dto.Description,
            Labels = ConvertLabelsToModel(dto.Labels),
            IsFlaky = dto.IsFlaky
        };
    }

    public static AutoTestResultsForTestRunModel ConvertResultToModel(AutoTestResult dto, string configurationId)
    {
        var links = dto.Links?.Select(l =>
            new LinkPostModel(
                l.Title,
                l.Url,
                l.Description,
                Enum.Parse<LinkType>(l.Type.ToString()!))
        ).ToList();

        return new AutoTestResultsForTestRunModel(
            autoTestExternalId: dto.ExternalId,
            outcome: Enum.Parse<AvailableTestResultOutcome>(dto.Outcome.ToString()))
        {
            ConfigurationId = new Guid(configurationId),
            Links = links,
            Message = dto.Message,
            Traces = dto.Traces,
            StartedOn = dto.StartedOn,
            CompletedOn = dto.CompletedOn,
            Duration = dto.Duration,
            Attachments = dto.Attachments.Select(a => new AttachmentPutModel(a)).ToList(),
            Parameters = dto.Parameters,
            StepResults = ConvertResultStepToModel(dto.StepResults),
            SetupResults = ConvertResultStepToModel(dto.SetupResults),
            TeardownResults = ConvertResultStepToModel(dto.TeardownResults)
        };
    }

    private static List<LabelPostModel>? ConvertLabelsToModel(IEnumerable<string>? labels)
    {
        return labels?.Select(l =>
                new LabelPostModel(l))
            .ToList();
    }

    private static List<AttachmentPutModelAutoTestStepResultsModel> ConvertResultStepToModel(
        IEnumerable<AutoTestStepResult> dtos)
    {
        return dtos
            .Select(s => new AttachmentPutModelAutoTestStepResultsModel
            {
                Title = s.Title,
                Description = s.Description,
                StartedOn = s.StartedOn,
                CompletedOn = s.CompletedOn,
                Duration = s.Duration,
                Attachments = s.Attachments.Select(a => new AttachmentPutModel(a)).ToList(),
                Parameters = s.Parameters,
                StepResults = ConvertResultStepToModel(s.Steps),
                Outcome = Enum.Parse<AvailableTestResultOutcome>(s.Outcome)
            }).ToList();
    }

    private static List<AutoTestStepModel> ConvertStepsToModel(IEnumerable<AutoTestStep> stepDtos)
    {
        return stepDtos
            .Select(s => new AutoTestStepModel(
                s.Title,
                s.Description,
                ConvertStepsToModel(s.Steps))).ToList();
    }
}
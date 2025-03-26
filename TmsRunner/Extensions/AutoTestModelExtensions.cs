using TestIT.ApiClient.Model;

namespace TmsRunner.Extensions;

public static class AutoTestModelExtensions
{
    public static AutoTestApiResult ToApiResult(this AutoTestModel model)
    {
        return new AutoTestApiResult()
        {
            Id = model.Id,
            ProjectId = model.ProjectId,
            ExternalId = model.ExternalId,
            Name = model.Name,
            Classname = model.Classname,
            CreatedById = model.CreatedById,
            CreatedDate = model.CreatedDate,
            Description = model.Description,
            ExternalKey = model.ExternalKey,
            GlobalId = model.GlobalId,
            Title = model.Title,
            IsFlaky = model.IsFlaky ?? false,
            MustBeApproved = model.MustBeApproved,
            IsDeleted = model.IsDeleted,
            ModifiedById = model.ModifiedById,
            ModifiedDate = model.ModifiedDate,
            LastTestResultId = model.LastTestResultId,
            LastTestRunId = model.LastTestRunId,
            LastTestRunName = model.LastTestRunName,
            LastTestResultOutcome = model.LastTestResultOutcome,
            LastTestResultConfiguration = model.LastTestResultConfiguration.ToApiResult()
        };
    }

    private static ConfigurationShortApiResult ToApiResult(this ConfigurationShortModel model)
    {
        return new ConfigurationShortApiResult()
        {
            Id = model.Id,
            Name = model.Name
        };
    }
} 



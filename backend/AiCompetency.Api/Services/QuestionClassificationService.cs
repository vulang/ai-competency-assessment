using System.ComponentModel.DataAnnotations;
using AiCompetency.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AiCompetency.Api.Services;

public sealed record QuestionClassification(int SkillId, string Group, string Level);

public sealed class QuestionClassificationService(ApplicationDbContext context)
{
    public async Task<QuestionClassification> ResolveAsync(string? group, string? level, int? skillId, int? difficulty)
    {
        if (difficulty is < 1 or > 5) throw new ValidationException("Difficulty must be between 1 and 5, or unknown.");
        var code = CompetencyCatalog.GroupCode(group);
        if (code == null) throw new ValidationException("Choose a supported competency group.");
        var levelName = CompetencyCatalog.LevelName(level);
        if (levelName == null) throw new ValidationException("Choose Basic, Intermediate, or Advanced.");
        var eligible = context.Skills.Include(s => s.Domain)
            .Where(s => s.Domain.Framework.IsActive && s.Domain.Code == code);
        var skill = skillId.HasValue
            ? await eligible.SingleOrDefaultAsync(s => s.Id == skillId)
            : await eligible.SingleOrDefaultAsync(s => s.IsDefault);
        if (skill == null) throw new ValidationException("The selected skill must belong to this group in the active framework.");
        return new QuestionClassification(skill.Id, skill.Domain.Name, levelName);
    }

    public static string NormalizeType(string type) => type switch
    {
        "single_choice" => "mcq_single",
        "multi_choice" => "mcq_multi",
        "tf" => "true_false",
        "short" => "short_answer",
        "case" => "scenario",
        _ => type
    };
}

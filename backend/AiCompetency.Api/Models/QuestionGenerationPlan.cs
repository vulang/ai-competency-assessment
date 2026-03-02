using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace AiCompetency.Api.Models;

[Table("question_generation_plans")]
public class QuestionGenerationPlan
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("plan_json", TypeName = "jsonb")]
    [Required]
    public JsonDocument PlanJson { get; set; } = JsonDocument.Parse("{}");

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

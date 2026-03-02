using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiCompetency.Api.Models;

[Table("exam_questions")]
public class ExamQuestion
{
    [Key]
    [Column("eq_id")]
    public int Id { get; set; }

    [Column("exam_id")]
    public int ExamId { get; set; }

    [Column("question_id")]
    public int QuestionId { get; set; }

    [Column("order_index")]
    public int OrderIndex { get; set; }

    [Column("point_value")]
    public decimal PointValue { get; set; } = 1.0m;

    public Exam? Exam { get; set; }
    public Question? Question { get; set; }
}

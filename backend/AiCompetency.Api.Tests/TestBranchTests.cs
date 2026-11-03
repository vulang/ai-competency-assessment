using System.Net;
using System.Security.Claims;
using System.Text.Json;
using AiCompetency.Api.Controllers;
using AiCompetency.Api.Data;
using AiCompetency.Api.DTOs;
using AiCompetency.Api.Models;
using AiCompetency.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AiCompetency.Api.Tests;

public class TestBranchTests
{
    private static readonly ScoringService Scoring = new();

    private static async Task<ApplicationDbContext> CreateDbAsync()
    {
        var db = new TestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static T As<T>(T controller, string userId, string? email = null) where T : ControllerBase
    {
        var claims = new List<Claim> { new("sub", userId) };
        if (email != null) claims.Add(new Claim("email", email));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) }
        };
        return controller;
    }

    private static Question Item(string group, int level, string answer = "A", string type = "mcq_single",
        QuestionStatus status = QuestionStatus.Published, bool reviewed = true) => new()
    {
        Content = $"{group} {level}",
        Type = type,
        GroupCode = group,
        TargetLevel = level,
        Status = status,
        ReviewedBy = reviewed ? "expert@example.com" : null,
        ReviewedAt = reviewed ? DateTimeOffset.UtcNow : null,
        Metadata = JsonDocument.Parse(type == "true_false"
            ? $"{{\"answer\":{answer}}}"
            : $"{{\"answer\":\"{answer}\",\"options\":[\"A. One\",\"B. Two\",\"C. Three\"]}}")
    };

    private static IEnumerable<Question> Bank(int perLevel = 2) =>
        TestBlueprints.GroupCodes.SelectMany(g => Enumerable.Range(1, 3).SelectMany(l => Enumerable.Range(0, perLevel).Select(_ => Item(g, l))));

    // ── Scoring ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("mcq_single", "{\"answer\":\"B\",\"options\":[\"A. x\",\"B. y\"]}", "B. y", AnswerOutcome.Correct)]
    [InlineData("mcq_single", "{\"answer\":\"B\",\"options\":[\"A. x\",\"B. y\"]}", "A", AnswerOutcome.Incorrect)]
    [InlineData("mcq_multi", "{\"answer\":[\"A\",\"C\"],\"options\":[\"A. x\",\"B. y\",\"C. z\"]}", "[\"C. z\",\"A. x\"]", AnswerOutcome.Correct)]
    [InlineData("mcq_multi", "{\"answer\":[\"A\",\"C\"],\"options\":[\"A. x\",\"B. y\",\"C. z\"]}", "[\"A. x\"]", AnswerOutcome.Incorrect)]
    [InlineData("true_false", "{\"answer\":false}", "false", AnswerOutcome.Correct)]
    [InlineData("true_false", "{\"answer\":false}", "true", AnswerOutcome.Incorrect)]
    [InlineData("short_answer", "{\"answer\":\"Prompt\"}", " prompt ", AnswerOutcome.Correct)]
    [InlineData("scenario", "{\"answer\":\"x\"}", "A long answer", AnswerOutcome.ManualReview)]
    [InlineData("mcq_single", "{\"answer\":\"A\"}", "  ", AnswerOutcome.Unanswered)]
    public void ScoringServiceHandlesEachQuestionType(string type, string metadata, string answer, AnswerOutcome expected)
    {
        var scored = Scoring.Score(new Question { Type = type, Metadata = JsonDocument.Parse(metadata) }, answer, 2m);
        Assert.Equal(expected, scored.Outcome);
        Assert.Equal(expected == AnswerOutcome.Correct ? 2m : 0m, scored.Score);
    }

    [Fact]
    public void ScoringServiceResolvesOptionLayoutAndSelections()
    {
        var single = new Question { Type = "mcq_single", Metadata = JsonDocument.Parse("{\"answer\":\"C\",\"options\":[\"A. x\",\"B. y\",\"C. z\"]}") };
        Assert.Equal(new[] { 2 }, Scoring.Options(single)!.Key);
        Assert.Equal(3, Scoring.Options(single)!.OptionCount);
        Assert.Equal(new[] { 1 }, Scoring.SelectedOptions(single, "B. y"));
        Assert.Equal(new[] { 1 }, Scoring.SelectedOptions(single, "b"));
        Assert.Empty(Scoring.SelectedOptions(single, "not an option"));
        Assert.Empty(Scoring.SelectedOptions(single, null));

        var multi = new Question { Type = "mcq_multi", Metadata = JsonDocument.Parse("{\"answer\":[\"C\",\"A\"],\"options\":[\"x\",\"y\",\"z\"]}") };
        Assert.Equal(new[] { 0, 2 }, Scoring.Options(multi)!.Key);
        Assert.Equal(new[] { 1, 2 }, Scoring.SelectedOptions(multi, "[\"z\",\"y\"]"));
        Assert.Empty(Scoring.SelectedOptions(multi, "not json"));

        var tf = new Question { Type = "true_false", Metadata = JsonDocument.Parse("{\"answer\":false}") };
        Assert.Equal(2, Scoring.Options(tf)!.OptionCount);
        Assert.Equal(new[] { 1 }, Scoring.Options(tf)!.Key);
        Assert.Equal(new[] { 0 }, Scoring.SelectedOptions(tf, "true"));

        var badKey = new Question { Type = "mcq_single", Metadata = JsonDocument.Parse("{\"answer\":\"E\",\"options\":[\"A. x\",\"B. y\"]}") };
        Assert.Null(Scoring.Options(badKey));
    }

    // ── Group scores and level mapping ───────────────────────────────────────

    [Theory]
    [InlineData(49.99, 5, 1)]
    [InlineData(50, 5, 2)]
    [InlineData(79.99, 5, 2)]
    [InlineData(80, 5, 3)]
    [InlineData(100, 4, null)]
    public void LevelMappingUsesInclusiveLowerBoundsAndMinimumItems(double percent, int items, int? expected)
    {
        Assert.Equal(expected, GroupScoreCalculator.MapLevel((decimal)percent, items, TestLevelMappingCatalog.Draft));
        Assert.Null(GroupScoreCalculator.MapLevel((decimal)percent, items, null));
    }

    [Fact]
    public void GroupScoresCountUnansweredObjectiveItemsAndSkipOpenScenarios()
    {
        var data = new[] { Item("DATA", 1), Item("DATA", 2), Item("DATA", 3), Item("ETHICS", 1), Item("ETHICS", 1, type: "scenario") };
        var scores = GroupScoreCalculator.Compute(Guid.NewGuid(), [
            new ScoredItem(data[0], 1, 1), new ScoredItem(data[1], 1, 0), new ScoredItem(data[2], 2, 2),
            new ScoredItem(data[3], 1, 0), new ScoredItem(data[4], 1, 0)], null);
        var dataScore = scores.Single(s => s.GroupCode == "DATA");
        Assert.Equal((3m, 4m, 3, 75m), (dataScore.RawScore, dataScore.MaxScore, dataScore.ItemCount, dataScore.Percent));
        var ethics = scores.Single(s => s.GroupCode == "ETHICS");
        Assert.Equal(1, ethics.ItemCount);
        Assert.Null(ethics.Level);
        Assert.Equal(7, TestProfileView.SevenGroups(scores).Count);
    }

    // ── Blueprints ───────────────────────────────────────────────────────────

    [Fact]
    public void LevelTargetsUseLargestRemainder()
    {
        var mix = new Dictionary<int, int> { [1] = 30, [2] = 40, [3] = 30 };
        Assert.Equal([(1, 2), (2, 2), (3, 2)], TestBlueprints.LevelTargets(6, mix));
        Assert.Equal([(1, 2), (2, 2), (3, 1)], TestBlueprints.LevelTargets(5, mix));
        Assert.Empty(TestBlueprints.LevelTargets(5, new Dictionary<int, int>()));
    }

    [Fact]
    public void BlueprintValidationAndStandardConformance()
    {
        Assert.Null(TestBlueprints.Validate(new TestBlueprintSpec { GroupQuotas = new() { ["DATA"] = 3 } }));
        Assert.NotNull(TestBlueprints.Validate(new TestBlueprintSpec { GroupQuotas = new() { ["UNKNOWN"] = 3 } }));
        Assert.NotNull(TestBlueprints.Validate(new TestBlueprintSpec { GroupQuotas = new() }));
        Assert.NotNull(TestBlueprints.Validate(new TestBlueprintSpec { GroupQuotas = new() { ["DATA"] = 3 }, LevelMix = new() { [1] = 50, [2] = 40 } }));
        Assert.NotNull(TestBlueprints.Validate(new TestBlueprintSpec { GroupQuotas = new() { ["DATA"] = 3 }, PassPercent = 0 }));

        var five = TestBlueprints.GroupCodes.ToDictionary(g => g, _ => 5);
        Assert.Empty(TestBlueprints.StandardIssues(five));
        Assert.Empty(TestBlueprints.StandardIssues(TestBlueprints.GroupCodes.ToDictionary(g => g, _ => 6)));
        Assert.Single(TestBlueprints.StandardIssues(new Dictionary<string, int>(five) { ["DATA"] = 7 }));
        Assert.Equal(2, TestBlueprints.StandardIssues(new Dictionary<string, int>(five) { ["DATA"] = 4 }).Count);
    }

    [Fact]
    public void AssemblyIsDeterministicAndUsesOnlyEligibleQuestions()
    {
        var pool = Bank(3).ToList();
        var id = 0;
        foreach (var q in pool) q.Id = ++id;
        pool.Add(new Question { Id = 1000, Type = "mcq_single", GroupCode = "DATA", TargetLevel = 1, Status = QuestionStatus.Published });
        var scenario = Item("DATA", 1, type: "scenario");
        scenario.Id = 1001;
        pool.Add(scenario);
        var spec = new TestBlueprintSpec
        {
            GroupQuotas = TestBlueprints.GroupCodes.ToDictionary(g => g, _ => 6),
            LevelMix = new() { [1] = 30, [2] = 40, [3] = 30 },
            Seed = 42
        };
        var first = TestBlueprints.Assemble(spec, pool);
        var again = TestBlueprints.Assemble(spec, pool.AsEnumerable().Reverse());
        Assert.Empty(first.Warnings);
        Assert.Equal(42, first.Questions.Count);
        Assert.Equal(first.Questions.Select(q => q.Id), again.Questions.Select(q => q.Id));
        Assert.DoesNotContain(first.Questions, q => q.Id >= 1000);
        Assert.All(first.Questions.GroupBy(q => q.GroupCode), g =>
            Assert.Equal(new[] { 2, 2, 2 }, g.GroupBy(q => q.TargetLevel).OrderBy(l => l.Key).Select(l => l.Count())));
        var reseeded = TestBlueprints.Assemble(spec with { Seed = 7 }, pool);
        Assert.NotEqual(first.Questions.Select(q => q.Id), reseeded.Questions.Select(q => q.Id));

        var shortBank = TestBlueprints.Assemble(spec with { LevelMix = new() { [3] = 100 } }, pool);
        Assert.Contains(shortBank.Warnings, w => w.Contains("filled"));
        Assert.Equal(42, shortBank.Questions.Count);
        var tooFew = TestBlueprints.Assemble(spec with { GroupQuotas = new() { ["DATA"] = 12 } }, pool);
        Assert.Contains(tooFew.Warnings, w => w.Contains("only 9 of 12"));
    }

    [Fact]
    public async Task StandardFormLifecycleEnforcesQuotaReviewAndSingleActiveStandard()
    {
        await using var db = await CreateDbAsync();
        db.Questions.AddRange(Bank());
        var unreviewed = Item("DATA", 1, reviewed: false);
        db.Questions.Add(unreviewed);
        await db.SaveChangesAsync();
        var tests = new TestsController(db);
        var request = new SaveTestRequest
        {
            Name = "Standard form A", IsStandard = true, Seed = 11, PassPercent = 60,
            GroupQuotas = TestBlueprints.GroupCodes.ToDictionary(g => g, _ => 5)
        };

        var preview = Assert.IsType<TestPreviewDto>(Assert.IsType<OkObjectResult>((await tests.Preview(request)).Result).Value);
        Assert.Equal(35, preview.Questions.Count);
        Assert.Empty(preview.StandardIssues);
        var created = Assert.IsType<TestDetailDto>(Assert.IsType<CreatedAtActionResult>((await tests.CreateTest(request)).Result).Value);
        Assert.Equal(preview.Questions.Select(q => q.QuestionId), created.Questions.Select(q => q.QuestionId));
        var id = int.Parse(created.Id);

        var ids = created.Questions.Select(q => q.QuestionId).ToList();
        Assert.IsType<BadRequestObjectResult>((await tests.SetQuestions(id, new([.. ids, unreviewed.Id]))).Result);
        Assert.IsType<BadRequestObjectResult>((await tests.SetQuestions(id, new([ids[0], ids[0]]))).Result);
        var trimmed = Assert.IsType<TestDetailDto>(Assert.IsType<OkObjectResult>((await tests.SetQuestions(id, new(ids.Skip(1).Reverse().ToList()))).Result).Value);
        Assert.Equal(ids.Last(), trimmed.Questions[0].QuestionId);
        Assert.NotEmpty(trimmed.StandardIssues);
        Assert.IsType<BadRequestObjectResult>((await tests.Publish(id)).Result);

        Assert.IsType<OkObjectResult>((await tests.Assemble(id, new(null))).Result);
        var published = Assert.IsType<TestDetailDto>(Assert.IsType<OkObjectResult>((await tests.Publish(id)).Result).Value);
        Assert.Equal("active", published.Status);
        Assert.Equal(21, published.PassingScore);
        Assert.IsType<ConflictObjectResult>((await tests.SetQuestions(id, new(ids))).Result);
        Assert.IsType<ConflictObjectResult>((await tests.UpdateTest(id, request)).Result);

        var clone = Assert.IsType<TestDetailDto>(Assert.IsType<CreatedAtActionResult>((await tests.Clone(id)).Result).Value);
        Assert.Equal("draft", clone.Status);
        Assert.Equal(35, clone.Questions.Count);
        Assert.IsType<ConflictObjectResult>((await tests.Publish(int.Parse(clone.Id))).Result);
        Assert.IsType<OkObjectResult>((await tests.Archive(id)).Result);
        Assert.IsType<OkObjectResult>((await tests.Publish(int.Parse(clone.Id))).Result);
        Assert.IsType<ConflictObjectResult>(await tests.DeleteTest(id));
    }

    // ── Question review ──────────────────────────────────────────────────────

    [Fact]
    public async Task QuestionsNeedReviewBeforePublishingAndEditsResetReview()
    {
        await using var db = await CreateDbAsync();
        var questions = As(new QuestionsController(db, new QuestionClassificationService(db), Scoring), "expert-id", "expert@example.com");
        var dto = new QuestionDto
        {
            Title = "Review", Prompt = "Choose", Type = "mcq_single", CompetencyGroupId = "DATA", CompetencyLevelId = "Basic",
            Options = ["A. Yes", "B. No"], Answer = "A", Status = "published"
        };
        Assert.IsType<ConflictObjectResult>(await questions.CreateQuestion(dto));
        dto.Status = "draft";
        var id = int.Parse(Assert.IsType<QuestionDto>(Assert.IsType<CreatedAtActionResult>(await questions.CreateQuestion(dto)).Value).Id);
        dto.Status = "published";
        Assert.IsType<ConflictObjectResult>(await questions.UpdateQuestion(id, dto));

        var reviewed = Assert.IsType<QuestionDto>(Assert.IsType<OkObjectResult>(await questions.ReviewQuestion(id, new("Clear and keyed"))).Value);
        Assert.Equal(("reviewed", "expert@example.com", "Clear and keyed"), (reviewed.Status, reviewed.ReviewedBy, reviewed.ReviewNote));
        var published = Assert.IsType<QuestionDto>(Assert.IsType<OkObjectResult>(await questions.UpdateQuestion(id, dto)).Value);
        Assert.True(published.Eligible);

        // Metadata-only edits keep the review; a changed key does not.
        dto.Tags = ["tagged"];
        Assert.Equal("published", Assert.IsType<QuestionDto>(Assert.IsType<OkObjectResult>(await questions.UpdateQuestion(id, dto)).Value).Status);
        dto.Answer = "B";
        dto.Status = "published";
        Assert.IsType<ConflictObjectResult>(await questions.UpdateQuestion(id, dto));
        var question = await db.Questions.AsNoTracking().SingleAsync();
        Assert.Equal((QuestionStatus.Published, "A"), (question.Status, question.Metadata.RootElement.GetProperty("answer").GetString()));

        db.ChangeTracker.Clear();
        dto.Status = "draft";
        var edited = Assert.IsType<QuestionDto>(Assert.IsType<OkObjectResult>(await questions.UpdateQuestion(id, dto)).Value);
        Assert.Equal("draft", edited.Status);
        Assert.Null(edited.ReviewedBy);
    }

    [Fact]
    public async Task ReviewRequiresResolvableKeyAndDeliveredQuestionsAreLocked()
    {
        await using var db = await CreateDbAsync();
        var badKey = Item("DATA", 1, answer: "E", status: QuestionStatus.Draft, reviewed: false);
        var delivered = Item("DATA", 1);
        db.Questions.AddRange(badKey, delivered);
        db.ExamResults.Add(new ExamResult { UserId = "u", Status = "Completed", Exam = new Exam { Title = "T" },
            Responses = [new Response { Question = delivered, FinalAnswer = "A" }] });
        await db.SaveChangesAsync();
        var questions = As(new QuestionsController(db, new QuestionClassificationService(db), Scoring), "expert");
        Assert.IsType<BadRequestObjectResult>(await questions.ReviewQuestion(badKey.Id, new(null)));
        Assert.IsType<ConflictObjectResult>(await questions.DeleteQuestion(delivered.Id));
        Assert.IsType<ConflictObjectResult>(await questions.UpdateQuestion(delivered.Id, new QuestionDto
        {
            Prompt = "Changed", Type = "mcq_single", CompetencyGroupId = "DATA", CompetencyLevelId = "Basic",
            Options = ["A. One", "B. Two", "C. Three"], Answer = "A", Status = "published"
        }));
        var coverage = JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(await questions.GetCoverage()).Value);
        var cell = coverage.GetProperty("Cells").EnumerateArray().Single(c => c.GetProperty("GroupCode").GetString() == "DATA" && c.GetProperty("Level").GetInt32() == 1);
        Assert.Equal((2, 1, 1), (cell.GetProperty("Total").GetInt32(), cell.GetProperty("Draft").GetInt32(), cell.GetProperty("Eligible").GetInt32()));
    }

    // ── Candidate flow ───────────────────────────────────────────────────────

    [Fact]
    public async Task StandardSubmissionStoresGroupScoresAndMappingBackfillsLevels()
    {
        await using var db = await CreateDbAsync();
        var candidate = As(new CandidateTestsController(db, new ProfileJobQueue(enabled: false), Scoring), "alice");
        Assert.IsType<NotFoundObjectResult>(await candidate.StartStandardTest());

        db.Questions.AddRange(Bank(perLevel: 2));
        await db.SaveChangesAsync();
        var tests = new TestsController(db);
        var created = Assert.IsType<TestDetailDto>(Assert.IsType<CreatedAtActionResult>((await tests.CreateTest(new SaveTestRequest
        {
            Name = "Standard", IsStandard = true, Seed = 3, GroupQuotas = TestBlueprints.GroupCodes.ToDictionary(g => g, _ => 5)
        })).Result).Value);
        Assert.IsType<OkObjectResult>((await tests.Publish(int.Parse(created.Id))).Result);

        var started = JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(await candidate.StartStandardTest()).Value);
        var sessionId = started.GetProperty("SessionId").GetGuid();
        // Answer DATA correctly on four of five items, skip everything else.
        var data = created.Questions.Where(q => q.GroupCode == "DATA").Take(4)
            .Select(q => new CandidateTestsController.QuestionResponse { QuestionId = q.QuestionId, Answer = "A" }).ToList();
        Assert.IsType<OkObjectResult>(await candidate.SubmitTest(sessionId, new() { Responses = data }));

        var scores = await db.TestGroupScores.Where(s => s.SessionId == sessionId).ToListAsync();
        Assert.Equal(7, scores.Count);
        Assert.Equal(80m, scores.Single(s => s.GroupCode == "DATA").Percent);
        Assert.All(scores, s => Assert.Null(s.LevelMappingVersion));

        var mappings = new TestLevelMappingsController(db);
        Assert.IsType<BadRequestObjectResult>(await mappings.Publish("tl-v1-draft", new("", null)));
        var published = JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(await mappings.Publish("tl-v1-draft", new("Expert panel", "Approved"))).Value);
        Assert.Equal(7, published.GetProperty("BackfilledGroupScores").GetInt32());
        db.ChangeTracker.Clear();
        scores = await db.TestGroupScores.Where(s => s.SessionId == sessionId).ToListAsync();
        Assert.Equal(3, scores.Single(s => s.GroupCode == "DATA").Level);
        Assert.Equal(1, scores.Single(s => s.GroupCode == "ETHICS").Level);
        Assert.All(scores, s => Assert.Equal("tl-v1-draft", s.LevelMappingVersion));
        Assert.IsType<ConflictObjectResult>(await mappings.Update("tl-v1-draft", new("tl-v1-draft", 40, 70, 5, null)));

        // A later version applies to new submissions only.
        Assert.IsType<CreatedAtActionResult>(await mappings.Create(new("tl-v2", 40, 70, 5, "Recalibrated")));
        Assert.IsType<OkObjectResult>(await mappings.Publish("tl-v2", new("Expert panel", null)));
        Assert.Equal("tl-v2", (await db.TestLevelMappings.SingleAsync(m => m.IsActive)).Version);
        Assert.All(await db.TestGroupScores.ToListAsync(), s => Assert.Equal("tl-v1-draft", s.LevelMappingVersion));

        var result = JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(await candidate.GetTestResult(sessionId)).Value);
        Assert.Equal(7, result.GetProperty("GroupScores").GetArrayLength());
        var profileController = As(new CompetencyProfileController(db, new CompetencyProfileService(db,
            new MlService(new HttpClient(), NullLogger<MlService>.Instance), NullLogger<CompetencyProfileService>.Instance)), "alice");
        var profile = JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(await profileController.GetMyTestProfile()).Value);
        Assert.Equal(sessionId, profile.GetProperty("SessionId").GetGuid());
        As(profileController, "bob");
        Assert.IsType<NotFoundObjectResult>(await profileController.GetMyTestProfile());
    }

    [Fact]
    public void DisabledProfileQueueDropsJobs()
    {
        var queue = new ProfileJobQueue(enabled: false);
        queue.Enqueue(Guid.NewGuid());
        Assert.False(queue.ReadAllAsync(CancellationToken.None).GetAsyncEnumerator().MoveNextAsync().IsCompleted);
    }

    // ── Item analysis ────────────────────────────────────────────────────────

    [Fact]
    public async Task ItemAnalysisSendsScoredMatrixWithSelectionsAndExportsCsv()
    {
        await using var db = await CreateDbAsync();
        var q1 = Item("DATA", 1, answer: "B");
        var q2 = Item("ETHICS", 2, answer: "true", type: "true_false");
        var open = Item("ETHICS", 2, type: "scenario");
        var exam = new Exam { Title = "Form", Status = "active", ExamQuestions = [
            new ExamQuestion { Question = q1, OrderIndex = 1, PointValue = 1 },
            new ExamQuestion { Question = q2, OrderIndex = 2, PointValue = 1 },
            new ExamQuestion { Question = open, OrderIndex = 3, PointValue = 1 }] };
        db.Exams.Add(exam);
        db.ExamResults.AddRange(
            new ExamResult { UserId = "a", Exam = exam, Status = "Completed", EndTime = DateTime.UtcNow.AddMinutes(-2),
                Responses = [new Response { Question = q1, FinalAnswer = "B. Two", ScoreEarned = 1 }, new Response { Question = q2, FinalAnswer = "false" }] },
            new ExamResult { UserId = "b", Exam = exam, Status = "Completed", EndTime = DateTime.UtcNow.AddMinutes(-1),
                Responses = [new Response { Question = q1, FinalAnswer = "C. Three" }] },
            new ExamResult { UserId = "c", Exam = exam, Status = "InProgress" });
        await db.SaveChangesAsync();

        var handler = new CttHandler();
        var service = new ItemAnalysisService(db, new MlService(new HttpClient(handler) { BaseAddress = new Uri("http://ml.test") },
            NullLogger<MlService>.Instance), Scoring);
        var report = await service.AnalyzeAsync(exam.Id);
        Assert.NotNull(report);
        Assert.Equal(1, report!.ExcludedNonObjective);
        using var sent = JsonDocument.Parse(handler.Body!);
        var items = sent.RootElement.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal("[1]", items[0].GetProperty("key").GetRawText());
        Assert.Equal(3, items[0].GetProperty("option_count").GetInt32());
        Assert.Equal("[0]", items[1].GetProperty("key").GetRawText());
        var respondents = sent.RootElement.GetProperty("respondents");
        Assert.Equal(2, respondents.GetArrayLength());
        Assert.Equal("[1,0]", respondents[0].GetProperty("scores").GetRawText());
        Assert.Equal("[[1],[1]]", respondents[0].GetProperty("selections").GetRawText());
        Assert.Equal("[[2],[]]", respondents[1].GetProperty("selections").GetRawText());

        var csv = ItemAnalysisService.ItemsCsv(report).Split('\n');
        Assert.StartsWith("question_id,order,group,level,type,title,n,p,", csv[0]);
        Assert.Contains("option_C", csv[0]);
        Assert.StartsWith($"{q1.Id},1,DATA,1,mcq_single,", csv[1]);
        Assert.Contains("too_hard", csv[1]);
        var matrix = await service.LoadMatrixAsync(exam.Id);
        var responses = ItemAnalysisService.ResponsesCsv(matrix!).Split('\n');
        Assert.Equal($"session_id,completed_at,q{q1.Id},q{q2.Id},total", responses[0].TrimEnd('\r'));
        Assert.EndsWith(",1,0,1", responses[1].TrimEnd('\r'));
        Assert.Contains("TOTAL,2,2,", ItemAnalysisService.ReliabilityCsv(report));

        var unavailable = new ItemAnalysisService(db, new MlService(new HttpClient(new FailingHandler()) { BaseAddress = new Uri("http://ml.test") },
            NullLogger<MlService>.Instance), Scoring);
        await Assert.ThrowsAsync<HttpRequestException>(() => unavailable.AnalyzeAsync(exam.Id));
        Assert.Null(await service.AnalyzeAsync(9999));
    }

    private sealed class CttHandler : HttpMessageHandler
    {
        public string? Body { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var payload = JsonDocument.Parse(Body);
            var ids = payload.RootElement.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("item_id").GetString()).ToList();
            var items = string.Join(',', ids.Select(id => $$"""
                {"item_id":"{{id}}","group":"DATA","n":2,"p":0.5,"item_rest_r":null,"item_rest_r_group":null,
                 "discrimination_index":1,"alpha_if_deleted":null,"omit_rate":0,"flags":["too_hard"],
                 "distractors":[{"option":0,"is_key":false,"proportion":0,"upper":0,"lower":0,"option_rest_r":null,"flags":["nonfunctional"]},
                                {"option":1,"is_key":true,"proportion":0.5,"upper":1,"lower":0,"option_rest_r":null,"flags":[]},
                                {"option":2,"is_key":false,"proportion":0.5,"upper":0,"lower":1,"option_rest_r":null,"flags":[]}]}
                """));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$$"""
                    {"n_respondents":2,"n_items":2,"total":{"mean":1,"sd":0,"min":1,"max":1,"histogram":[{"score":1,"count":2}]},
                     "reliability":{"n_items":2,"alpha":null,"omega":null,"excluded_items":[],"notes":["n too small"]},
                     "groups":[],"items":[{{{items}}}],
                     "thresholds":{"min_difficulty":0.2,"max_difficulty":0.9,"min_discrimination":0.2,"min_distractor_rate":0.05}}
                    """)
            };
        }
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
    }
}

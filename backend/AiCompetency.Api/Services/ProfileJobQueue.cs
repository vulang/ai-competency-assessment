using System.Threading.Channels;
using AiCompetency.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AiCompetency.Api.Services;

// Phase 0 uses an in-process queue. Jobs are best effort and are lost on restart.
public sealed class ProfileJobQueue
{
    private readonly Channel<Guid> _jobs = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true,
        AllowSynchronousContinuations = false
    });

    public void Enqueue(Guid sessionId) => _jobs.Writer.TryWrite(sessionId);
    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken token) => _jobs.Reader.ReadAllAsync(token);
}

public sealed class ProfileJobWorker(
    ProfileJobQueue queue,
    IServiceScopeFactory scopes,
    ILogger<ProfileJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var sessionId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var session = await context.ExamResults.Include(s => s.Responses).ThenInclude(r => r.Question)
                    .SingleOrDefaultAsync(s => s.Id == sessionId && s.Status == "Completed", stoppingToken);
                if (session == null) continue;
                var responses = session.Responses.Where(r => r.Question!.Type != "scenario").Select(r => (
                    QuestionId: r.QuestionId,
                    IsCorrect: r.ScoreEarned > 0,
                    DifficultyLevel: r.Question!.DifficultyLevel,
                    SkillId: r.Question!.SkillId)).ToList();
                await scope.ServiceProvider.GetRequiredService<CompetencyProfileService>()
                    .ProcessSessionAsync(session.Id, session.UserId, responses);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Profile processing failed for session {SessionId}", sessionId); }
        }
    }
}

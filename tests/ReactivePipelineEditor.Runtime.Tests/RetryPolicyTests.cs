using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using ReactivePipelineEditor.Runtime.Retry;

namespace ReactivePipelineEditor.Runtime.Tests;

public sealed class RetryPolicyTests
{
    private readonly FakeTimeProvider _time = new();
    private readonly NullLogger _logger = NullLogger.Instance;

    [Fact]
    public async Task Succeeds_on_first_attempt()
    {
        var policy = new RetryPolicy(timeProvider: _time);
        var result = await policy.ExecuteAsync(_ => Task.FromResult(42), _logger, CancellationToken.None);
        result.Should().Be(42);
    }

    [Fact]
    public async Task Retries_and_succeeds_on_second_attempt()
    {
        var policy = new RetryPolicy(maxAttempts: 3, timeProvider: _time);
        var callCount = 0;

        var result = await policy.ExecuteAsync(_ =>
        {
            callCount++;
            if (callCount == 1) throw new InvalidOperationException("first fail");
            return Task.FromResult(42);
        }, _logger, CancellationToken.None);

        result.Should().Be(42);
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task Throws_after_max_attempts()
    {
        var policy = new RetryPolicy(maxAttempts: 3, timeProvider: _time);
        var callCount = 0;

        Func<Task> act = () => policy.ExecuteAsync<int>(_ =>
        {
            callCount++;
            throw new InvalidOperationException("always fail");
        }, _logger, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*3 attempts*");
        callCount.Should().Be(3);
    }

    [Theory]
    [InlineData(2, 200)]
    [InlineData(3, 400)]
    [InlineData(4, 800)]
    public void GetDelay_returns_exponential_backoff(int attempt, int expectedMs)
    {
        var policy = new RetryPolicy(
            baseDelay: TimeSpan.FromMilliseconds(200),
            backoffMultiplier: 2.0,
            timeProvider: _time);

        policy.GetDelay(attempt).TotalMilliseconds.Should().Be(expectedMs);
    }

    [Fact]
    public void GetDelay_caps_at_max_delay()
    {
        var policy = new RetryPolicy(
            baseDelay: TimeSpan.FromSeconds(1),
            backoffMultiplier: 10.0,
            maxDelay: TimeSpan.FromSeconds(5),
            timeProvider: _time);

        policy.GetDelay(10).Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GetDelay_returns_zero_for_first_attempt()
    {
        var policy = new RetryPolicy(timeProvider: _time);
        policy.GetDelay(1).Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task Does_not_retry_on_cancellation()
    {
        var policy = new RetryPolicy(maxAttempts: 3, timeProvider: _time);
        var callCount = 0;

        Func<Task> act = () => policy.ExecuteAsync<int>(_ =>
        {
            callCount++;
            throw new OperationCanceledException();
        }, _logger, CancellationToken.None);

        await act.Should().ThrowAsync<OperationCanceledException>();
        callCount.Should().Be(1);
    }
}

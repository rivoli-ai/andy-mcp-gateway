namespace McpGateway.Application.Proxying;

/// <summary>
/// Shared retry policy for transient proxy operations.
/// </summary>
public static class McpProxyRetry
{
    private const int DefaultRetryAttempts = 2;
    private const int BaseRetryDelayMs = 100;

    public static async Task ExecuteWithRetryAsync(Func<Task> operation, int maxRetries = DefaultRetryAttempts)
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                await operation().ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                lastException = ex;
                await DelayBeforeRetry(attempt).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }

        if (lastException != null)
        {
            throw lastException;
        }
    }

    public static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, T failureResult, int maxRetries = DefaultRetryAttempts)
    {
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch when (attempt < maxRetries)
            {
                await DelayBeforeRetry(attempt).ConfigureAwait(false);
            }
        }

        return failureResult;
    }

    private static async Task DelayBeforeRetry(int attemptNumber)
    {
        var delayMs = (int)(Math.Pow(2, attemptNumber) * BaseRetryDelayMs);
        await Task.Delay(TimeSpan.FromMilliseconds(delayMs)).ConfigureAwait(false);
    }
}

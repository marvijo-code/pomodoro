using System;
using System.Collections.Generic;
using System.Linq;

namespace UnoPomodoro.Services;

public static class CompletionVibrationPattern
{
    private const int PulseMilliseconds = 400;
    private const int GapMilliseconds = 200;

    public static long[] Build(int durationSeconds)
    {
        var targetMilliseconds = Math.Max(1, durationSeconds) * 1000;
        var pattern = new List<long> { 0 };
        var emittedMilliseconds = 0;

        while (emittedMilliseconds < targetMilliseconds)
        {
            var pulse = Math.Min(PulseMilliseconds, targetMilliseconds - emittedMilliseconds);
            pattern.Add(pulse);
            emittedMilliseconds += pulse;

            if (emittedMilliseconds >= targetMilliseconds)
            {
                break;
            }

            var gap = Math.Min(GapMilliseconds, targetMilliseconds - emittedMilliseconds);
            pattern.Add(gap);
            emittedMilliseconds += gap;
        }

        return pattern.Select(value => (long)value).ToArray();
    }

    public static int GetDurationMilliseconds(long[] pattern)
    {
        if (pattern == null || pattern.Length == 0)
        {
            return 0;
        }

        var total = 0L;
        foreach (var entry in pattern)
        {
            total += Math.Max(0L, entry);
        }

        return total > int.MaxValue ? int.MaxValue : (int)total;
    }
}

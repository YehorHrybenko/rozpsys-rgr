namespace Orchestrator.Services;

public class RangeSplitter
{
    public static List<(int Start, int End)> SplitRange(int start, int end, int splits)
    {
        if (splits <= 0)
        {
            throw new ArgumentException("Number of splits must be greater than zero.", nameof(splits));
        }

        if (start > end)
        {
            throw new ArgumentException("Start must be less than or equal to end.", nameof(start));
        }

        List<(int Start, int End)> segments = [];
        int totalRange = end - start;
        int segmentSize = totalRange / splits;
        int remainder = totalRange % splits;

        int currentStart = start;

        for (int i = 0; i < splits; i++)
        {
            int currentEnd = currentStart + segmentSize + (i < remainder ? 1 : 0);
            segments.Add((currentStart, currentEnd));
            currentStart = currentEnd + 1;
        }

        return segments;
    }
}

namespace Lab1.Exemplar;

/// <summary>
/// Typical AI-generated task manager for Lab 1 review.
/// The quality issues marked below are intentional.
/// </summary>
public class TaskManager
{
    public Dictionary<string, TaskRecord> Tasks { get; } = new();
    public Dictionary<string, object> Resources { get; } = new();

    public void AddTask(string taskId, string name, List<string> dependencies, int duration, List<string> resourcesNeeded)
    {
        // ISSUE: No input validation
        Tasks[taskId] = new TaskRecord
        {
            Name = name,
            Dependencies = dependencies,
            Duration = duration,
            Resources = resourcesNeeded,
            StartDate = null
        };
    }

    public bool DetectCircularDependencies()
    {
        // ISSUE: Inefficient O(n²) algorithm, doesn't handle all cases
        foreach (var taskId in Tasks.Keys)
        {
            var visited = new List<string>();
            if (HasCycle(taskId, visited))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasCycle(string taskId, List<string> visited)
    {
        // ISSUE: Incorrect cycle detection logic
        if (visited.Contains(taskId))
        {
            return true;
        }

        visited.Add(taskId);
        foreach (var dependency in Tasks[taskId].Dependencies)
        {
            if (HasCycle(dependency, visited))
            {
                return true;
            }
        }

        return false;
    }

    public int CalculateEarliestStart(string taskId)
    {
        // ISSUE: No memoization, exponential time complexity
        if (Tasks[taskId].Dependencies.Count == 0)
        {
            return 0;
        }

        var maxEnd = 0;
        foreach (var dependency in Tasks[taskId].Dependencies)
        {
            var dependencyStart = CalculateEarliestStart(dependency);
            var dependencyEnd = dependencyStart + Tasks[dependency].Duration;
            maxEnd = Math.Max(maxEnd, dependencyEnd);
        }

        return maxEnd;
    }
}

public class TaskRecord
{
    public string? Name { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public int Duration { get; set; }
    public List<string>? Resources { get; set; }
    public DateTime? StartDate { get; set; }
}

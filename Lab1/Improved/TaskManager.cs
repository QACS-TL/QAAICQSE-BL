namespace Lab1.Improved;

public sealed class Task
{
    public required string TaskId { get; init; }
    public required string Name { get; init; }
    public required List<string> Dependencies { get; init; }
    public required int Duration { get; init; }
    public required List<string> ResourcesNeeded { get; init; }
    public DateTime? StartDate { get; init; }
    public int? EarliestStart { get; set; }
}

public sealed class TaskManagerException : Exception
{
    public TaskManagerException(string message) : base(message)
    {
    }
}

/// <summary>
/// Reference solution for Lab 1: validated input, correct cycle detection, and a memoized schedule.
/// </summary>
public sealed class TaskManager
{
    private readonly Dictionary<string, int> _earliestStartCache = new();

    public Dictionary<string, Task> Tasks { get; } = new();
    public Dictionary<string, int> Resources { get; } = new();

    /// <summary>Add a task with proper validation.</summary>
    public void AddTask(string taskId, string name, List<string> dependencies, int duration, List<string> resourcesNeeded)
    {
        if (string.IsNullOrEmpty(taskId))
        {
            throw new TaskManagerException("Task ID must be a non-empty string");
        }

        if (Tasks.ContainsKey(taskId))
        {
            throw new TaskManagerException($"Task {taskId} already exists");
        }

        if (duration <= 0)
        {
            throw new TaskManagerException("Duration must be positive");
        }

        if (string.IsNullOrEmpty(name))
        {
            throw new TaskManagerException("Task name must be a non-empty string");
        }

        foreach (var dependency in dependencies)
        {
            if (!Tasks.ContainsKey(dependency) && dependency != taskId)
            {
                Log.Warning($"Dependency {dependency} for task {taskId} not found");
            }
        }

        Tasks[taskId] = new Task
        {
            TaskId = taskId,
            Name = name,
            Dependencies = dependencies,
            Duration = duration,
            ResourcesNeeded = resourcesNeeded
        };

        _earliestStartCache.Clear();
        Log.Info($"Added task {taskId} with {dependencies.Count} dependencies");
    }

    /// <summary>Cycle detection using depth-first search with coloring.</summary>
    public bool DetectCircularDependencies()
    {
        const int White = 0;
        const int Gray = 1;
        const int Black = 2;
        var colors = Tasks.Keys.ToDictionary(taskId => taskId, _ => White);

        bool HasCycle(string taskId)
        {
            if (colors[taskId] == Gray)
            {
                return true;
            }

            if (colors[taskId] == Black)
            {
                return false;
            }

            colors[taskId] = Gray;
            foreach (var dependency in Tasks[taskId].Dependencies)
            {
                if (Tasks.ContainsKey(dependency) && HasCycle(dependency))
                {
                    return true;
                }
            }

            colors[taskId] = Black;
            return false;
        }

        foreach (var taskId in Tasks.Keys)
        {
            if (colors[taskId] == White && HasCycle(taskId))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Calculate earliest start time with memoization.</summary>
    public int CalculateEarliestStart(string taskId)
    {
        if (!Tasks.ContainsKey(taskId))
        {
            throw new TaskManagerException($"Task {taskId} not found");
        }

        if (_earliestStartCache.TryGetValue(taskId, out var cached))
        {
            return cached;
        }

        var task = Tasks[taskId];
        if (task.Dependencies.Count == 0)
        {
            _earliestStartCache[taskId] = 0;
            return 0;
        }

        var maxEnd = 0;
        foreach (var dependency in task.Dependencies)
        {
            if (Tasks.ContainsKey(dependency))
            {
                var dependencyStart = CalculateEarliestStart(dependency);
                var dependencyEnd = dependencyStart + Tasks[dependency].Duration;
                maxEnd = Math.Max(maxEnd, dependencyEnd);
            }
        }

        _earliestStartCache[taskId] = maxEnd;
        return maxEnd;
    }

    /// <summary>Find the longest path through the project.</summary>
    public List<string> GetCriticalPath()
    {
        if (DetectCircularDependencies())
        {
            throw new TaskManagerException("Cannot calculate critical path: circular dependencies detected");
        }

        var successors = Tasks.Keys.ToDictionary(taskId => taskId, _ => new List<string>());
        foreach (var (taskId, task) in Tasks)
        {
            foreach (var dependency in task.Dependencies)
            {
                if (successors.ContainsKey(dependency))
                {
                    successors[dependency].Add(taskId);
                }
            }
        }

        var endTasks = successors.Where(pair => pair.Value.Count == 0).Select(pair => pair.Key).ToList();
        if (endTasks.Count == 0)
        {
            return new List<string>();
        }

        var latestEndTask = endTasks[0];
        var latestFinish = CalculateEarliestStart(latestEndTask) + Tasks[latestEndTask].Duration;
        for (var index = 1; index < endTasks.Count; index++)
        {
            var finish = CalculateEarliestStart(endTasks[index]) + Tasks[endTasks[index]].Duration;
            if (finish > latestFinish)
            {
                latestFinish = finish;
                latestEndTask = endTasks[index];
            }
        }

        return FindPath(latestEndTask);
    }

    private List<string> FindPath(string taskId)
    {
        var task = Tasks[taskId];
        if (task.Dependencies.Count == 0)
        {
            return new List<string> { taskId };
        }

        var taskStart = CalculateEarliestStart(taskId);
        string? criticalDependency = null;
        foreach (var dependency in task.Dependencies)
        {
            if (Tasks.ContainsKey(dependency))
            {
                var dependencyEnd = CalculateEarliestStart(dependency) + Tasks[dependency].Duration;
                if (dependencyEnd == taskStart)
                {
                    criticalDependency = dependency;
                    break;
                }
            }
        }

        if (criticalDependency is not null)
        {
            var path = FindPath(criticalDependency);
            path.Add(taskId);
            return path;
        }

        return new List<string> { taskId };
    }

    private static class Log
    {
        public static void Info(string message) => Console.Error.WriteLine($"INFO: {message}");
        public static void Warning(string message) => Console.Error.WriteLine($"WARNING: {message}");
    }
}

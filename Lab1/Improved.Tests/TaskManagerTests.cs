namespace Lab1.Improved.Tests;

public class TaskManagerTests
{
    [Fact]
    public void AddTask_StoresTaskWithProvidedValues()
    {
        var manager = new TaskManager();

        manager.AddTask("T1", "Design", new List<string>(), 3, new List<string> { "Dev" });

        Assert.True(manager.Tasks.ContainsKey("T1"));
        var task = manager.Tasks["T1"];
        Assert.Equal("T1", task.TaskId);
        Assert.Equal("Design", task.Name);
        Assert.Equal(3, task.Duration);
        Assert.Empty(task.Dependencies);
        Assert.Equal(new List<string> { "Dev" }, task.ResourcesNeeded);
        Assert.Null(task.StartDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddTask_WhenTaskIdIsInvalid_ThrowsTaskManagerException(string? taskId)
    {
        var manager = new TaskManager();

        var exception = Assert.Throws<TaskManagerException>(() =>
            manager.AddTask(taskId!, "Design", new List<string>(), 3, new List<string>()));

        Assert.Contains("Task ID", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddTask_WhenNameIsInvalid_ThrowsTaskManagerException(string? name)
    {
        var manager = new TaskManager();

        var exception = Assert.Throws<TaskManagerException>(() =>
            manager.AddTask("T1", name!, new List<string>(), 3, new List<string>()));

        Assert.Contains("Task name", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddTask_WhenDurationIsNotPositive_ThrowsTaskManagerException(int duration)
    {
        var manager = new TaskManager();

        var exception = Assert.Throws<TaskManagerException>(() =>
            manager.AddTask("T1", "Design", new List<string>(), duration, new List<string>()));

        Assert.Contains("Duration", exception.Message);
    }

    [Fact]
    public void AddTask_WhenTaskIdAlreadyExists_ThrowsTaskManagerException()
    {
        var manager = new TaskManager();
        manager.AddTask("T1", "Design", new List<string>(), 3, new List<string>());

        var exception = Assert.Throws<TaskManagerException>(() =>
            manager.AddTask("T1", "Build", new List<string>(), 2, new List<string>()));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public void DetectCircularDependencies_WhenNoCycle_ReturnsFalse()
    {
        var manager = new TaskManager();
        manager.AddTask("A", "A", new List<string>(), 2, new List<string>());
        manager.AddTask("B", "B", new List<string> { "A" }, 3, new List<string>());
        manager.AddTask("C", "C", new List<string> { "B" }, 1, new List<string>());

        var hasCycle = manager.DetectCircularDependencies();

        Assert.False(hasCycle);
    }

    [Fact]
    public void DetectCircularDependencies_WhenCycleExists_ReturnsTrue()
    {
        var manager = new TaskManager();
        manager.AddTask("A", "A", new List<string> { "B" }, 2, new List<string>());
        manager.AddTask("B", "B", new List<string> { "A" }, 3, new List<string>());

        var hasCycle = manager.DetectCircularDependencies();

        Assert.True(hasCycle);
    }

    [Fact]
    public void DetectCircularDependencies_WhenSelfDependencyExists_ReturnsTrue()
    {
        var manager = new TaskManager();
        manager.AddTask("A", "A", new List<string> { "A" }, 2, new List<string>());

        var hasCycle = manager.DetectCircularDependencies();

        Assert.True(hasCycle);
    }

    [Fact]
    public void DetectCircularDependencies_WhenCycleInDisconnectedComponent_ReturnsTrue()
    {
        var manager = new TaskManager();
        manager.AddTask("A", "A", new List<string>(), 1, new List<string>());
        manager.AddTask("B", "B", new List<string> { "C" }, 1, new List<string>());
        manager.AddTask("C", "C", new List<string> { "B" }, 1, new List<string>());

        var hasCycle = manager.DetectCircularDependencies();

        Assert.True(hasCycle);
    }

    [Fact]
    public void DetectCircularDependencies_WhenDependencyMissing_IgnoresMissingDependency()
    {
        var manager = new TaskManager();
        manager.AddTask("A", "A", new List<string> { "Missing" }, 1, new List<string>());

        var hasCycle = manager.DetectCircularDependencies();

        Assert.False(hasCycle);
    }

    [Fact]
    public void CalculateEarliestStart_WhenNoDependencies_ReturnsZero()
    {
        var manager = new TaskManager();
        manager.AddTask("A", "A", new List<string>(), 5, new List<string>());

        var result = manager.CalculateEarliestStart("A");

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateEarliestStart_WhenMultipleDependencies_UsesMaxDependencyEnd()
    {
        var manager = new TaskManager();
        manager.AddTask("A", "A", new List<string>(), 2, new List<string>());
        manager.AddTask("B", "B", new List<string>(), 4, new List<string>());
        manager.AddTask("C", "C", new List<string> { "A", "B" }, 1, new List<string>());

        var result = manager.CalculateEarliestStart("C");

        Assert.Equal(4, result);
    }

    [Fact]
    public void CalculateEarliestStart_WhenTaskDoesNotExist_ThrowsTaskManagerException()
    {
        var manager = new TaskManager();

        var exception = Assert.Throws<TaskManagerException>(() => manager.CalculateEarliestStart("Missing"));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void CalculateEarliestStart_WhenDependencyMissing_TreatsMissingDependencyAsZero()
    {
        var manager = new TaskManager();
        manager.AddTask("A", "A", new List<string> { "Missing" }, 1, new List<string>());

        var result = manager.CalculateEarliestStart("A");

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateEarliestStart_WhenTaskAddedAfterCaching_RecalculatesDueToCacheClear()
    {
        var manager = new TaskManager();
        var dependencies = new List<string> { "A" };

        manager.AddTask("A", "A", new List<string>(), 2, new List<string>());
        manager.AddTask("B", "B", dependencies, 1, new List<string>());

        var first = manager.CalculateEarliestStart("B");
        Assert.Equal(2, first);

        dependencies.Add("C");
        manager.AddTask("C", "C", new List<string>(), 10, new List<string>());

        var second = manager.CalculateEarliestStart("B");
        Assert.Equal(10, second);
    }

    [Fact]
    public void GetCriticalPath_WhenNoTasks_ReturnsEmptyPath()
    {
        var manager = new TaskManager();

        var path = manager.GetCriticalPath();

        Assert.Empty(path);
    }

    [Fact]
    public void GetCriticalPath_WhenLinearChain_ReturnsFullChain()
    {
        var manager = new TaskManager();
        manager.AddTask("A", "A", new List<string>(), 2, new List<string>());
        manager.AddTask("B", "B", new List<string> { "A" }, 3, new List<string>());
        manager.AddTask("C", "C", new List<string> { "B" }, 4, new List<string>());

        var path = manager.GetCriticalPath();

        Assert.Equal(new List<string> { "A", "B", "C" }, path);
    }

    [Fact]
    public void GetCriticalPath_WhenBranchingGraph_ReturnsLongestPath()
    {
        var manager = new TaskManager();
        manager.AddTask("A", "A", new List<string>(), 3, new List<string>());
        manager.AddTask("B", "B", new List<string> { "A" }, 4, new List<string>());
        manager.AddTask("C", "C", new List<string> { "A" }, 2, new List<string>());
        manager.AddTask("D", "D", new List<string> { "B", "C" }, 1, new List<string>());

        var path = manager.GetCriticalPath();

        Assert.Equal(new List<string> { "A", "B", "D" }, path);
    }

    [Fact]
    public void GetCriticalPath_WhenCycleExists_ThrowsTaskManagerException()
    {
        var manager = new TaskManager();
        manager.AddTask("A", "A", new List<string> { "B" }, 1, new List<string>());
        manager.AddTask("B", "B", new List<string> { "A" }, 1, new List<string>());

        var exception = Assert.Throws<TaskManagerException>(() => manager.GetCriticalPath());

        Assert.Contains("circular dependencies", exception.Message);
    }
}

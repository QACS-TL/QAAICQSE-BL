namespace Lab1.Exemplar.Tests;

public class TaskManagerTests
{
    [Fact]
    public void AddTask_StoresTaskWithProvidedValues()
    {
        var manager = new TaskManager();

        manager.AddTask("T1", "Design", new List<string>(), 3, new List<string> { "Dev" });

        Assert.True(manager.Tasks.ContainsKey("T1"));
        var task = manager.Tasks["T1"];
        Assert.Equal("Design", task.Name);
        Assert.Equal(3, task.Duration);
        Assert.Empty(task.Dependencies);
        Assert.Equal(new List<string> { "Dev" }, task.Resources);
        Assert.Null(task.StartDate);
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
}

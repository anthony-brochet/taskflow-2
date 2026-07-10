using TaskFlow.Models;
using Xunit;

namespace TaskFlow.Tests.Models;

public class TodoTaskTests
{
    [Fact]
    public void TodoTask_DefaultValues_AreCorrect()
    {
        var task = new TodoTask();

        // ACT = EXECUTER
        // Assert.False = vérifie qu'une condition est false.
        // Asert.Equal, = vérifie l'égalité entre deux valeurs.
        // Assert.Null vérifie qu'un objet est null.
        // Si un test échoue, le test échoue et affiche un message clair.
        Assert.False(task.IsCompleted, "Une nouvelle tâche ne doit pas être terminée par défaut");
        Assert.Equal(PriorityLevel.Medium, task.Priority);
        Assert.Null(task.UserId);
        Assert.Null(task.CategoryId);           
   }

    [Fact]
    public void TodoTask_IsCompleted_CanBeToggled()
    {
        var task = new TodoTask { IsCompleted = false };

        task.IsCompleted = !task.IsCompleted;

        // ASSERT : la tâche doit maintenant être terminée.
        Assert.True(task.IsCompleted, "La tâche doit-être terminée après le toggle.");
    }

    // [Theory] : test paramétré - même test avec plusieurs jeux de données.
    // [InlineData] : un jeu de données.
    [Theory]
    [InlineData(PriorityLevel.Low, 0)]
    [InlineData(PriorityLevel.Medium, 1)]
    [InlineData(PriorityLevel.High, 2)]
    public void Priority_HasCorrectIntegerValues(PriorityLevel priorityLevel, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)priorityLevel);
    }

    [Fact]
    public void TodoTask_DueDate_IsInFutureByDefault()
    {
        var task = new TodoTask();

        Assert.True( task.DueDate >= DateTime.Today,
            "La date d'échéance par défaut doit-être dans le futur.");
    }
}

using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.Tests;

namespace TaskFlow.Tests.Services;

// Tests de la COUCHE MÉTIER. Depuis la refonte, la logique (requêtes EF,
// filtres, sécurité) vit dans TaskService : c'est donc ici qu'on la teste,
// sur une vraie base InMemory. Patron AAA (Arrange / Act / Assert).
public class TaskServiceTests
{
    private static TaskService CreateService(Data.AppDbContext context) => new(context);

    [Fact]
    public async Task GetPagedAsync_ReturnsOnlyUserTasks()
    {
        var user1 = "user-1";
        var user2 = "user-2";

        using var context = TestHelpers.CreateInMemoryContext();
        context.TodoTasks.AddRange(
            new TodoTask { Id = 1, Title = "T1", UserId = user1, DueDate = DateTime.Today },
            new TodoTask { Id = 2, Title = "T2", UserId = user1, DueDate = DateTime.Today },
            new TodoTask { Id = 3, Title = "T3", UserId = user2, DueDate = DateTime.Today });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var page = await service.GetPagedAsync(user1, null, null, null, 1, 10);

        Assert.Equal(2, page.Count);
        Assert.DoesNotContain(page, t => t.UserId == user2);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByCompletedStatus()
    {
        var user = "user-1";
        using var context = TestHelpers.CreateInMemoryContext();
        context.TodoTasks.AddRange(
            new TodoTask { Id = 1, Title = "Faite", UserId = user, IsCompleted = true, DueDate = DateTime.Today },
            new TodoTask { Id = 2, Title = "En cours", UserId = user, IsCompleted = false, DueDate = DateTime.Today });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var page = await service.GetPagedAsync(user, null, "completed", null, 1, 10);

        Assert.Single(page);
        Assert.True(page[0].IsCompleted);
    }

    [Fact]
    public async Task CreateAsync_SetsUserIdAndPersists()
    {
        var user = "user-1";
        using var context = TestHelpers.CreateInMemoryContext();
        var service = CreateService(context);

        await service.CreateAsync(user, new TodoTask
        {
            Title = "Nouvelle tâche",
            Priority = PriorityLevel.High,
            DueDate = DateTime.Today.AddDays(3)
        });

        var saved = Assert.Single(context.TodoTasks);
        Assert.Equal("Nouvelle tâche", saved.Title);
        Assert.Equal(user, saved.UserId);
    }

    [Fact]
    public async Task GetForEditAsync_ReturnsNull_WhenTaskBelongsToAnotherUser()
    {
        // Cœur de la sécurité anti-IDOR : un utilisateur ne récupère pas la tâche d'autrui.
        var owner = "owner";
        var intruder = "intruder";
        using var context = TestHelpers.CreateInMemoryContext();
        context.TodoTasks.Add(new TodoTask { Id = 51, Title = "Privée", UserId = owner, DueDate = DateTime.Today });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetForEditAsync(intruder, 51);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalse_WhenNotOwner()
    {
        var owner = "owner";
        var intruder = "intruder";
        using var context = TestHelpers.CreateInMemoryContext();
        context.TodoTasks.Add(new TodoTask { Id = 7, Title = "Privée", UserId = owner, DueDate = DateTime.Today });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var updated = await service.UpdateAsync(intruder, 7,
            new TodoTask { Id = 7, Title = "Piraté", DueDate = DateTime.Today });

        Assert.False(updated);
        Assert.Equal("Privée", context.TodoTasks.Single().Title); // inchangée
    }

    [Fact]
    public async Task DeleteAsync_RemovesTask_WhenOwner()
    {
        var user = "user-1";
        using var context = TestHelpers.CreateInMemoryContext();
        context.TodoTasks.Add(new TodoTask { Id = 42, Title = "À supprimer", UserId = user, DueDate = DateTime.Today });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var deleted = await service.DeleteAsync(user, 42);

        Assert.True(deleted);
        Assert.Empty(context.TodoTasks);
    }

    [Fact]
    public async Task ToggleAsync_FlipsCompletion_AndReturnsNewState()
    {
        var user = "user-1";
        using var context = TestHelpers.CreateInMemoryContext();
        context.TodoTasks.Add(new TodoTask { Id = 9, Title = "T", UserId = user, IsCompleted = false, DueDate = DateTime.Today });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var newState = await service.ToggleAsync(user, 9);

        Assert.True(newState);
        Assert.True(context.TodoTasks.Single().IsCompleted);
    }

    [Fact]
    public async Task ToggleAsync_ReturnsNull_WhenNotFound()
    {
        var user = "user-1";
        using var context = TestHelpers.CreateInMemoryContext();
        var service = CreateService(context);

        Assert.Null(await service.ToggleAsync(user, 999));
    }

    [Fact]
    public async Task GetDashboardAsync_ComputesCounters()
    {
        var user = "user-1";
        using var context = TestHelpers.CreateInMemoryContext();
        context.TodoTasks.AddRange(
            new TodoTask { Id = 1, Title = "Faite", UserId = user, IsCompleted = true, DueDate = DateTime.Today },
            new TodoTask { Id = 2, Title = "En cours", UserId = user, IsCompleted = false, DueDate = DateTime.Today.AddDays(2) },
            new TodoTask { Id = 3, Title = "En retard", UserId = user, IsCompleted = false, DueDate = DateTime.Today.AddDays(-2) });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var vm = await service.GetDashboardAsync(user);

        Assert.Equal(3, vm.Total);
        Assert.Equal(1, vm.Completed);
        Assert.Equal(1, vm.InProgress);
        Assert.Equal(1, vm.Late);
        Assert.Equal(33, vm.CompletedPercentage); // 1/3 arrondi
    }
}

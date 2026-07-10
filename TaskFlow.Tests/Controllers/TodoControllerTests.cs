using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskFlow.Controllers;
using TaskFlow.Helpers;
using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.Tests;

namespace TaskFlow.Tests.Controllers;

// Depuis la refonte, le contrôleur dépend d'INTERFACES (ITaskService,
// ICategoryService). On peut donc le tester SANS base de données : on injecte
// des mocks Moq et on vérifie que le contrôleur traduit bien le résultat du
// service en bonne réponse HTTP (View / Redirect / NotFound). C'est tout le
// bénéfice de l'inversion de dépendance.
public class TodoControllerTests
{
    private const string UserId = "user-1";

    private static TodoController CreateController(
        Mock<ITaskService> taskService, Mock<ICategoryService>? categoryService = null)
    {
        categoryService ??= new Mock<ICategoryService>();
        categoryService.Setup(c => c.GetAllAsync()).ReturnsAsync([]);

        var controller = new TodoController(
            taskService.Object,
            categoryService.Object,
            TestHelpers.CreateMockUserManager(UserId));

        TestHelpers.SetUser(controller, UserId);

        return controller;
    }

    [Fact]
    public async Task Index_ReturnsView_WithPagedTasksFromService()
    {
        var tasks = new List<TodoTask>
        {
            new() { Id = 1, Title = "T1", UserId = UserId, DueDate = DateTime.Today }
        };

        var page = PaginatedList<TodoTask>.Create(tasks, tasks.Count, 1, 10);

        var taskService = new Mock<ITaskService>();

        taskService
            .Setup(s => s.GetPagedAsync(UserId, null, null, null, 1, It.IsAny<int>()))
            .ReturnsAsync(page);

        var controller = CreateController(taskService);

        var result = await controller.Index(null, null, null);

        var view = Assert.IsType<ViewResult>(result);

        Assert.Same(page, view.Model);
    }

    [Fact]
    public async Task Edit_Get_Returns404_WhenServiceReturnsNull()
    {
        var taskService = new Mock<ITaskService>();

        taskService.Setup(s => s.GetForEditAsync(UserId, 51)).ReturnsAsync((TodoTask?)null);

        var controller = CreateController(taskService);

        var result = await controller.Edit(51);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_Post_CallsServiceAndRedirects()
    {
        var taskService = new Mock<ITaskService>();
        var controller = CreateController(taskService);

        var task = new TodoTask { Title = "Test", DueDate = DateTime.Today.AddDays(1) };
        var result = await controller.Create(task);

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal("Index", redirect.ActionName);

        taskService.Verify(s => s.CreateAsync(UserId, task), Times.Once);
    }

    [Fact]
    public async Task ToggleCompletion_Returns404_WhenServiceReturnsNull()
    {
        var taskService = new Mock<ITaskService>();
        
        taskService.Setup(s => s.ToggleAsync(UserId, 5)).ReturnsAsync((bool?)null);

        var controller = CreateController(taskService);

        var result = await controller.ToggleCompletion(5);

        Assert.IsType<NotFoundResult>(result);
    }
}

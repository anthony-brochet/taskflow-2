using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskFlow.Controllers.Api;
using TaskFlow.Dtos;
using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.Tests;

namespace TaskFlow.Tests.Controllers;

// Tests du contrôleur d'API, mockés sur ITaskService (même service que le web).
// On vérifie le contrat REST : bons codes HTTP (200 / 201 / 204 / 404) et bon
// mapping entité -> DTO.
public class TaskApiControllerTests
{
    private const string UserId = "user-1";

    private static TaskApiController CreateController(Mock<ITaskService> taskService)
    {
        var controller = new TaskApiController(
            taskService.Object, TestHelpers.CreateMockUserManager(UserId));

        TestHelpers.SetUser(controller, UserId);

        return controller;
    }

    [Fact]
    public async Task GetById_Returns404_WhenServiceReturnsNull()
    {
        var taskService = new Mock<ITaskService>();

        taskService.Setup(s => s.GetDetailsAsync(UserId, 5)).ReturnsAsync((TodoTask?)null);

        var controller = CreateController(taskService);

        var result = await controller.GetById(5);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithDto_WhenFound()
    {
        var task = new TodoTask { Id = 5, Title = "T5", UserId = UserId, DueDate = DateTime.Today };
        var taskService = new Mock<ITaskService>();

        taskService.Setup(s => s.GetDetailsAsync(UserId, 5)).ReturnsAsync(task);

        var controller = CreateController(taskService);

        var result = await controller.GetById(5);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<TaskDto>(ok.Value);

        Assert.Equal(5, dto.Id);
        Assert.Equal("T5", dto.Title);
    }

    [Fact]
    public async Task Create_Returns201Created()
    {
        var taskService = new Mock<ITaskService>();
        var controller = CreateController(taskService);

        var input = new SaveTaskDto("Nouvelle", null, PriorityLevel.Medium, false, DateTime.Today, null);
        var result = await controller.Create(input);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);

        Assert.Equal(nameof(TaskApiController.GetById), created.ActionName);

        taskService.Verify(s => s.CreateAsync(UserId, It.IsAny<TodoTask>()), Times.Once);
    }

    [Fact]
    public async Task Delete_Returns404_WhenServiceReturnsFalse()
    {
        var taskService = new Mock<ITaskService>();

        taskService.Setup(s => s.DeleteAsync(UserId, 9)).ReturnsAsync(false);

        var controller = CreateController(taskService);

        var result = await controller.Delete(9);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_Returns204_WhenDeleted()
    {
        var taskService = new Mock<ITaskService>();
        
        taskService.Setup(s => s.DeleteAsync(UserId, 9)).ReturnsAsync(true);

        var controller = CreateController(taskService);

        var result = await controller.Delete(9);

        Assert.IsType<NoContentResult>(result);
    }
}

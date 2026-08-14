using PixelArt.Core.Application.Drawings;
using PixelArt.Core.Application.Drawings.Exceptions;

namespace PixelArt.Core.Tests.Drawings;

public class DrawingServiceTests
{
    private const int OwnerId = 1;
    private const int OtherUserId = 2;

    private static string[][] Grid() =>
    [
        ["#FF0000FF", "#00FF00FF"],
        ["#0000FFFF", "#000000FF"],
    ];

    private static (DrawingService Service, FakeDrawingRepository Repository) Build()
    {
        var repository = new FakeDrawingRepository();
        return (new DrawingService(repository), repository);
    }

    [Fact]
    public async Task CreateAsync_StoresDrawingWithOwner()
    {
        var (service, repository) = Build();

        var drawing = await service.CreateAsync("art", 2, 2, Grid(), OwnerId);

        Assert.Equal(1, drawing.Id);
        Assert.Equal("art", drawing.Name);
        Assert.Equal(OwnerId, drawing.UserId);
        Assert.Single(repository.Stored);
    }

    [Fact]
    public async Task CreateAsync_InvalidInput_ThrowsAndStoresNothing()
    {
        var (service, repository) = Build();

        await Assert.ThrowsAsync<InvalidDrawingException>(
            () => service.CreateAsync("art", 5, 2, Grid(), OwnerId));

        Assert.Empty(repository.Stored);
    }

    [Fact]
    public async Task GetAsync_OwnedDrawing_ReturnsIt()
    {
        var (service, _) = Build();
        var created = await service.CreateAsync("art", 2, 2, Grid(), OwnerId);

        var found = await service.GetAsync(created.Id, OwnerId);

        Assert.Equal(created.Id, found.Id);
    }

    [Fact]
    public async Task GetAsync_MissingDrawing_Throws()
    {
        var (service, _) = Build();

        var ex = await Assert.ThrowsAsync<DrawingNotFoundException>(
            () => service.GetAsync(999, OwnerId));

        Assert.Equal("Drawing 999 was not found.", ex.Message);
    }

    [Fact]
    public async Task GetAsync_AnotherUsersDrawing_ThrowsNotFound()
    {
        var (service, _) = Build();
        var created = await service.CreateAsync("art", 2, 2, Grid(), OwnerId);

        await Assert.ThrowsAsync<DrawingNotFoundException>(
            () => service.GetAsync(created.Id, OtherUserId));
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyCallersDrawings()
    {
        var (service, _) = Build();
        await service.CreateAsync("mine", 2, 2, Grid(), OwnerId);
        await service.CreateAsync("theirs", 2, 2, Grid(), OtherUserId);

        var mine = await service.ListAsync(OwnerId);

        Assert.Single(mine);
        Assert.Equal("mine", mine[0].Name);
    }

    [Fact]
    public async Task UpdateAsync_ChangesStoredFields()
    {
        var (service, _) = Build();
        var created = await service.CreateAsync("before", 2, 2, Grid(), OwnerId);

        string[][] updated = [["#FFFFFFFF", "#FFFFFFFF"], ["#FFFFFFFF", "#FFFFFFFF"]];
        await service.UpdateAsync(created.Id, "after", 2, 2, updated, OwnerId);

        var found = await service.GetAsync(created.Id, OwnerId);
        Assert.Equal("after", found.Name);
        Assert.Equal("#FFFFFFFF", found.Pixels[0, 0]);
    }

    [Fact]
    public async Task UpdateAsync_AnotherUsersDrawing_ThrowsNotFound()
    {
        var (service, _) = Build();
        var created = await service.CreateAsync("art", 2, 2, Grid(), OwnerId);

        await Assert.ThrowsAsync<DrawingNotFoundException>(
            () => service.UpdateAsync(created.Id, "hijacked", 2, 2, Grid(), OtherUserId));
    }

    [Fact]
    public async Task DeleteAsync_RemovesDrawing()
    {
        var (service, repository) = Build();
        var created = await service.CreateAsync("art", 2, 2, Grid(), OwnerId);

        await service.DeleteAsync(created.Id, OwnerId);

        Assert.Empty(repository.Stored);
    }

    [Fact]
    public async Task DeleteAsync_MissingDrawing_Throws()
    {
        var (service, _) = Build();

        await Assert.ThrowsAsync<DrawingNotFoundException>(
            () => service.DeleteAsync(999, OwnerId));
    }
}

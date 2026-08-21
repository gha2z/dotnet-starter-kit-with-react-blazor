using Bunit;
using FSH.BlazorShared.Models.Files;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Files;

public sealed class FileManagerPageTests : TestSetup
{
    private readonly IFileService _files = Substitute.For<IFileService>();

    public FileManagerPageTests()
    {
        Services.AddSingleton(_files);
    }

    private static FileAssetDto SampleFile(
        string name = "report.pdf",
        string contentType = "application/pdf",
        long size = 1024,
        FileVisibility visibility = FileVisibility.Private) =>
        new(Guid.NewGuid(), "MyFiles", null, name, contentType, size, visibility,
            FileAssetStatus.Available, 1, DateTime.UtcNow.AddDays(-1), null, "user-1");

    [Fact]
    public void Renders_file_list_with_data()
    {
        _files.ListMyFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([SampleFile("report.pdf"), SampleFile("image.png", "image/png", 2048)]);
        _files.ListSharedFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Files.FileManagerPage>();

        cut.FindAll("tr").Count.ShouldBeGreaterThan(1); // header + 2 rows
    }

    [Fact]
    public void Shows_empty_state_when_no_files()
    {
        _files.ListMyFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _files.ListSharedFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Files.FileManagerPage>();

        cut.Markup.ShouldContain("No files uploaded yet");
    }

    [Fact]
    public void Displays_upload_zone_on_my_files_tab()
    {
        _files.ListMyFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _files.ListSharedFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Files.FileManagerPage>();

        cut.FindAll(".fsh-file-dropzone").Count.ShouldBe(1);
    }

    [Fact]
    public void Shows_filter_chips()
    {
        _files.ListMyFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _files.ListSharedFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Files.FileManagerPage>();

        var chips = cut.FindAll(".mud-chip");
        chips.Count.ShouldBeGreaterThanOrEqualTo(4); // All, Images, Documents, Archives (+ maybe Other)
    }

    [Fact]
    public void Shows_file_size_formatted()
    {
        _files.ListMyFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([SampleFile("large.zip", "application/zip", 5 * 1024 * 1024)]);
        _files.ListSharedFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Files.FileManagerPage>();

        cut.Markup.ShouldContain("5.0 MB");
    }

    [Fact]
    public void Shows_visibility_chip()
    {
        _files.ListMyFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([SampleFile(visibility: FileVisibility.Public)]);
        _files.ListSharedFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Files.FileManagerPage>();

        cut.Markup.ShouldContain("Public");
    }

    [Fact]
    public void Toggles_dragover_highlight_on_drag_enter_and_leave()
    {
        _files.ListMyFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _files.ListSharedFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Files.FileManagerPage>();
        var zone = cut.Find("#fsh-dropzone");

        zone.TriggerEvent("ondragenter", new DragEventArgs());
        cut.Find(".fsh-file-dropzone").ClassList.ShouldContain("dragover");

        zone.TriggerEvent("ondragleave", new DragEventArgs());
        cut.Find(".fsh-file-dropzone").ClassList.ShouldNotContain("dragover");
    }

    [Fact]
    public void Dropzone_uses_prevent_default_modifiers()
    {
        _files.ListMyFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _files.ListSharedFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Files.FileManagerPage>();

        cut.Markup.ShouldContain("ondragenter:preventdefault");
        cut.Markup.ShouldContain("ondragover:preventdefault");
        cut.Markup.ShouldContain("ondrop:preventdefault");
    }

    [Fact]
    public async Task Drop_uploads_dropped_files_via_js_bridge()
    {
        _files.ListMyFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _files.ListSharedFilesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _files.RequestUploadUrlAsync(Arg.Any<RequestUploadUrlRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PresignedUploadResponse(
                Guid.NewGuid(),
                new Uri("https://presigned.local/upload"),
                new Dictionary<string, string>(),
                DateTimeOffset.UtcNow.AddMinutes(10)));
        _files.FinalizeUploadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(SampleFile("drop.txt"));

        JSInterop.SetupVoid("fshInitDropCapture", _ => true);
        JSInterop.Setup<List<FSH.Dashboard.Wasm.Pages.Files.FileManagerPage.DroppedFileInfo>>("fshGetDroppedFiles")
            .SetResult([new FSH.Dashboard.Wasm.Pages.Files.FileManagerPage.DroppedFileInfo("drop.txt", "text/plain", 42)]);
        JSInterop.Setup<byte[]>("fshReadDroppedFile", _ => true)
            .SetResult("hello, dropped!"u8.ToArray());
        JSInterop.SetupVoid("fshFileUpload", _ => true);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Files.FileManagerPage>();
        var zone = cut.Find("#fsh-dropzone");

        zone.TriggerEvent("ondragenter", new DragEventArgs());
        cut.Find(".fsh-file-dropzone").ClassList.ShouldContain("dragover");

        await cut.InvokeAsync(() => zone.TriggerEvent("ondrop", new DragEventArgs()));

        await _files.Received(1).RequestUploadUrlAsync(
            Arg.Is<RequestUploadUrlRequest>(r => r.FileName == "drop.txt" && r.SizeBytes == 42),
            Arg.Any<CancellationToken>());
        cut.Find(".fsh-file-dropzone").ClassList.ShouldNotContain("dragover");
    }
}

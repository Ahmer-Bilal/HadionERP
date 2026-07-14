namespace Platform.Attachments.Tests;

/// <summary>Proves <see cref="AttachmentService"/> — the single entry point modules call — enforces its
/// validation rules (size ceiling, content-type allowlist) and correctly round-trips through the
/// repository port. The real database-backed repository is proved separately by an integration test.</summary>
public class AttachmentServiceTests
{
    private const string BusinessObjectType = "BusinessPartner";

    private static (AttachmentService service, FakeAttachmentRepository repository) NewService()
    {
        var repository = new FakeAttachmentRepository();
        return (new AttachmentService(repository), repository);
    }

    [Fact]
    public async Task UploadAsync_stores_the_file_and_returns_its_metadata()
    {
        var (service, _) = NewService();
        var businessObjectId = Guid.NewGuid();
        var content = new byte[] { 1, 2, 3 };

        var metadata = await service.UploadAsync(
            BusinessObjectType, businessObjectId, "cr-copy.pdf", "application/pdf", content, "ahmer.bilal");

        Assert.Equal("cr-copy.pdf", metadata.FileName);
        Assert.Equal("application/pdf", metadata.ContentType);
        Assert.Equal(3, metadata.SizeBytes);
        Assert.Equal("ahmer.bilal", metadata.UploadedBy);
        Assert.Equal(businessObjectId, metadata.BusinessObjectId);
    }

    [Fact]
    public async Task UploadAsync_rejects_an_empty_file()
    {
        var (service, _) = NewService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadAsync(BusinessObjectType, Guid.NewGuid(), "empty.pdf", "application/pdf", Array.Empty<byte>(), "ahmer.bilal"));
    }

    [Fact]
    public async Task UploadAsync_rejects_a_file_over_the_size_limit()
    {
        var (service, _) = NewService();
        var tooLarge = new byte[AttachmentService.MaxSizeBytes + 1];

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadAsync(BusinessObjectType, Guid.NewGuid(), "huge.pdf", "application/pdf", tooLarge, "ahmer.bilal"));
    }

    [Fact]
    public async Task UploadAsync_rejects_a_disallowed_content_type()
    {
        var (service, _) = NewService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadAsync(BusinessObjectType, Guid.NewGuid(), "installer.exe", "application/x-msdownload", new byte[] { 1 }, "ahmer.bilal"));
    }

    [Fact]
    public async Task ListAsync_returns_only_attachments_for_the_requested_business_object()
    {
        var (service, _) = NewService();
        var targetId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        await service.UploadAsync(BusinessObjectType, targetId, "a.pdf", "application/pdf", new byte[] { 1 }, "ahmer.bilal");
        await service.UploadAsync(BusinessObjectType, otherId, "b.pdf", "application/pdf", new byte[] { 1 }, "ahmer.bilal");

        var results = await service.ListAsync(BusinessObjectType, targetId);

        var only = Assert.Single(results);
        Assert.Equal("a.pdf", only.FileName);
    }

    [Fact]
    public async Task DownloadAsync_returns_the_metadata_and_the_original_bytes()
    {
        var (service, _) = NewService();
        var content = new byte[] { 9, 8, 7, 6 };
        var uploaded = await service.UploadAsync(
            BusinessObjectType, Guid.NewGuid(), "cert.pdf", "application/pdf", content, "ahmer.bilal");

        var downloaded = await service.DownloadAsync(uploaded.Id);

        Assert.NotNull(downloaded);
        Assert.Equal("cert.pdf", downloaded!.Value.Metadata.FileName);
        Assert.Equal(content, downloaded.Value.Content);
    }

    [Fact]
    public async Task DownloadAsync_returns_null_for_an_unknown_id()
    {
        var (service, _) = NewService();

        Assert.Null(await service.DownloadAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_removes_the_attachment_and_it_is_no_longer_listed()
    {
        var (service, _) = NewService();
        var businessObjectId = Guid.NewGuid();
        var uploaded = await service.UploadAsync(
            BusinessObjectType, businessObjectId, "old.pdf", "application/pdf", new byte[] { 1 }, "ahmer.bilal");

        var deleted = await service.DeleteAsync(uploaded.Id);

        Assert.True(deleted);
        Assert.Empty(await service.ListAsync(BusinessObjectType, businessObjectId));
    }

    [Fact]
    public async Task DeleteAsync_returns_false_for_an_unknown_id()
    {
        var (service, _) = NewService();

        Assert.False(await service.DeleteAsync(Guid.NewGuid()));
    }
}

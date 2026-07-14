using Modules.MasterData.Infrastructure;
using Platform.Attachments;

namespace Modules.MasterData.IntegrationTests;

/// <summary>
/// Proves <see cref="EfAttachmentRepository"/> actually persists both an attachment's metadata AND its
/// file bytes to real PostgreSQL, and that the bytes round-trip byte-for-byte through a fresh
/// <see cref="MasterDataDbContext"/> — the same "prove persistence across a fresh DbContext" pattern this
/// module already uses for Business Partner and WorkflowInstance, applied here to the one thing genuinely
/// new about this port: binary content stored separately from its own metadata row (see
/// <see cref="AttachmentMetadata"/>'s own doc comment for why).
/// </summary>
public class AttachmentPersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task An_uploaded_attachment_reads_back_identically_through_a_fresh_DbContext()
    {
        var businessObjectId = Guid.NewGuid();
        var content = new byte[] { 1, 2, 3, 4, 5 };
        Guid attachmentId;

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var repository = new EfAttachmentRepository(writeContext);
            var service = new AttachmentService(repository);
            var metadata = await service.UploadAsync(
                "BusinessPartner", businessObjectId, "cr-copy.pdf", "application/pdf", content, "ahmer.bilal");
            attachmentId = metadata.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var readRepository = new EfAttachmentRepository(readContext);
        var readService = new AttachmentService(readRepository);

        var listed = await readService.ListAsync("BusinessPartner", businessObjectId);
        var listedOnly = Assert.Single(listed);
        Assert.Equal("cr-copy.pdf", listedOnly.FileName);
        Assert.Equal("application/pdf", listedOnly.ContentType);
        Assert.Equal(5, listedOnly.SizeBytes);

        var downloaded = await readService.DownloadAsync(attachmentId);
        Assert.NotNull(downloaded);
        Assert.Equal(content, downloaded!.Value.Content);
    }

    [Fact]
    public async Task Deleting_an_attachment_removes_both_its_metadata_and_its_content_row()
    {
        Guid attachmentId;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var service = new AttachmentService(new EfAttachmentRepository(writeContext));
            var metadata = await service.UploadAsync(
                "BusinessPartner", Guid.NewGuid(), "old.pdf", "application/pdf", new byte[] { 9 }, "ahmer.bilal");
            attachmentId = metadata.Id;
        }

        await using (var deleteContext = TestDatabase.CreateContext())
        {
            var deleted = await new AttachmentService(new EfAttachmentRepository(deleteContext)).DeleteAsync(attachmentId);
            Assert.True(deleted);
        }

        await using var readContext = TestDatabase.CreateContext();
        var readService = new AttachmentService(new EfAttachmentRepository(readContext));
        Assert.Null(await readService.DownloadAsync(attachmentId));
    }
}

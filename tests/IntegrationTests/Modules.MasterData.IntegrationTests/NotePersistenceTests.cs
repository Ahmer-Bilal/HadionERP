using Modules.MasterData.Infrastructure;
using Platform.Notes;

namespace Modules.MasterData.IntegrationTests;

/// <summary>Proves <see cref="EfNoteRepository"/> actually persists a note to real PostgreSQL and it reads
/// back identically through a fresh <see cref="MasterDataDbContext"/> — the same "prove persistence across
/// a fresh DbContext" pattern this module already uses everywhere else.</summary>
public class NotePersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task An_added_note_reads_back_identically_through_a_fresh_DbContext()
    {
        var businessObjectId = Guid.NewGuid();

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var service = new NoteService(new EfNoteRepository(writeContext));
            await service.AddAsync("BusinessPartner", businessObjectId, "Confirmed bank details by phone.", "ahmer.bilal");
        }

        await using var readContext = TestDatabase.CreateContext();
        var readService = new NoteService(new EfNoteRepository(readContext));
        var notes = await readService.ListAsync("BusinessPartner", businessObjectId);

        var only = Assert.Single(notes);
        Assert.Equal("Confirmed bank details by phone.", only.Text);
        Assert.Equal("ahmer.bilal", only.CreatedBy);
    }

    [Fact]
    public async Task Deleting_a_note_removes_it()
    {
        var businessObjectId = Guid.NewGuid();
        Guid noteId;

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var service = new NoteService(new EfNoteRepository(writeContext));
            var note = await service.AddAsync("BusinessPartner", businessObjectId, "Temporary note.", "ahmer.bilal");
            noteId = note.Id;
        }

        await using (var deleteContext = TestDatabase.CreateContext())
        {
            var deleted = await new NoteService(new EfNoteRepository(deleteContext)).DeleteAsync(noteId);
            Assert.True(deleted);
        }

        await using var readContext = TestDatabase.CreateContext();
        var readService = new NoteService(new EfNoteRepository(readContext));
        Assert.Empty(await readService.ListAsync("BusinessPartner", businessObjectId));
    }
}

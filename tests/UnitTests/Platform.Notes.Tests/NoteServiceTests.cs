namespace Platform.Notes.Tests;

/// <summary>Proves <see cref="NoteService"/> enforces its validation rules and correctly round-trips
/// through the repository port. The real database-backed repository is proved separately by an
/// integration test.</summary>
public class NoteServiceTests
{
    private const string BusinessObjectType = "BusinessPartner";

    private static NoteService NewService() => new(new FakeNoteRepository());

    [Fact]
    public async Task AddAsync_stores_the_note_and_returns_it()
    {
        var service = NewService();
        var businessObjectId = Guid.NewGuid();

        var note = await service.AddAsync(BusinessObjectType, businessObjectId, "Called the vendor, confirmed bank details.", "ahmer.bilal");

        Assert.Equal("Called the vendor, confirmed bank details.", note.Text);
        Assert.Equal("ahmer.bilal", note.CreatedBy);
        Assert.Equal(businessObjectId, note.BusinessObjectId);
    }

    [Fact]
    public async Task AddAsync_rejects_empty_text()
    {
        var service = NewService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAsync(BusinessObjectType, Guid.NewGuid(), "   ", "ahmer.bilal"));
    }

    [Fact]
    public async Task AddAsync_rejects_text_over_the_length_limit()
    {
        var service = NewService();
        var tooLong = new string('a', NoteService.MaxTextLength + 1);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAsync(BusinessObjectType, Guid.NewGuid(), tooLong, "ahmer.bilal"));
    }

    [Fact]
    public async Task ListAsync_returns_only_notes_for_the_requested_business_object_in_chronological_order()
    {
        var service = NewService();
        var targetId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        await service.AddAsync(BusinessObjectType, targetId, "First note", "ahmer.bilal");
        await service.AddAsync(BusinessObjectType, otherId, "Someone else's note", "finance.manager");
        await service.AddAsync(BusinessObjectType, targetId, "Second note", "ahmer.bilal");

        var notes = await service.ListAsync(BusinessObjectType, targetId);

        Assert.Equal(2, notes.Count);
        Assert.Equal("First note", notes[0].Text);
        Assert.Equal("Second note", notes[1].Text);
    }

    [Fact]
    public async Task DeleteAsync_removes_the_note_and_it_is_no_longer_listed()
    {
        var service = NewService();
        var businessObjectId = Guid.NewGuid();
        var note = await service.AddAsync(BusinessObjectType, businessObjectId, "Oops, wrong partner.", "ahmer.bilal");

        var deleted = await service.DeleteAsync(note.Id);

        Assert.True(deleted);
        Assert.Empty(await service.ListAsync(BusinessObjectType, businessObjectId));
    }

    [Fact]
    public async Task DeleteAsync_returns_false_for_an_unknown_id()
    {
        var service = NewService();

        Assert.False(await service.DeleteAsync(Guid.NewGuid()));
    }
}

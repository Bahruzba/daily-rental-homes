using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Contacts;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Route("api/contacts")]
public sealed class ContactsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ContactsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var items = await _db.RelatedContacts.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpPost]
    public async Task<IActionResult> Create(NewContactRequest request, CancellationToken cancellationToken)
    {
        if (request.RentalHomeId <= 0 || TextRules.Empty(request.Value))
        {
            return BadRequest(ApiResponse<object>.Fail("Rental home and contact value are required."));
        }

        var contact = new RelatedContact
        {
            RentalHomeId = request.RentalHomeId,
            FullName = TextRules.Empty(request.FullName) ? "Contact" : TextRules.Clean(request.FullName),
            Value = TextRules.Clean(request.Value),
            ContactType = request.ContactType,
            NotifyEnabled = request.NotifyEnabled
        };

        _db.RelatedContacts.Add(contact);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { contact.Id }));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var contact = await _db.RelatedContacts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (contact is null)
        {
            return NotFound(ApiResponse<object>.Fail("Contact not found."));
        }

        contact.IsDeleted = true;
        contact.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { contact.Id }));
    }
}

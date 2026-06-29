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
        var items = await _db.RelatedContacts.AsNoTracking().ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(NewContactRequest request, CancellationToken cancellationToken)
    {
        var contact = new RelatedContact
        {
            RentalHomeId = request.RentalHomeId,
            FullName = request.FullName,
            Value = request.Value,
            ContactType = request.ContactType,
            NotifyEnabled = request.NotifyEnabled
        };

        _db.RelatedContacts.Add(contact);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(contact.Id);
    }
}

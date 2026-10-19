using System.Security.Cryptography;
using HaladeHighSchool.Api.Data;
using HaladeHighSchool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HaladeHighSchool.Api.Services;

public sealed class SmartIDCardService
{
    private readonly ApplicationDbContext _db;
    private readonly SmartIDTokenKeyProtector _tokenKeyProtector;

    public SmartIDCardService(
        ApplicationDbContext db,
        SmartIDTokenKeyProtector tokenKeyProtector)
    {
        _db = db;
        _tokenKeyProtector = tokenKeyProtector;
    }

    public async Task AddCardAsync(
        ApplicationUser user,
        string cardType,
        CancellationToken cancellationToken)
    {
        await _db.Database.OpenConnectionAsync(cancellationToken);
        string cardUid;
        try
        {
            await using var command = _db.Database.GetDbConnection().CreateCommand();
            command.CommandText =
                "SELECT 'SID-' || EXTRACT(YEAR FROM CURRENT_TIMESTAMP)::int::text || '-' || " +
                "LPAD(nextval('\"SmartCardSerialSequence\"')::text, 8, '0');";
            cardUid = (string?)await command.ExecuteScalarAsync(cancellationToken)
                ?? throw new InvalidOperationException("The Smart ID serial sequence did not return a card UID.");
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }

        var tokenKey = RandomNumberGenerator.GetBytes(32);
        var protectedKey = _tokenKeyProtector.Protect(tokenKey);
        CryptographicOperations.ZeroMemory(tokenKey);

        _db.SmartCards.Add(new SmartCard
        {
            UserId = user.Id,
            CardUID = cardUid,
            QrTokenHash = protectedKey,
            CardType = cardType,
            Status = "ACTIVE",
            IssuedDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow.AddYears(5),
            CreatedAt = DateTime.UtcNow
        });
    }

}

using System.Security.Cryptography;
using HaladeHighSchool.Api.Data;
using HaladeHighSchool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HaladeHighSchool.Api.Services;

public sealed class SmartIDCardService
{
    private readonly ApplicationDbContext _db;
    private readonly SmartIDTokenKeyProtector _tokenKeyProtector;
    private readonly ISystemSettingsService _settings;

    public SmartIDCardService(
        ApplicationDbContext db,
        SmartIDTokenKeyProtector tokenKeyProtector,
        ISystemSettingsService settings)
    {
        _db = db;
        _tokenKeyProtector = tokenKeyProtector;
        _settings = settings;
    }

    public async Task<SmartCard> AddCardAsync(
        ApplicationUser user,
        string cardType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(user.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(cardType);

        if (cardType is not ("STUDENT" or "TEACHER" or "STAFF"))
        {
            throw new ArgumentException("The Smart ID card type is not supported.", nameof(cardType));
        }

        await _db.Database.OpenConnectionAsync(cancellationToken);
        string cardUid;
        try
        {
            await using var command = _db.Database.GetDbConnection().CreateCommand();
            command.CommandText =
                "SELECT 'SID-' || EXTRACT(YEAR FROM CURRENT_TIMESTAMP)::int::text || '-' || " +
                "LPAD(nextval('\"SmartCardSerialSequence\"')::text, 8, '0');";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            cardUid = result as string
                ?? throw new InvalidOperationException("The Smart ID serial sequence did not return a card UID.");

            if (string.IsNullOrWhiteSpace(cardUid))
            {
                throw new InvalidOperationException("The Smart ID serial sequence returned an empty card UID.");
            }
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }

        var tokenKey = RandomNumberGenerator.GetBytes(32);
        string protectedKey;
        try
        {
            protectedKey = _tokenKeyProtector.Protect(tokenKey);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(tokenKey);
        }

        if (string.IsNullOrWhiteSpace(protectedKey))
        {
            throw new InvalidOperationException("The Smart ID token key could not be protected.");
        }

        var schoolInfo = await _settings.GetSchoolInfoAsync(cancellationToken);
        var card = new SmartCard
        {
            UserId = user.Id,
            CardUID = cardUid,
            QrTokenHash = protectedKey,
            CardType = cardType,
            Status = "ACTIVE",
            SchoolName = schoolInfo.SchoolName,
            IssuedDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow.AddYears(5),
            CreatedAt = DateTime.UtcNow
        };
        _db.SmartCards.Add(card);
        return card;
    }

}

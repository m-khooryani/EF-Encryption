using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Microsoft.Extensions.Options;

namespace SampleLib;

public class AppDbContext : DbContext
{
    private readonly byte[] _aesKey;
    private readonly byte[] _aesIv;

    public AppDbContext(DbContextOptions<AppDbContext> options, IOptions<EncryptionOptions> encryptionOptions)
        : base(options)
    {
        if (encryptionOptions == null || string.IsNullOrEmpty(encryptionOptions.Value.EncryptionKeyBase64))
        {
            throw new InvalidOperationException("Encryption key not configured properly.");
        }

        _aesKey = Convert.FromBase64String(encryptionOptions.Value.EncryptionKeyBase64);
        _aesIv = Convert.FromBase64String(encryptionOptions.Value.EncryptionIVBase64);
    }

    public DbSet<Person> People { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var memberInfo = property.PropertyInfo;
                if (memberInfo != null && memberInfo.GetCustomAttribute<SensitiveDataAttribute>() != null)
                {
                    var converterType = typeof(AesEncryptedConverter<>).MakeGenericType(property.ClrType);
                    var converterInstance = Activator.CreateInstance(converterType, _aesKey, _aesIv) as ValueConverter;
                    property.SetValueConverter(converterInstance);
                }
            }
        }
    }
}
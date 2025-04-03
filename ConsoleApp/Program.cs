using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SampleLib;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // IMPORTANT!!! STORE KEY IN SECURE WAY
        var keyFromVault = "bl9Wb+Gzn1TLzPEG47ypBf6CcZPDYcIJw7XCi/7aCSs=";
        var ivFromVault = "eaPymshZlcH0vxXCEZEucw==";

        services.Configure<EncryptionOptions>(opts =>
        {
            opts.EncryptionKeyBase64 = keyFromVault;
            opts.EncryptionIVBase64 = ivFromVault;
        });

        services.AddDbContext<AppDbContext>(dbOptions =>
        {
            dbOptions.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=sampleEncryptionTestDB;Trusted_Connection=True;");
        });
    })
    .Build();

using var scope = host.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

context.Database.EnsureDeleted();
context.Database.EnsureCreated();

// Create & save a person
var person = new Person
{
    Name = "Alice",
    SocialSecurityNumber = "123-45-6789",
    SecretCode = 42
};
context.People.Add(person);
context.SaveChanges();

Console.WriteLine("=== Data saved to EF with AES encryption ===");

// Re-load from DB to prove decryption works
var reloaded = context.People.FirstOrDefault(p => p.Id == person.Id);
if (reloaded != null)
{
    Console.WriteLine($"Name: {reloaded.Name}");
    Console.WriteLine($"SSN: {reloaded.SocialSecurityNumber}  (decrypted by EF)");
    Console.WriteLine($"SecretCode: {reloaded.SecretCode}     (decrypted by EF)");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
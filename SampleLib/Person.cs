namespace SampleLib;

public class Person
{
    public int Id { get; set; }

    // Not encrypted
    public string? Name { get; set; }

    [SensitiveData]
    public string? SocialSecurityNumber { get; set; }

    [SensitiveData]
    public int SecretCode { get; set; }
}

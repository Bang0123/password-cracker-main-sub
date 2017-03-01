namespace MasterCrack.Model
{
    
    public interface IUnencryptedPassword
    {
        string UnencryptedPassword { get; set; }
        bool HasUnencryptedPassword();
    }
}

namespace AlloyClient.Data;

public sealed class LoginData(string guid, string password) : IGlobalData {

    public static readonly LoginData Default = new ("", "");
    
    public readonly string GUID = guid;
    public readonly string Password = password;
    
}
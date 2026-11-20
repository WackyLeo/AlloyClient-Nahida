using AlloyClient.Data;
using AlloyClient.State;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using AlloyClient.ParticleEffects;
using AlloyClient.Screens.Components;
using AlloyClient.UiLib.Extra;
using Timer = System.Timers.Timer;

namespace AlloyClient.AppEngine;

public struct AppResponse
{
    public bool Success;
    public string Message;
}

public static class AppRequests
{
    private static readonly Timer StartTimer = new(11000);

    public static async Task Startup()
    {
        StartTimer.Enabled = true;
        StartTimer.AutoReset = true;
        await CheckConnection(); // Check connection here so if successful, can stop timer before it continues
        StartTimer.Elapsed += async (sender, args) => { await CheckConnection(); };
    }

    private static async Task CheckConnection()
    {
        var result = await VerifyAsync();


        if (result.Message == "Invalid account credentials.")
        {
            StartTimer.Stop();
            Console.WriteLine($"Result: {result.Message}");

            if (GlobalData.Contains<LoginData>())
                GlobalData.Remove<LoginData>();

            Settings.SaveLocalAccount();
            AccountOverlay.OnVerifyAsyncSuccessLoggedOut.Dispatch();
            AccountOverlay.Connected = true;

            return;
        }

        if (result.Message == "Error")
        {
            StartTimer.Stop();
            Console.WriteLine($"Result: {result.Message}");

            if (GlobalData.Contains<LoginData>())
                GlobalData.Remove<LoginData>();

            Settings.SaveLocalAccount();
            AccountOverlay.OnVerifyAsyncSuccessLoggedOut.Dispatch();
            AccountOverlay.Connected = true;

            return;
        }

        if (result.Message != "Failed to contact server.")
        {
            StartTimer.Stop();
            Console.WriteLine($"Result: Success");

            AccountOverlay.OnVerifyAsyncSuccess.Dispatch();
            AccountOverlay.Connected = true;

            await GetCharList();

            await Assets.RemoteAssetLoader.LoadAllAsync();
            return;
        }

        AccountOverlay.Connected = false;
    }

    public static async Task<AppResponse> VerifyAsync()
    {
        var login = GlobalData.Get<LoginData>() ?? LoginData.Default;
        return await VerifyAsync(login.GUID, login.Password);
    }

    public static async Task<AppResponse> VerifyAsync(string guid, string password, bool saveInfo = false)
    {
        var dict = BuildAccountRequestData(guid, password);
        dict.Add("WLrequest", GetLocalIPv4());
        var response = await AppEngineClient.SendRequest("/account/verify", dict, retries: 3);

        if (response == null)
        {
            return new AppResponse { Success = false, Message = "Failed to contact server." };
        }

        if (response.Contains("Invalid"))
        {
            return new AppResponse { Success = false, Message = "Invalid account credentials." };
        }

        if (response.Contains("Error"))
        {
            return new AppResponse { Success = false, Message = response };
        }

        var xml = XElement.Parse(response);
        GlobalData.Add(new AccountData(xml));
        GlobalData.Add(new LoginData(guid, password));

        if (saveInfo)
        {
            Settings.SaveLocalAccount();
        }

        return new AppResponse { Success = true };
    }

    public static async Task<AppResponse> Register(string guid, string password)
    {
        var data = new Dictionary<string, string>
        {
            { "newGUID", guid }, { "Guid", guid }, { "newPassword", password }
        };
        var request = await AppEngineClient.SendRequest("/account/register", data, retries: 3);

        if (request == null)
        {
            return new AppResponse { Success = false, Message = "Failed to contact server." };
        }

        var result = XElement.Parse(request).Value;

        if (result != string.Empty)
        {
            return new AppResponse { Success = false, Message = result };
        }

        return await VerifyAsync(guid, password, true);
    }

    public static async Task<AppResponse> PurchaseCharSlot()
    {
        var login = GlobalData.Get<LoginData>();

        if (login is null)
            return new AppResponse { Success = false, Message = "Not logged in" };

        var response = await AppEngineClient.SendRequest("/account/purchaseCharSlot",
            BuildAccountRequestData(login.GUID, login.Password), retries: 3);

        if (response == null)
            return new AppResponse { Success = false, Message = "Failed to contact server." };

        var result = XElement.Parse(response).Value;

        await GetCharList();

        return result == string.Empty
            ? new AppResponse { Success = true }
            : new AppResponse { Success = false, Message = result };
    }

    // Delete
    public static async Task<AppResponse> DeleteCharacter(string characterId)
    {
        var login = GlobalData.Get<LoginData>() ?? LoginData.Default;

        if (login is null)
            return new AppResponse { Success = false, Message = "Not logged in" };

        var data = BuildAccountRequestData(login.GUID, login.Password);
        data.Add("charId", characterId);

        var response = await AppEngineClient.SendRequest("/char/delete", data, retries: 3);

        if (response == null)
            return new AppResponse { Success = false, Message = "Failed to contact server." };

        var result = XElement.Parse(response).Value;

        return result == string.Empty
            ? new AppResponse { Success = true }
            : new AppResponse { Success = false, Message = result };
    }

    //Rename
    public static async Task<AppResponse> RenameAccount(string newName)
    {
        var login = GlobalData.Get<LoginData>() ?? LoginData.Default;

        if (login is null)
            return new AppResponse { Success = false, Message = "Please log in to choose a name!" };

        var data = BuildAccountRequestData(login.GUID, login.Password);
        data.Add("name", newName);

        var response = await AppEngineClient.SendRequest("/account/setName", data, retries: 3);

        if (response == null)
            return new AppResponse { Success = false, Message = "Failed to contact server." };

        var result = XElement.Parse(response).Value;

        return result == string.Empty
            ? new AppResponse { Success = true }
            : new AppResponse { Success = false, Message = result };
    }
    //Fame

    public static async Task<AppResponse> GetCharList()
    {
        var login = GlobalData.Get<LoginData>() ?? LoginData.Default;
        var response = await AppEngineClient.SendRequest("/char/list",
            BuildAccountRequestData(login.GUID, login.Password), retries: 3);

        if (response == null)
        {
            return new AppResponse { Success = false, Message = "Failed to contact server." };
        }

        if (response.Contains("Failed"))
        {
            return new AppResponse { Success = false, Message = response };
        }

        var xml = XElement.Parse(response);
        var newAccount = new AccountData(xml.Element("Account"));
        var charList = new CharacterListData(xml);
        var newsData = new NewsData(xml.Elements("News"));
        var servers = new ServerListData(xml.Element("Servers"));

        GlobalData.Add(newAccount);
        GlobalData.Add(charList);
        GlobalData.Add(newsData);
        GlobalData.Add(servers);

        return new AppResponse { Success = true };
    }

    private static Dictionary<string, string> BuildAccountRequestData(string guid, string password) =>
        new() { { "guid", guid }, { "password", password } };

    private static string GetLocalIPv4()
    {
        foreach (var ip in Dns.GetHostAddresses(Dns.GetHostName()))
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        }

        return string.Empty;
    }
}
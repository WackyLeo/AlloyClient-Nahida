using System;
using System.Collections.Generic;
using System.Linq;
using AlloyClient.State;

namespace AlloyClient.Data;

public interface IGlobalData;

public static class GlobalData 
{

    private static readonly Dictionary<Type, IGlobalData> DataStorage = [];
    private static readonly Dictionary<Type, IGlobalData> OverriddenDataStorage = [];

    public static int SelectedCharacterId;
    
    public static ushort CharacterType;

    public static bool Overridden = false;

    public static T Get<T>() where T : class, IGlobalData 
    {
        if (DataStorage.TryGetValue(typeof(T), out var data)) 
        {
            return (T)data;
        }

        return null;
    }

    public static bool Contains<T>() where T : class, IGlobalData 
    {
        return DataStorage.ContainsKey(typeof(T));
    }

    public static void Add<T>(T data) where T : class, IGlobalData 
    {
        DataStorage[typeof(T)] = data;
    }

    public static void Remove<T>() where T : class, IGlobalData 
    {
        DataStorage.Remove(typeof(T));
    }
    
    public static T OverriddenGet<T>() where T : class, IGlobalData 
    {
        if (OverriddenDataStorage.TryGetValue(typeof(T), out var data)) 
        {
            return (T)data;
        }

        return null;
    }

    public static bool OverriddenContains<T>() where T : class, IGlobalData 
    {
        return OverriddenDataStorage.ContainsKey(typeof(T));
    }

    public static void OverriddenAdd<T>(T data) where T : class, IGlobalData 
    {
        OverriddenDataStorage[typeof(T)] = data;
    }

    public static void OverriddenRemove<T>() where T : class, IGlobalData 
    {
        OverriddenDataStorage.Remove(typeof(T));
    }

    public static void Logout() 
    {
        DataStorage.Clear();
        OverriddenDataStorage.Clear();
        Settings.SaveLocalAccount();
    }
}
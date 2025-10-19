using System;
using System.Collections.Concurrent;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Utils;

namespace UIFixes.Server;

// This class completely reimplements the ProfileDataService because that thing is broken until 4.1

[Injectable(InjectionType = InjectionType.Singleton)]
public class ProfileDataHelper(FileUtil fileUtil, JsonUtil jsonUtil)
{
    private static readonly string ModKey = "Tyfon.UIFixes";
    private const string ProfileDataFilepath = "user/profileData/";

    private readonly ConcurrentDictionary<MongoId, ProfileData> profileDataCache = [];

    public ProfileData GetProfileData(MongoId profileId)
    {
        if (profileDataCache.TryGetValue(profileId, out var profileData))
        {
            return profileData;
        }

        var filePath = $"{ProfileDataFilepath}{profileId}/{ModKey}.json";

        if (fileUtil.FileExists(filePath))
        {
            profileData = jsonUtil.Deserialize<ProfileData>(fileUtil.ReadFile(filePath));
        }

        if (profileData == null)
        {
            profileData = new();
        }

        profileDataCache[profileId] = profileData;
        return profileData;
    }

    public void SaveProfileData(MongoId profileId)
    {
        if (profileDataCache.TryGetValue(profileId, out var profileData))
        {
            var data = jsonUtil.Serialize(profileData, true);
            if (data == null)
            {
                throw new Exception("UIFixes: ProfileData serialized to null");
            }

            var filePath = $"{ProfileDataFilepath}{profileId}/{ModKey}.json";
            fileUtil.WriteFile(filePath, data);
        }
    }
}
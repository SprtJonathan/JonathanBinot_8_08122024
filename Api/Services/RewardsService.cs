using GpsUtil.Location;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Users;

namespace TourGuide.Services;

public class RewardsService : IRewardsService
{
    private const double StatuteMilesPerNauticalMile = 1.15077945;
    private readonly int _defaultProximityBuffer = 10;
    private int _proximityBuffer;
    private readonly int _attractionProximityRange = 200;
    private readonly IGpsUtil _gpsUtil;
    private readonly IRewardCentral _rewardsCentral;
    private static int count = 0;
    private List<Attraction> _attractionsCache;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public RewardsService(IGpsUtil gpsUtil, IRewardCentral rewardCentral)
    {
        _gpsUtil = gpsUtil;
        _rewardsCentral =rewardCentral;
        _proximityBuffer = _defaultProximityBuffer;
    }

    private async Task<List<Attraction>> GetAttractionsCachedAsync()
    {
        if (_attractionsCache == null)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_attractionsCache == null)
                {
                    _attractionsCache = await _gpsUtil.GetAttractionsAsync();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
        return _attractionsCache;
    }

    public void SetProximityBuffer(int proximityBuffer)
    {
        _proximityBuffer = proximityBuffer;
    }

    public void SetDefaultProximityBuffer()
    {
        _proximityBuffer = _defaultProximityBuffer;
    }

    public async Task CalculateRewardsAsync(User user)
    {
        List<Attraction> attractions = await GetAttractionsCachedAsync();
        List<VisitedLocation> userLocations = user.VisitedLocations;

        // Créer un HashSet des attractions déjà récompensées pour des vérifications rapides
        var rewardedAttractions = new HashSet<string>(user.UserRewards.Select(r => r.Attraction.AttractionName));
        var lockObject = new object(); // Objet de verrouillage partagé

        // Créer une liste de tâches asynchrones pour chaque VisitedLocation
        var tasks = userLocations.Select(async visitedLocation =>
        {
            foreach (var attraction in attractions)
            {
                bool alreadyRewarded;
                lock (lockObject)
                {
                    alreadyRewarded = rewardedAttractions.Contains(attraction.AttractionName);
                }

                if (!alreadyRewarded)
                {
                    if (NearAttraction(visitedLocation, attraction))
                    {
                        int points = await GetRewardPointsAsync(attraction, user);
                        lock (lockObject)
                        {
                            // Vérifier à nouveau pour éviter les doublons en concurrence
                            if (!rewardedAttractions.Contains(attraction.AttractionName))
                            {
                                user.AddUserReward(new UserReward(visitedLocation, attraction, points));
                                rewardedAttractions.Add(attraction.AttractionName);
                            }
                        }
                    }
                }
            }
        });

        // Attendre que toutes les tâches soient terminées
        await Task.WhenAll(tasks);
    }

    public bool IsWithinAttractionProximity(Attraction attraction, Locations location)
    {
        Console.WriteLine(GetDistance(attraction, location));
        return GetDistance(attraction, location) <= _attractionProximityRange;
    }

    private bool NearAttraction(VisitedLocation visitedLocation, Attraction attraction)
    {
        return GetDistance(attraction, visitedLocation.Location) <= _proximityBuffer;
    }

    private async Task<int> GetRewardPointsAsync(Attraction attraction, User user)
    {
        return await _rewardsCentral.GetAttractionRewardPointsAsync(attraction.AttractionId, user.UserId);
    }

    public double GetDistance(Locations loc1, Locations loc2)
    {
        double lat1 = Math.PI * loc1.Latitude / 180.0;
        double lon1 = Math.PI * loc1.Longitude / 180.0;
        double lat2 = Math.PI * loc2.Latitude / 180.0;
        double lon2 = Math.PI * loc2.Longitude / 180.0;

        double angle = Math.Acos(Math.Sin(lat1) * Math.Sin(lat2)
                                + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2));

        double nauticalMiles = 60.0 * angle * 180.0 / Math.PI;
        return StatuteMilesPerNauticalMile * nauticalMiles;
    }
}

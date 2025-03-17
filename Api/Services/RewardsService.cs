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
    private List<Attraction>? _attractionsCache;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); 
    private static int count = 0;

    public RewardsService(IGpsUtil gpsUtil, IRewardCentral rewardCentral)
    {
        _gpsUtil = gpsUtil;
        _rewardsCentral = rewardCentral;
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
        // Créer des copies des collections pour éviter les modifications concurrentes
        List<VisitedLocation> userLocationsCopy = new List<VisitedLocation>(user.VisitedLocations);
        List<Attraction> attractionsCopy = new List<Attraction>(await GetAttractionsCachedAsync());

        // Objet pour synchroniser l'accès à UserRewards
        object lockObject = new object();

        await Task.WhenAll(userLocationsCopy.Select(async visitedLocation =>
        {
            foreach (var attraction in attractionsCopy)
            {
                bool shouldAddReward = false;

                // Vérifier si la récompense existe déjà
                lock (lockObject)
                {
                    if (!user.UserRewards.Any(r => r.Attraction.AttractionName == attraction.AttractionName))
                    {
                        shouldAddReward = true;
                    }
                }

                if (shouldAddReward)
                {
                    if (NearAttraction(visitedLocation, attraction))
                    {
                        int rewardPoints = await GetRewardPointsAsync(attraction, user);

                        // Ajouter la récompense de manière thread-safe
                        lock (lockObject)
                        {
                            if (!user.UserRewards.Any(r => r.Attraction.AttractionName == attraction.AttractionName))
                            {
                                user.AddUserReward(new UserReward(visitedLocation, attraction, rewardPoints));
                            }
                        }
                    }
                }
            }
        }));
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

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
        List<VisitedLocation> userLocationsCopy = new List<VisitedLocation>(user.VisitedLocations);
        List<Attraction> attractionsCopy = new List<Attraction>(await GetAttractionsCachedAsync());

        // Collecter les récompenses potentielles sans calculer les points immédiatement
        List<(VisitedLocation location, Attraction attraction)> potentialRewards = new List<(VisitedLocation, Attraction)>();
        object lockObject = new object();

        await Task.WhenAll(userLocationsCopy.Select(visitedLocation =>
            Task.Run(() =>
            {
                foreach (var attraction in attractionsCopy)
                {
                    lock (lockObject)
                    {
                        if (!user.UserRewards.Any(r => r.Attraction.AttractionName == attraction.AttractionName) &&
                            NearAttraction(visitedLocation, attraction))
                        {
                            potentialRewards.Add((visitedLocation, attraction));
                        }
                    }
                }
            })
        ));

        var rewardTasks = potentialRewards.Select(async pr =>
        {
            int points = await GetRewardPointsAsync(pr.attraction, user);
            return (pr.location, pr.attraction, points);
        });
        var rewardsToAdd = await Task.WhenAll(rewardTasks);

        lock (lockObject)
        {
            foreach (var (location, attraction, points) in rewardsToAdd)
            {
                if (!user.UserRewards.Any(r => r.Attraction.AttractionName == attraction.AttractionName))
                {
                    user.AddUserReward(new UserReward(location, attraction, points));
                }
            }
        }
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

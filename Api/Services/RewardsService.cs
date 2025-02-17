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

    public RewardsService(IGpsUtil gpsUtil, IRewardCentral rewardCentral)
    {
        _gpsUtil = gpsUtil;
        _rewardsCentral =rewardCentral;
        _proximityBuffer = _defaultProximityBuffer;
    }

    private async Task<List<Attraction>> GetAttractionsCached()
    {
        return await _gpsUtil.GetAttractionsAsync();
    }

    public void SetProximityBuffer(int proximityBuffer)
    {
        _proximityBuffer = proximityBuffer;
    }

    public void SetDefaultProximityBuffer()
    {
        _proximityBuffer = _defaultProximityBuffer;
    }

    //public void CalculateRewards(User user)
    //{
    //    count++;
    //    List<VisitedLocation> userLocations = user.VisitedLocations;
    //    List<Attraction> attractions = GetAttractionsCached(); // étudier le ConcurrentBag pour la récupération de la liste

    //    Parallel.ForEach(userLocations, visitedLocation =>
    //    {
    //        foreach (var attraction in attractions)
    //        {               
    //            if (!user.UserRewards.Any(r => r.Attraction.AttractionName == attraction.AttractionName))
    //            {
    //                if (NearAttraction(visitedLocation, attraction))
    //                {
    //                    // Verrouiller l'accès à la collection pour l'itération et l'ajout
    //                    lock (user.UserRewards)
    //                    {
    //                        // Vérifier de nouveau dans le lock pour éviter les doublons en cas de concurrence
    //                        if (!user.UserRewards.Any(r => r.Attraction.AttractionName == attraction.AttractionName))
    //                        {
    //                            user.AddUserReward(new UserReward(visitedLocation, attraction, GetRewardPoints(attraction, user)));
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    });
    //}

    public async Task CalculateRewardsAsync(User user)
    {
        count++;
        List<VisitedLocation> userLocations = user.VisitedLocations;
        List<Attraction> attractions = await GetAttractionsCached();

        var tasks = userLocations.Select(async visitedLocation =>
        {
            foreach (var attraction in attractions)
            {
                if (!user.UserRewards.Any(r => r.Attraction.AttractionName == attraction.AttractionName))
                {
                    if (NearAttraction(visitedLocation, attraction))
                    {
                        lock (user.UserRewards)
                        {
                            if (!user.UserRewards.Any(r => r.Attraction.AttractionName == attraction.AttractionName))
                            {
                                user.AddUserReward(new UserReward(visitedLocation, attraction, GetRewardPoints(attraction, user)));
                            }
                        }
                    }
                }
            }
        });

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

    private int GetRewardPoints(Attraction attraction, User user)
    {
        return _rewardsCentral.GetAttractionRewardPoints(attraction.AttractionId, user.UserId);
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

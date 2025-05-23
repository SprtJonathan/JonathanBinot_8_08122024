﻿using GpsUtil.Location;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Users;
using TourGuide.Utilities;
using TripPricer;

namespace TourGuide.Services;

public class TourGuideService : ITourGuideService
{
    private readonly ILogger _logger;
    private readonly IGpsUtil _gpsUtil;
    private readonly IRewardsService _rewardsService;
    private readonly TripPricer.TripPricer _tripPricer;
    public Tracker Tracker { get; private set; }
    private readonly Dictionary<string, User> _internalUserMap = new();
    private const string TripPricerApiKey = "test-server-api-key";
    private bool _testMode = true;

    public TourGuideService(ILogger<TourGuideService> logger, IGpsUtil gpsUtil, IRewardsService rewardsService, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _tripPricer = new();
        _gpsUtil = gpsUtil;
        _rewardsService = rewardsService;

        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        if (_testMode)
        {
            _logger.LogInformation("TestMode enabled");
            _logger.LogDebug("Initializing users");
            InitializeInternalUsers();
            _logger.LogDebug("Finished initializing users");
        }

        var trackerLogger = loggerFactory.CreateLogger<Tracker>();

        Tracker = new Tracker(this, trackerLogger);
        AddShutDownHook();
    }

    public List<UserReward> GetUserRewards(User user)
    {
        return user.UserRewards;
    }

    public async Task<VisitedLocation> GetUserLocation(User user)
    {
        return user.VisitedLocations.Any() ? user.GetLastVisitedLocation() : await TrackUserLocation(user);
    }

    public User GetUser(string userName)
    {
        return _internalUserMap.ContainsKey(userName) ? _internalUserMap[userName] : null;
    }

    public Task<List<User>> GetAllUsers()
    {
        return Task.FromResult(_internalUserMap.Values.ToList());
    }

    public void AddUser(User user)
    {
        if (!_internalUserMap.ContainsKey(user.UserName))
        {
            _internalUserMap.Add(user.UserName, user);
        }
    }

    public List<Provider> GetTripDeals(User user)
    {
        int cumulativeRewardPoints = user.UserRewards.Sum(i => i.RewardPoints);
        List<Provider> providers = _tripPricer.GetPrice(TripPricerApiKey, user.UserId,
            user.UserPreferences.NumberOfAdults, user.UserPreferences.NumberOfChildren,
            user.UserPreferences.TripDuration, cumulativeRewardPoints);
        user.TripDeals = providers;
        return providers;
    }

    public async Task<VisitedLocation> TrackUserLocation(User user)
    {
        VisitedLocation visitedLocation = await _gpsUtil.GetUserLocationAsync(user.UserId);
        user.AddToVisitedLocations(visitedLocation);
        await _rewardsService.CalculateRewardsAsync(user);
        return visitedLocation;
    }


    public List<Attraction> GetNearByAttractions(VisitedLocation visitedLocation)
    {
        // On récupère toutes les attractions
        var allAttractions = _gpsUtil.GetAttractionsAsync().GetAwaiter().GetResult();

        // On trie les attractions par distance croissante par rapport à la position de l'utilisateur
        var closestAttractions = allAttractions
            .OrderBy(attraction => _rewardsService.GetDistance(
                new Locations(attraction.Latitude, attraction.Longitude),
                visitedLocation.Location))
            .Take(5) // ON ne garde que les 5 premières
            .ToList();

        return closestAttractions;
    }

    private void AddShutDownHook()
    {
        AppDomain.CurrentDomain.ProcessExit += (sender, e) => Tracker.StopTracking();
    }

    /**********************************************************************************
    * 
    * Methods Below: For Internal Testing
    * 
    **********************************************************************************/
    private static readonly Random SharedRandom = new Random();

    private void InitializeInternalUsers()
    {
        Parallel.For(0, InternalTestHelper.GetInternalUserNumber(), i =>
        {
            var userName = $"internalUser{i}";
            var user = new User(Guid.NewGuid(), userName, "000", $"{userName}@tourGuide.com");
            GenerateUserLocationHistory(user);
            lock (_internalUserMap)
            {
                _internalUserMap.Add(userName, user);
            }
        });

        _logger.LogDebug($"Created {InternalTestHelper.GetInternalUserNumber()} internal test users.");
    }

    private void GenerateUserLocationHistory(User user)
    {
        Parallel.For(0, 3, i =>
        {
            lock (SharedRandom) // Verrou pour garantir des valeurs aléatoires uniques
            {
                var visitedLocation = new VisitedLocation(
                    user.UserId,
                    new Locations(GenerateRandomLatitude(), GenerateRandomLongitude()),
                    GetRandomTime());
                user.AddToVisitedLocations(visitedLocation);
            }
        });
    }

    private double GenerateRandomLongitude()
    {
        return SharedRandom.NextDouble() * (180 - (-180)) + (-180);
    }

    private double GenerateRandomLatitude()
    {
        return SharedRandom.NextDouble() * (90 - (-90)) + (-90);
    }

    private DateTime GetRandomTime()
    {
        return DateTime.UtcNow.AddDays(-SharedRandom.Next(30));
    }
}

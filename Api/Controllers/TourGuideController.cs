using GpsUtil.Location;
using Microsoft.AspNetCore.Mvc;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services;
using TourGuide.Services.Interfaces;
using TourGuide.Users;
using TripPricer;

namespace TourGuide.Controllers;

[ApiController]
[Route("[controller]")]
public class TourGuideController : ControllerBase
{
    private readonly ITourGuideService _tourGuideService;

    public TourGuideController(ITourGuideService tourGuideService)
    {
        _tourGuideService = tourGuideService;
    }

    [HttpGet("getLocation")]
    public ActionResult<VisitedLocation> GetLocation([FromQuery] string userName)
    {
        var location = _tourGuideService.GetUserLocation(GetUser(userName));
        return Ok(location);
    }

    // TODO: Change this method to no longer return a List of Attractions.
    // Instead: Get the closest five tourist attractions to the user - no matter how far away they are.
    // Return a new JSON object that contains:
    // Name of Tourist attraction, 
    // Tourist attractions lat/long, 
    // The user's location lat/long, 
    // The distance in miles between the user's location and each of the attractions.
    // The reward points for visiting each Attraction.
    //    Note: Attraction reward points can be gathered from RewardsCentral
    [HttpGet("getNearbyAttractions")]
    public ActionResult<List<object>> GetNearbyAttractions(
    [FromQuery] string userName,
    [FromServices] IRewardsService rewardService,
    [FromServices] IRewardCentral rewardCentral)
    {
        var user = new User(Guid.NewGuid(), "jon", "000", "jon@tourGuide.com")/* GetUser(userName)*/;
        var visitedLocation = _tourGuideService.GetUserLocation(user);
        List<Attraction> closestAttractions = _tourGuideService.GetNearByAttractions(visitedLocation);

        var result = closestAttractions.Select(attraction => new
        {
            AttractionName = attraction.AttractionName,
            AttractionLocation = new Locations(attraction.Latitude, attraction.Longitude),
            UserLocation = visitedLocation.Location,
            DistanceFromAttraction = rewardService.GetDistance(
                new Locations(attraction.Latitude, attraction.Longitude),
                visitedLocation.Location),
            RewardPoints = rewardCentral.GetAttractionRewardPoints(attraction.AttractionId, visitedLocation.UserId)
        }).ToList();

        return Ok(result);
    }

    [HttpGet("getRewards")]
    public ActionResult<List<UserReward>> GetRewards([FromQuery] string userName)
    {
        var rewards = _tourGuideService.GetUserRewards(GetUser(userName));
        return Ok(rewards);
    }

    [HttpGet("getTripDeals")]
    public ActionResult<List<Provider>> GetTripDeals([FromQuery] string userName)
    {
        var deals = _tourGuideService.GetTripDeals(GetUser(userName));
        return Ok(deals);
    }

    private User GetUser(string userName)
    {
        return _tourGuideService.GetUser(userName);
    }
}

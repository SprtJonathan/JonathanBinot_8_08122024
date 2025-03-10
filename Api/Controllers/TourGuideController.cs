using GpsUtil.Location;
using Microsoft.AspNetCore.Mvc;
using TourGuide.LibrairiesWrappers.Interfaces;
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

    [HttpGet("getNearbyAttractions")]
    public async Task<ActionResult<List<object>>> GetNearbyAttractionsAsync(
    [FromQuery] string userName,
    [FromServices] IRewardsService rewardService,
    [FromServices] IRewardCentral rewardCentral)
    {
        var user = GetUser(userName);
        var visitedLocation = await _tourGuideService.GetUserLocation(user);
        List<Attraction> closestAttractions = _tourGuideService.GetNearByAttractions(visitedLocation);

        var result = closestAttractions.Select(attraction => new
        {
            attraction.AttractionName,
            AttractionLocation = new Locations(attraction.Latitude, attraction.Longitude),
            UserLocation = visitedLocation.Location,
            DistanceFromAttraction = rewardService.GetDistance(
                new Locations(attraction.Latitude, attraction.Longitude),
                visitedLocation.Location),
            RewardPoints = rewardCentral.GetAttractionRewardPointsAsync(attraction.AttractionId, visitedLocation.UserId)
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

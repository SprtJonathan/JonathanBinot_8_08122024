using GpsUtil.Location;
using TourGuide.Users;

namespace TourGuideTest;

public class RewardServiceTest : IClassFixture<DependencyFixture>
{
    private readonly DependencyFixture _fixture;

    public RewardServiceTest(DependencyFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UserGetRewards()
    {
        _fixture.Initialize(0);
        var user = new User(Guid.NewGuid(), "jon", "000", "jon@tourGuide.com");
        var attractions = await _fixture.GpsUtil.GetAttractionsAsync();
        var attraction = attractions[0];
        user.AddToVisitedLocations(new VisitedLocation(user.UserId, attraction, DateTime.Now));
        await _fixture.TourGuideService.TrackUserLocation(user);
        var userRewards = user.UserRewards;
        _fixture.TourGuideService.Tracker.StopTracking();
        Assert.True(userRewards.Count == 1);
    }

    [Fact]
    public async Task IsWithinAttractionProximity()
    {
        var attractions = await _fixture.GpsUtil.GetAttractionsAsync();
        var attraction = attractions[0];
        Assert.True(_fixture.RewardsService.IsWithinAttractionProximity(attraction, attraction));
    }

    [Fact]
    public async Task NearAllAttractions()
    {
        _fixture.Initialize(1);
        _fixture.RewardsService.SetProximityBuffer(int.MaxValue);

        var users = await _fixture.TourGuideService.GetAllUsers();
        var user = users[0];
        await _fixture.RewardsService.CalculateRewardsAsync(user);
        var userRewards = _fixture.TourGuideService.GetUserRewards(user);
        _fixture.TourGuideService.Tracker.StopTracking();

        var attractions = await _fixture.GpsUtil.GetAttractionsAsync();

        Assert.Equal(attractions.Count, userRewards.Count);
    }
}

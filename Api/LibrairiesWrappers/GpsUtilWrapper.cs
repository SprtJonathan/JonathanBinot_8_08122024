using GpsUtil.Location;
using TourGuide.LibrairiesWrappers.Interfaces;

namespace TourGuide.LibrairiesWrappers;

public class GpsUtilWrapper : IGpsUtil
{
    private readonly GpsUtil.GpsUtil _gpsUtil;

    public GpsUtilWrapper()
    {
        _gpsUtil = new();
    }

    public VisitedLocation GetUserLocation(Guid userId)
    {
        return _gpsUtil.GetUserLocation(userId);
    }
    public async Task<VisitedLocation> GetUserLocationAsync(Guid userId)
    {
        return await _gpsUtil.GetUserLocationAsync(userId);
    }

    public List<Attraction> GetAttractions()
    {
        return _gpsUtil.GetAttractionsAsync().GetAwaiter().GetResult();
    }

    public async Task<List<Attraction>> GetAttractionsAsync()
    {
        return await _gpsUtil.GetAttractionsAsync();
    }
}

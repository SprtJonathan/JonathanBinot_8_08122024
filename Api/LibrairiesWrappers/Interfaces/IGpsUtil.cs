using GpsUtil.Location;

namespace TourGuide.LibrairiesWrappers.Interfaces
{
    public interface IGpsUtil
    {
        VisitedLocation GetUserLocation(Guid userId);
        Task<VisitedLocation> GetUserLocationAsync(Guid userId);
        List<Attraction> GetAttractions();
    }
}

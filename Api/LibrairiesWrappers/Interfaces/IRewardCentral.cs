namespace TourGuide.LibrairiesWrappers.Interfaces
{
    public interface IRewardCentral
    {
        Task<int> GetAttractionRewardPointsAsync(Guid attractionId, Guid userId);
    }
}

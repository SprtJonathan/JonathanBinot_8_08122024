using TourGuide.LibrairiesWrappers.Interfaces;

namespace TourGuide.LibrairiesWrappers
{
    public class RewardCentralWrapper : IRewardCentral
    {
        private readonly RewardCentral.RewardCentral _rewardCentral;

        public RewardCentralWrapper()
        {
            _rewardCentral = new ();
        }

        public async Task<int> GetAttractionRewardPointsAsync(Guid attractionId, Guid userId)
        {
            return await Task.FromResult(await _rewardCentral.GetAttractionRewardPointsAsync(attractionId, userId));
        }
    }
}

namespace RewardCentral;

public class RewardCentral
{
    public async Task<int> GetAttractionRewardPointsAsync(Guid attractionId, Guid userId)
    {
        int randomDelay = new Random().Next(1, 1000);
        await Task.Delay(randomDelay);

        int randomInt = new Random().Next(1, 1000);
        return randomInt;
    }
}

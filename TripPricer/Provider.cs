namespace TripPricer;

public class Provider
{
    public string Name { get; }
    public double Price { get; }
    public Guid TripId { get; }

    public Provider(Guid tripId, string name, double price)
    {
        Name = name;
        TripId = tripId;
        Price = price;
    }
}

using static Exit;

public class ParkingSpot
{
    public SpotType spotType { get; set; }
    public int spotId;

    public ParkingSpot()
    {
       // IsAvailable = true;
    }
}


public class Vehicle
{
    public string LicensePlate
    {
        get; private set;
    }

    public SpotType requiredSpotType
    {
        get; private set;
    }

    public Vehicle(string licensePlate, SpotType requiredSpotType)
    {
        LicensePlate = licensePlate;
        this.requiredSpotType = requiredSpotType;
    }
}

    public class Ticket
{
    public int TicketId { get; private set; }
    public int SpotId { get;  set; }
    public DateTime ParkTime { get; set; }

    public DateTime ExitTime { get; set; }

    public Vehicle vehicle { get; set; }

    public int cost { get; set; }
}

public class Entrance
{
    // public ParkingSpot[] ParkingSpots { get; set; }
    private ParkingLot parkingLot;

    public Entrance(ParkingLot parkingLot)
    {
        this.parkingLot = parkingLot;
    }

    public Ticket TakeTicket(Vehicle vehicle)
    {
        if (!parkingLot.IsSpotAvailabel(vehicle.requiredSpotType))
        {
            throw new Exception("No available spot");
        }

        Ticket ticket = new Ticket();
        ticket.vehicle = vehicle;
        ticket.ParkTime = DateTime.Now;
        
        ticket.SpotId = parkingLot.TakenOneSpot(vehicle.requiredSpotType);
        return ticket;
    }
}


public class DisplayBoard
{
    private ParkingLot parkingLot;

  public Dictionary<SpotType, int> availableSpots
    {
        get
        {
            return parkingLot.FreeParkingSpotCount;
        }
    }

public DisplayBoard(ParkingLot parkingLot)
    {
        this.parkingLot = parkingLot;
    }

}

public class Customer
{
    public string LicensePlate { get; set; }
    public Ticket ticket { get; set; }

    public string CreditcardInfo { get; set; }

}

public class Exit
{
    private int hourlyAtDay = 5;
    private int hourlyAtNights = 2;
    private ParkingLot parkingLot;
    private Ticket curTicket;
    public void InsertTicket(Ticket ticket)
    {
        var now = DateTime.Now;
        var timeParked = now - ticket.ParkTime;
        var cost = now.Hour > 17 && now.Hour < 8 ? timeParked.Hours * hourlyAtNights : timeParked.Hours * hourlyAtDay;

     //   ticket.spot.IsAvailable = true;
        ticket.ExitTime = now;
        ticket.cost = cost;
        curTicket = ticket;

        parkingLot.FreeParkingSpot(ticket.SpotId, ticket.vehicle.requiredSpotType );
    }

    public void Pay(string creditCardInfO)
    {

    }
}

public enum SpotType
{
    Compact,
    large,
    motorcycle,
    handicapped
}

    public class ParkingLot
{
    private int spotCapacity;
    private Dictionary<SpotType, HashSet<int>> freeParkingSpots;
    public Dictionary<SpotType, int> FreeParkingSpotCount;
    private Dictionary<SpotType, HashSet<int>> takenParkignSpots;

  //  private Dictionary<int, ParkingSpot> takenSpots;
  //  public Dictionary<int, ParkingSpot> freeSpots { get; private set; }

  //  private SpotChangedEventHandler spotChangedFunc;

 //   public delegate void SpotChangedEventHandler(bool free, int spotId, SpotType spotType);

    public ParkingLot(int capacity, List<ParkingSpot> parkingSpotList)//, SpotChangedEventHandler spotChangedFunc)
    {
        spotCapacity = capacity;
        freeParkingSpots = new Dictionary<SpotType, HashSet<int>>();
        takenParkignSpots = new Dictionary<SpotType, HashSet<int>>();
        FreeParkingSpotCount = new Dictionary<SpotType, int>();
        //   takenSpots = new Dictionary<int, ParkingSpot>();
        //  freeSpots = new Dictionary<int, ParkingSpot>();
        for (int i = 0; i < parkingSpotList.Count; i++)
        {
            if (!freeParkingSpots.ContainsKey(parkingSpotList[i].spotType))
            {
                freeParkingSpots[parkingSpotList[i].spotType] = new HashSet<int>();
            }

            freeParkingSpots[parkingSpotList[i].spotType].Add(i);
            
//            freeSpots.Add(i, new ParkingSpot());
        }
    //    this.spotChangedFunc = spotChangedFunc;
    }

    public bool IsSpotAvailabel(SpotType spotType)
    {
        return freeParkingSpots.ContainsKey(spotType) && freeParkingSpots[spotType].Count > 0;
    }


    public int TakenOneSpot(SpotType spotType)
    {
        if (!freeParkingSpots.ContainsKey(spotType) || freeParkingSpots[spotType].Count == 0)
        {
            throw new Exception("No Available spot");
        }

        int spotId = freeParkingSpots[spotType].First();
        freeParkingSpots[spotType].Remove(spotId);

      //  this.spotChangedFunc(false, spotId, spotType);
        FreeParkingSpotCount[spotType] = freeParkingSpots[spotType].Count;
        return spotId;

        throw new Exception("spot does not exist");
    }

    public void FreeParkingSpot(int spotId, SpotType spotType)
    {
        if (!freeParkingSpots.ContainsKey(spotType))
        {
            throw new Exception($"wrong spottype {spotType} does not exist");
        }

        var spot = freeParkingSpots[spotType].Add(spotId);
        FreeParkingSpotCount[spotType] = freeParkingSpots[spotType].Count;
   //     this.spotChangedFunc(true, spotId, spotType);

    }
}

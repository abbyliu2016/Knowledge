public enum VehicleType
{
    Car,
    Truck,
    Van,
    Motorcycle
};

public class Vehicle
{
    public VehicleType vehicleType { get; set; }
    public string licensePlate { get; set; }
    public string barcode { get; set; }
    public int parkingNumber { get; set; }
}

public class Customer
{
    public string name
    {
        get; set;
    }

    public string email
    {
        get; set;

    }

    public string driverLicense
    {
        get; set;
    }

    public string ReservationId
    {
        get; set;
    }

    public Vehicle vehicle
    {
        get; set;
    }

    public string insuranceId
    {
        get; set;
    }

    

}

    public class Reservation
{
    public string Id { get; set; }
//    public Vehicle vehicle { get; set; }
    public DateTime startTime { get; set; }
    public DateTime endTime { get; set; }
    public Customer customer { get; set; }
    public List<string> equipments { get; set; }

    public List<string> services { get; set; }
  //  public string InsuranceId { get; set; }
    public int cost { get; set; }
    public States state { get; set; }
}

public enum States
{
    Reserved,
    CheckedOut,
    Returned,
    Canceled
}

public class  Event
{
    public string barcode { get; set; }
    public string emailAdress { get; set; }
    public string reservationId { get; set; }
    public string states { get; set; }
}

public class RentalProgram
{
    private Dictionary<VehicleType, HashSet<string>> freeVehicle;

    private Dictionary<string, Vehicle> vehicles;

    private Dictionary<string, Reservation> reservations;

    private Dictionary<VehicleType, HashSet<string>> takenVehicles;

    private PriorityQueue<string, DateTime> reservationPQ;

    public RentalProgram(List<Vehicle> vehicles)
    {
        freeVehicle = new Dictionary<VehicleType, HashSet<string>>();
        vehicles = new Dictionary<string, Vehicle>();
        reservations = new Dictionary<string, Reservation>();
        takenVehicles = new Dictionary<VehicleType, HashSet<string>>();
        reservationPQ = new PriorityQueue<string, DateTime>();

        foreach (var vehicle in vehicles)
        {
            if (!this.freeVehicle.ContainsKey(vehicle.vehicleType))
            {
                this.freeVehicle[vehicle.vehicleType] = new HashSet<string>();
            }
            freeVehicle[vehicle.vehicleType].Add(vehicle.barcode);
            this.vehicles[vehicle.barcode] = vehicle;
        }
    }
    public Reservation Reserve(Customer customer, List<string> additionalEquipment, List<string> additionalServices)
    {
        var vehicle = customer.vehicle;
        var vehicalType = vehicle.vehicleType;
        if (!freeVehicle.ContainsKey(vehicalType) || freeVehicle[vehicalType].Count == 0)
        {
            throw new Exception("No available vehicle");
        }

        var barcode = freeVehicle[vehicalType].First();
        if (!takenVehicles.ContainsKey(vehicalType))
        {
            takenVehicles[vehicalType] = new HashSet<string>();
        }
        takenVehicles[vehicalType].Add(barcode);

        var reservation = new Reservation()
        {
            Id = Guid.NewGuid().ToString(),
            customer = customer,
            startTime = DateTime.Now,
            equipments = additionalEquipment,
            services = additionalServices,
            state = States.Reserved
        };

        reservationPQ.Enqueue(reservation.Id, reservation.startTime);

        return reservation;
    }

    public void Rent(string reservationId)
    {
        if (!reservations.ContainsKey(reservationId))
        {
            throw new Exception("Reservation not found");
        }

        reservations[reservationId].state = States.CheckedOut;
    }

        public void Return(string reservationId)
    {
        //Charge late fee
        if (!reservations.ContainsKey(reservationId))
        {
            throw new Exception("Reservation not found");
        }

        reservations[reservationId].state = States.Returned;
        freeVehicle[reservations[reservationId].customer.vehicle.vehicleType].Add(reservations[reservationId].customer.vehicle.barcode);
        takenVehicles[reservations[reservationId].customer.vehicle.vehicleType].Remove(reservations[reservationId].customer.vehicle.barcode);
    }

    private Task CheckAsync()
    {
        while(true)
        {
            // Notify if the reservation is close or near the due date or after the due date

            while(reservationPQ.Count > 0 && reservations[reservationPQ.Peek()].startTime >= DateTime.Now.AddSeconds(-2000))
            {
                var tmpRes = reservationPQ.Dequeue();
                if (reservations[tmpRes].state != States.Reserved)
                {
                    continue;
                }
              
                // Notify customer;

            }

            await Task.Delay(1000);
        }
    }


    public bool Scan(string barcode)
    {

    }

    public bool CancelReservation(string reservationId)
    {
        if (!reservations.ContainsKey(reservationId) || reservations[reservationId].state != States.Reserved)
        {
            return false;
        }

        var tmpRes = reservations[reservationId];
        tmpRes.state = States.Canceled;
        freeVehicle[reservations[reservationId].customer.vehicle.vehicleType].Add(reservations[reservationId].customer.vehicle.barcode);
        takenVehicles[reservations[reservationId].customer.vehicle.vehicleType].Remove(reservations[reservationId].customer.vehicle.barcode);
        return true;
    }
}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[BsonIgnoreExtraElements]
public class Snapshot
{
    [BsonId]
    //public ObjectId ObjectId { get; set; }
    public string ObjectId { get; set; }
    public string carID { get; set; }
    public double speed { get; set; }
    public DateTime timestamp { get; set; }
    public double longitude { get; set; }
    public double latitude { get; set; }
    public string zbeename { get; set; }
    public double distanceTraveled { get; set; }
}

[BsonIgnoreExtraElements]
public class SimJobRouteModel
{
    public SimJobRouteModel()
    {
        Snapshots = new List<Snapshot>();
    }
    [BsonId]
    public string RouteId { get; set; }
    public string carID { get; set; }
    public DateTime timestamp { get; set; } // timestamp of last (newest) snapshot 

    public List<Snapshot> Snapshots { get; set; }

    // list of snapshot follows........
}

[BsonIgnoreExtraElements]
public class SimJobModel
{
    public SimJobModel()
    {
        Routes = new List<SimJobRouteModel>();
    }
    [BsonId]
    //public ObjectId SimJobId { get; set; }
    public string SimJobId { get; set; }
    public string simJobId { get; set; }    // temp hack
    public string carId { get; set; }
    public bool repeatJob { get; set; }
    public UInt16 speedX { get; set; } // speed simulation multiplier  
    public bool jobStarted { get; set; }

    public List<SimJobRouteModel> Routes { get; set; }
    public int nrOfRoutes { get; set; }
}

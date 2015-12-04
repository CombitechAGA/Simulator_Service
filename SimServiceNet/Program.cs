
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Web;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading;

namespace SimServiceNet
{
    public class SimulatedCar
    {
        // State information used in the task.
        SimJobModel taskSimJob;
        MqttClient taskClient;
        bool taskRepeat;
        UInt16 taskSpeedX;
        string taskCarID;
        
        // The constructor obtains the state information.
        public SimulatedCar(MqttClient client, SimJobModel simjob)
        {
            taskSimJob = simjob;
            taskClient = client;
        }

        public bool UpdateSimulatedCar(string CarID, bool repeat, UInt16 speedX)
        {
            taskRepeat = repeat;
            taskSpeedX = speedX;
            taskCarID = CarID;
            return true;
        }

        public void ThreadProc()
        {
            Console.WriteLine("Simjob started.\n");
            do
            {
                if (taskSimJob.jobStarted == true)
                {
                    foreach (var route in taskSimJob.Routes)
                    {
                        foreach (var snapshot in route.Snapshots)
                        {
                            string strValue = snapshot.carID + ";ts:" + snapshot.timestamp.GetMillisecondsSince1970() + ";fuel:"+ snapshot.fuel + ";speed:" + snapshot.speed + ";distance:" + snapshot.distanceTraveled + ";long:"/*11.9347495"*/ + snapshot.longitude + ";lat:" + snapshot.latitude + ";name:" + taskCarID; //taskSimJob.carId;
                            Console.WriteLine(strValue);
                            string js = JsonConvert.SerializeObject(snapshot);
                            taskClient.Publish("telemetry/snapshot", Encoding.UTF8.GetBytes(strValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
                            Thread.Sleep(1000/taskSpeedX); // publish each cycle per simjob
                        }
                    }
                }
            } while (taskRepeat);

            do 
            { 
                Thread.Sleep(10000); // sleep until termination from main thread (jobstarted = false)
            } while (true);
        }
    }
        
    class Program
    {
        public static object Resources { get; private set; }
        public static string Certificate { get; private set; }
        public static List<SimJobStatus> simJobStatus = new List<SimJobStatus>();
        public static int simJobIndex = 0;

        static void Main(string[] args)
        {
            Get().Wait();
        }

        void client_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            Console.WriteLine("MessageId = " + e.MessageId + " Published = " + e.IsPublished);
        }

        static void resetSimJob()
        {
            foreach (var simjob in simJobStatus)
            {
                simjob.active = false;
            }
        }

        static int findSimJob(SimJobModel simjobfind)
        {
            var index = 0;
            foreach (var simjobs in simJobStatus)
            {
                if (simjobs.ObjectId.Equals(simjobfind.SimJobId))
                {
                    return index;
                }
                index++;
            }
            return -1;
        }

        public static async Task Get()
        {
            MqttClient client = new MqttClient("127.0.0.1", 1883, false, null, MqttSslProtocols.None); // 1883 default
           // MqttClient client = new MqttClient("mqtt.phelicks.net");

            X509Certificate cert = null; //X509Certificate.CreateFromCertFile(Certificate); Not used atm.
            MqttSslProtocols sslProtocol = new MqttSslProtocols();

            string clientId = Guid.NewGuid().ToString(); 
            var simJobIndex = 0;

            //client.Connect(clientId, "cab", "sjuttongubbar");
            client.Connect(clientId);
            //client.MqttMsgPublished += client_MqttMsgPublished;
            simJobStatus.Clear();

            for (;;)
            {

                using (var httpclient = new HttpClient())
                {
                    httpclient.BaseAddress = new Uri("http://localhost:50780/");
                    httpclient.DefaultRequestHeaders.Accept.Clear();
                    httpclient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage response = await httpclient.GetAsync("api/SimJob"+"?includeSnapshots=true");
                    if (response.IsSuccessStatusCode)
                    {
                        List<SimJobModel> simJobList = await response.Content.ReadAsAsync<List<SimJobModel>>();

                        foreach (var simjob in simJobList)
                        {
                            //var simjobx = simJobStatus.Find(x => x.SimJobId.Contains(simjob.SimJobId));
                            simJobIndex = findSimJob(simjob); // simjob already exist
                            if (simJobIndex >= 0)
                            {
                                if (simjob.jobStarted == false) // existing simjob stopped - remove thread
                                {
                                    simJobStatus[simJobIndex].active = false; // mark passive- to be removed
                                }
                                else // update with latest fields from GUI
                                    simJobStatus[simJobIndex].tws.UpdateSimulatedCar(simjob.carId, simjob.repeatJob, simjob.speedX);
                            }
                            else
                            { // new simjob, not stored locally
                                if (simjob.jobStarted)
                                {
                                    SimJobStatus sim = new SimJobStatus();
                                    sim.ObjectId = simjob.SimJobId;
                                    sim.active = true;

                                    SimulatedCar tws = new SimulatedCar(client, simjob);
                                    tws.UpdateSimulatedCar(simjob.carId, simjob.repeatJob, simjob.speedX);
                                    Thread t = new Thread(new ThreadStart(tws.ThreadProc));
                                    sim.task = t;
                                    sim.tws = tws;
                                    simJobStatus.Add(sim); 
                                    t.Start();
                                }
                            }
                        }
                        for (int i = simJobStatus.Count - 1; i >= 0; i--) // remove passive simjobs
                        {
                            if (simJobStatus[i].active == false)
                            {
                                simJobStatus[i].task.Abort();
                                Console.WriteLine("Simjob terminated.\n");

                                simJobStatus.RemoveAt(i);
                            }
                        }
                    }
                }
                System.Threading.Thread.Sleep(100);
            }
        }
    }
}

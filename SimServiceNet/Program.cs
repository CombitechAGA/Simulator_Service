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

#if false
private static void start_get()
{
    //Our getVars, to test the get of our php. 
    //We can get a page without any of these vars too though.
    string getVars = "?var1=test1&var2=test2";
    //Initialization, we use localhost, change if applicable
    HttpWebRequest WebReq = (HttpWebRequest)WebRequest.Create
        (string.Format("http://127.0.0.1/test.php{0}", getVars));
    //This time, our method is GET.
    WebReq.Method = "GET";
    //From here on, it's all the same as above.
    HttpWebResponse WebResp = (HttpWebResponse)WebReq.GetResponse();
    //Let's show some information about the response
    Console.WriteLine(WebResp.StatusCode);
    Console.WriteLine(WebResp.Server);

    //Now, we read the response (the string), and output it.
    Stream Answer = WebResp.GetResponseStream();
    StreamReader _Answer = new StreamReader(Answer);
    Console.WriteLine(_Answer.ReadToEnd());

    //Congratulations, with these two functions in basic form, you just learned
    //the two basic forms of web surfing
    //This proves how easy it can be.
}
#endif

namespace SimServiceNet
{
    class Program
    {
        public static object Resources { get; private set; }
        public static string Certificate { get; private set; }

        static void Main(string[] args)
        {
            Get().Wait();
        }

        void client_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            Console.WriteLine("MessageId = " + e.MessageId + " Published = " + e.IsPublished);
        }

        public static async Task Get()
        {
          
            //MqttClient client = new MqttClient("127.0.0.1"); // 1883 default
            X509Certificate cert = null; //X509Certificate.CreateFromCertFile(Certificate);
            MqttSslProtocols sslProtocol = new MqttSslProtocols();

            MqttClient client = new MqttClient("mqtt.phelicks.net");


            string clientId = Guid.NewGuid().ToString(); // TEMP hack set client.settings.port to 9001
            client.Connect(clientId, "cab", "sjuttongubbar");
            //client.Connect(clientId);
            //client.MqttMsgPublished += client_MqttMsgPublished;
            for (;;)
            {
                using (var httpclient = new HttpClient())
                {
                    httpclient.BaseAddress = new Uri("http://localhost:50780/");
                    httpclient.DefaultRequestHeaders.Accept.Clear();
                    httpclient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage response = await httpclient.GetAsync("api/SimJob");
                    if (response.IsSuccessStatusCode)
                    {
                        List<SimJobModel> simJobList = await response.Content.ReadAsAsync<List<SimJobModel>>();
                        foreach (var simjob in simJobList)
                        {
                            if (simjob.jobStarted == true)
                            {
                                foreach (var route in simjob.Routes)
                                {
                                    foreach (var snapshot in route.Snapshots)
                                    {
                                        string strValue = snapshot.carID + ";ts:" + snapshot.timestamp.ToString() + ";fuel:0;speed:" + snapshot.speed.ToString() + ";distance:" + snapshot.distanceTraveled + ";lat:" + snapshot.latitude + ";long:" + snapshot.longitude + ";name:" + "simcar"; // snapshot.zbeename;
                                        Console.WriteLine(strValue);
                                        string js = JsonConvert.SerializeObject(snapshot);
                                        client.Publish("telemetry/snapshot", Encoding.UTF8.GetBytes(strValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
                                        System.Threading.Thread.Sleep(3000);
                                    }
                                }
                            }
                        }
                    }
                }
                System.Threading.Thread.Sleep(1000); 
            }
        }
    }
}

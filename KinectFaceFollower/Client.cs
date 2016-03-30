using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace KinectFaceFollower
{
    public class Client
    {

    
        public  static bool callDB(string message)
        {
            //ring the doorbell
            var dbFactory = new ChannelFactory<IDoorbellChannel>(
            new NetTcpRelayBinding(),
                new EndpointAddress(ServiceBusEnvironment.CreateServiceUri("sb", "projectnavi", "doorbell")));

            dbFactory.Endpoint.Behaviors.Add(new TransportClientEndpointBehavior { TokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider("RootManageSharedAccessKey", "24SX3tcvAwf5lhbSQ/YNsGM4mXGvpREpVLmfrKmz4WM=") });

            using (var db = dbFactory.CreateChannel())
            {
                return db.IsAllowedIn(message);
            }
        }
    
}
}

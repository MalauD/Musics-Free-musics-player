﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Utility.Network.Tracker.Identity;
using Utility.Network.Tracker.Requests;

namespace Tracker.Network.Trackers.Services
{
    public class ConnectionService
    {
        static private readonly Lazy<ConnectionService> instance = new Lazy<ConnectionService>(() => new ConnectionService());
        static public ConnectionService Instance { get => instance.Value; }

        public void Start()
        {
            Program._Tracker.OnPacketreceived += _Tracker_OnPacketreceived;
        }

        private void _Tracker_OnPacketreceived(object sender, Utility.Network.Server.PacketEventArgs a)
        {
            Program._Tracker.Logger.Info($"Receiving data from a socket - {a.Packet.GetType()}");
            if (a.Packet is TrackerRequest)
            {
                if (a.Packet is Register)
                {
                    Register reg = (a.Packet as Register);
                    Program._Tracker.Logger.Info("Receiving register demand from the socket");
                    if (reg.Identity is ServerIdentity)
                    {
                        int Port = reg.Identity.IPEndPoint.Port;
                        reg.Identity = new ServerIdentity()
                        {
                            IPEndPoint = a.Sender.RemoteEndPoint as IPEndPoint
                        };
                        reg.Identity.IPEndPoint.Port = Port;
                        Program._Tracker.AddServer(reg.Identity as ServerIdentity, a.Sender);
                        foreach (var id in Program._Tracker.Idlist.GetClientIdentity())
                            new ServerRequestAnswer(Program._Tracker.Idlist.GetServerIDAvailable()).Send(Program._Tracker.Idlist.GetSocketByID(id));
                    }
                    else
                    {
                        Program._Tracker.Idlist.AddIdentity(a.Sender, reg.Identity);
                    } 
                    new RegisterAck(true).Send(a.Sender);
                    Program._Tracker.Logger.Info("Done.");
                    return;
                }
            }
            Program._Tracker.Logger.Warn($"Uknown IPacket ! - {a.Packet.GetType()}");
        }
    }
}

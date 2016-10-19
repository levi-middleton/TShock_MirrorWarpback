using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using tShock_Util;
using TShockAPI;
using System.IO.Streams;
using static TShockAPI.GetDataHandlers;

namespace MirrorWarpback
{
    [ApiVersion(1, 25)]
    public class MirrorWarpback : TerrariaPlugin
    {
        public override Version Version
        {
            get
            {
                return new Version("1.1");
            }
        }

        public override string Name
        {
            get
            {
                return "MirrorWarpback - Custom";
            }
        }

        public override string Author
        {
            get
            {
                return "Brian Emmons and Levi Middleton";
            }
        }

        public override string Description
        {
            get
            {
                return "Lets you use a recall potion to return to the spot where you last used a magic mirror, ice mirror, or cell phone. Requires mw.warpback permission.";
            }
        }

        public enum WarpbackState
        {
            None,
            WaitingForSpawn,
            Available
        }

        public class WarpbackData
        {
            private TSPlayer Plr;
            private WarpbackState WarpbackState;
            private float X;
            private float Y;
            private PlayerDB.DB db = new PlayerDB.DB("MirrorWarpback", new String[] { "Avail", "X", "Y" });

            public static WarpbackData Get( TSPlayer plr )
            {
                WarpbackData ret = plr.GetData<WarpbackData>("warpback");
                if( ret == null )
                {
                    ret = new WarpbackData(plr);
                    plr.SetData<WarpbackData>("warpback", ret);
                }
                return ret;
            }

            public bool Available
            {
                get
                {
                    return WarpbackState == WarpbackState.Available;
                }
            }

            public bool WaitingForSpawn
            {
                get
                {
                    return WarpbackState == WarpbackState.WaitingForSpawn;
                }
            }

            public WarpbackData(TSPlayer plr)
            {
                Plr = plr;
                if (Plr.UUID != "")
                {
                    if(!Enum.TryParse<WarpbackState>(db.GetUserData(plr, "Avail"), out WarpbackState))
                    {
                        WarpbackState = WarpbackState.None;
                    }
                    
                    if (Available)
                    {
                        X = Convert.ToSingle(db.GetUserData(plr, "X"));
                        Y = Convert.ToSingle(db.GetUserData(plr, "Y"));
                    }
                }
                else
                {
                    TShock.Log.ConsoleError("WARNING: WarpbackData initialized before UUID available for " + plr.Name + "!");
                }
            }

            public void Set(float x, float y, WarpbackState avail)
            {
                WarpbackState = avail;
                X = x;
                Y = y;
                if( Plr.UUID != "" )
                    db.SetUserData(Plr, new List<string> { WarpbackState.ToString(), Convert.ToString(X), Convert.ToString(Y) });
            }

            public void Clear()
            {
                WarpbackState = WarpbackState.None;
                if( Plr.UUID != "" )
                    db.DelUserData(Plr.UUID);
            }

            public void Teleport(byte effect = 1)
            {
                if (WarpbackState != WarpbackState.Available)
                    return;

                Plr.Teleport(X, Y, effect);
                Clear();
            }

            public void Spawned()
            {
                if (WarpbackState != WarpbackState.WaitingForSpawn)
                    return;

                Set(X, Y, WarpbackState.Available);
            }
        }
        
        public bool[] Using = new bool[255];

        public MirrorWarpback(Main game) : base(game)
        {

        }

        public override void Initialize()
        {
            GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
            GetDataHandlers.PlayerSpawn += OnPlayerSpawn;
            GetDataHandlers.KillMe += OnKillMe;
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
            
        }

        protected override void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
                GetDataHandlers.PlayerSpawn -= OnPlayerSpawn;
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
            }
            base.Dispose(Disposing);
        }

        private void SendInfoMessageIfPresent( TSPlayer p, string msg )
        {
            if( p != null && p.ConnectionAlive && !string.IsNullOrEmpty(msg) )
            {
                p.SendInfoMessage(msg);
            }
        }

        public void OnGreet(GreetPlayerEventArgs args)
        {
            TSPlayer p = TShock.Players[args.Who];

            if (!p.ConnectionAlive)
            {
                return;
            }

            if (p.User == null)
            {
                // Player hasn't logged in or has no account.
                return;
            }
            
            WarpbackData wb = WarpbackData.Get(p);

            if( wb.Available ) {
                wb.Clear();
            }
        }

        private void OnKillMe(object sender, GetDataHandlers.KillMeEventArgs args)
        {
            TSPlayer p = TShock.Players[args.PlayerId];

            if (!p.ConnectionAlive)
            {
                return;
            }

            WarpbackData wb = WarpbackData.Get(p);

            if (wb.Available)
            {
                wb.Clear();
            }
        }

        private void OnPlayerSpawn(object sender, GetDataHandlers.SpawnEventArgs args)
        {
            TSPlayer p = TShock.Players[args.Player];

            if(!p.ConnectionAlive)
            {
                return;
            }

            WarpbackData wb = WarpbackData.Get(p);
            
            if(wb.Available)
            {
                wb.Teleport(Config.returnEffect);
            }
            else if (wb.WaitingForSpawn)
            {
                wb.Spawned();
            }
        }

        private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
        {

            if ((args.Control & 32) != 32)
            {
                Using[args.PlayerId] = false;
                return;
            }

            if (Using[args.PlayerId])
            {
                return;
            }

            TSPlayer p = TShock.Players[args.PlayerId];
            if(!p.ConnectionAlive)
            {
                return;
            }

            if (!p.HasPermission("mw.warpback"))
            {
                return;
            }

            Using[args.PlayerId] = true;
                
            //int uid = TShock.Players[args.PlayerId].User.ID;
            Item it = TShock.Players[args.PlayerId].TPlayer.inventory[args.Item];

            if ( (it.type == ItemId.Magic_Mirror || it.type == ItemId.Cell_Phone || it.type == ItemId.Ice_Mirror) && (Config.returnItemType != 0) ) // Magic Mirror, Cell Phone, Ice Mirror
            {
                WarpbackData wb = WarpbackData.Get(TShock.Players[args.PlayerId]);
                
                SendInfoMessageIfPresent(p, Config.msgOnMirrorTeleport);
                        
                wb.Set(p.X, p.Y, WarpbackState.WaitingForSpawn);
            }
            else if (it.type == Config.returnItemType && Config.returnItemType != 0)
            {
                WarpbackData wb = WarpbackData.Get(TShock.Players[args.PlayerId]);

                if(!wb.Available)
                {
                    return;
                }
                
                SendInfoMessageIfPresent(p, Config.msgOnWarpbackTeleport);
            }
        }
    }
}

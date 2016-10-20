using System.IO;
using Newtonsoft.Json;
using TShockAPI;

namespace MirrorWarpback
{
    public class Config
    {
        public static readonly int returnItemType = ItemId.Recall_Potion;
        public static readonly byte returnEffect = 1;
        public static readonly string msgOnMirrorTeleport = "You leave behind a memory of an adventure undone.";
        public static readonly string msgOnWarpbackTeleport = "You remember an expedition postponed.";
   }
}

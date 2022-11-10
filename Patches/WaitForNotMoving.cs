using HBMP.Object;
using UnityEngine;

namespace HBMP.Patches
{
    public class WaitForNotMoving : CustomYieldInstruction
    {
        private SyncedObject syncedObject;

        public WaitForNotMoving(SyncedObject syncedObject)
        {
            this.syncedObject = syncedObject;
        }

        public override bool keepWaiting {
            get
            {
                return syncedObject.HasChangedPositions();
            }
        }
    }
}
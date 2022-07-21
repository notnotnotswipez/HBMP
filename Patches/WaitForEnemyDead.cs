using UnityEngine;

namespace HBMP.Patches
{
    public class WaitForEnemyDead : CustomYieldInstruction
    {
        private HealthContainer _healthContainer;

        public WaitForEnemyDead(HealthContainer healthContainer)
        {
            this._healthContainer = healthContainer;
        }

        public override bool keepWaiting {
            get
            {
                return !_healthContainer.IsHealthContainerEmpty;
            }
        }
    }
}
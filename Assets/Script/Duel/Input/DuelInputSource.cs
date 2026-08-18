using UnityEngine;

namespace AOG.Duel
{
    public abstract class DuelInputSource : MonoBehaviour
    {
        public abstract DuelPlayerCommand ReadCommand();
    }
}

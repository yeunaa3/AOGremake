using UnityEngine;

namespace AOG.Duel
{
    public abstract class InputSource : MonoBehaviour
    {
        public abstract InputCmd ReadCommand();
    }
}

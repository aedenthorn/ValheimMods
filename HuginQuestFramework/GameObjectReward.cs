using UnityEngine;

namespace HuginQuestFramework
{
    public class GameObjectReward
    {
        public GameObject gameObject;
        public int reward;

        public GameObjectReward(GameObject go, int value)
        {
            gameObject = go;
            reward = value;
        }
    }
}
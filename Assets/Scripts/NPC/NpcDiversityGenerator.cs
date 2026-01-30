using UnityEngine;

namespace NPC
{
    public class NpcDiversityGenerator : MonoBehaviour
    {
        public Vector2 sizeMinMax;
        public GameObject[] randomObjects;
        void Start()
        {
            var randomisedSize = Random.Range(sizeMinMax.x, sizeMinMax.y);
            transform.localScale = new Vector3(
                transform.localScale.x * randomisedSize,
                transform.localScale.y * randomisedSize,
                transform.localScale.z * randomisedSize);
            foreach (var obj in randomObjects)
            {
                obj.SetActive(Random.Range(0, 100) > 50);
            }
        }
    }
}

using System;
using UnityEngine;



public class Teleport : MonoBehaviour {

    [SerializeField] string levelName;                          //nazwa nastêpnej sceny
    [SerializeField] string seedKey = "IslandSeed";             //seed nastêpnej sceny

    public void SetLevelName(string name)
    {
        levelName = name;
    }

    private void OnTriggerEnter(Collider other)
    {
        

        if (other.gameObject.activeSelf)
        {
            //teleportacja na lewituj¹c¹ wyspê
            Debug.Log("kolizja");
            SceneController.instance.LoadScene(levelName);

            int newSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            PlayerPrefs.SetInt(seedKey, newSeed);
            PlayerPrefs.Save();

            Debug.Log($"Teleport -> nowy seed: {newSeed}");
            SceneController.instance.LoadScene(levelName);
        }
    }

}
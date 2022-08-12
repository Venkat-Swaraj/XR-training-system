using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ImageTracker : MonoBehaviour
{

    public List<GameObject> trackedImagePrefabs = new List<GameObject>();
    public List<Texture2D> trackedImageImages = new List<Texture2D>();
    public List<string> trackedImageNames = new List<string>();


    public abstract void RegisterImage(Texture2D texture, string imageName, float scale, GameObject prefab);

    public abstract void RemoveImage(string imageName);

    public abstract void ReplaceImagePrefab(string imageName, GameObject newPrefab);

    public abstract GameObject GetTackedObject(string imageName);

}

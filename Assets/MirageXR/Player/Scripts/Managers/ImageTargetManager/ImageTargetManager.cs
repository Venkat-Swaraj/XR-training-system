using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


namespace MirageXR
{
    public class ImageTargetManager : MonoBehaviour
    {

        private ImageTracker imageTracker;


        private void CreateImageTracker() 
        {

#if UNITY_ANDROID || UNITY_IOS
            imageTracker = new ImageTrackerMobile();
#else
            imageTracker = new ImageTrackerHololens();
#endif
        }

        private ImageTracker ImageTrackerInstance() 
        {
            if (imageTracker == null)
            {
                CreateImageTracker();
            }

            return imageTracker;
        }



        public void AddImageToImageTracker(string imageName, string path, GameObject prefab)
        {
            Texture2D image = LoadImage(path, imageName);

            ImageTrackerInstance().RegisterImage(image, imageName, image.width, prefab);
        }

        public void AddImageToImageTracker(string imageName, Texture2D texture, GameObject prefab)
        {
            
            ImageTrackerInstance().RegisterImage(texture, imageName, texture.width, prefab);
        }

        public void RemoveImageFromImagetracker(string imageName) 
        {
            ImageTrackerInstance().RemoveImage(imageName);
        }

        public void ReplaceImagePrefabInImageTracker(string imageName, GameObject newPrefab) 
        {
            ImageTrackerInstance().ReplaceImagePrefab(imageName, newPrefab);
        }

        private Texture2D LoadImage(string path, string name)
        {

            Texture2D loadTexture = new Texture2D(2, 2);

            byte[] byteArray = File.ReadAllBytes(Path.Combine(path, name));
            loadTexture.LoadImage(byteArray);

            return loadTexture;
        }

        /*
         * 
         * 
                private List<Texture2D> LoadImages(string path, List<string> imageNames) 
        {

            List<Texture2D> imageList = new List<Texture2D>();

            foreach (string name in imageNames) {

                imageList.Add(LoadImage(path, name));
            }

            return imageList;
        }

        public void AddImagesImageTrackerMobile(List<string> imageNames, List<GameObject> prefabList)
        {
            ImagetrackerMobileInstance().AddImagesToLibrary(LoadImages(RootObject.Instance.activityManager.ActivityPath, imageNames), imageNames, prefabList);
        }

        //Instantiate(imageTrackerMobilePrefab, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<ImageTrackerMobile>();
        
        [SerializeField] private GameObject imageTrackerMobilePrefab;

        [SerializeField] private GameObject imageTrackerHololensPrefab;

        //RootObject.Instance.activityManager.ActivityPath
         */




    }
}

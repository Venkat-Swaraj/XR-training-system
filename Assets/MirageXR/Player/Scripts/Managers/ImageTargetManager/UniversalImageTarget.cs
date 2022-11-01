using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class used to store image target data. Designed to be used for both Vuforia and ARFoundations tracking (and any other tracking solutions used in the future).
/// </summary>
public class UniversalImageTarget
{
    private string targetName;
    private string path;
    private Texture2D image;
    private float scale;
    private GameObject prefab;

    /// <summary>
    /// Gets or sets the image target name.
    /// </summary>
    public string TargetName
    {
        get { return this.targetName; }
        set { this.targetName = value; }
    }

    /// <summary>
    /// Gets or sets the path of the image target image file.
    /// </summary>
    public string Path
    {
        get { return this.path; }
        set { this.path = value; }
    }

    /// <summary>
    /// Gets or sets the texture2D of the image target.
    /// </summary>
    public Texture2D Image
    {
        get { return this.image; }
        set { this.image = value; }
    }

    /// <summary>
    /// Gets or sets the image target texture scale.
    /// </summary>
    public float Scale
    {
        get { return this.scale; }
        set { this.scale = value; }
    }

    /// <summary>
    /// Gets or sets the prefab that will be displayed when the iamge target is being tracked.
    /// </summary>
    public GameObject Prefab
    {
        get { return this.prefab; }
        set { this.prefab = value; }
    }
}

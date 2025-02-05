﻿using i5.Toolkit.Core.VerboseLogging;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace MirageXR
{
    public class Potentiometer : MirageXRPrefab
    {
        public override bool Init(ToggleObject obj)
        {
            // Check that all the sensor related crap is defined.
            if (string.IsNullOrEmpty(obj.sensor))
            {
                AppLog.LogWarning("Sensor is not defined.");
                return false;
            }

            if (string.IsNullOrEmpty(obj.key))
            {
                AppLog.LogWarning("Sensor key is not defined.");
                return false;
            }

            if (string.IsNullOrEmpty(obj.option))
            {
                AppLog.LogWarning("Sensor option string is not defined.");
                return false;
            }

            var limits = obj.option.Split(';');

            if (limits.Length != 2)
            {
                AppLog.LogWarning("Sensor limits not properly set.");
                return false;
            }

            if (!float.TryParse(limits[0], out float min))
            {
                AppLog.LogWarning("Minimum value is not a float.");
                return false;
            }


            if (!float.TryParse(limits[1], out float max))
            {
                AppLog.LogWarning("Maximum value is not a float.");
                return false;
            }

            var controller = GetComponent<SmartRotationController>();

            if (!controller.AttachStream(obj.sensor, obj.key, min, max))
            {
                AppLog.LogWarning("Couldn't attach sensor stream.");
                return false;
            }

            // Try to set the parent and if it fails, terminate initialization.
            if (!SetParent(obj))
            {
                AppLog.LogWarning("Couldn't set the parent.");
                return false;
            }

            // Set name.
            name = obj.predicate;

            // Set scaling if defined in action configuration.
            if (!obj.scale.Equals(0))
                transform.localScale = new Vector3(obj.scale, obj.scale, obj.scale) * WorkplaceManager.ScalingFactor;

            // If scaling is not defined, default to 5 cm symbols.
            else
                transform.localScale = new Vector3(0.05f, 0.05f, 0.05f) * WorkplaceManager.ScalingFactor;

            if (!obj.id.Equals("UserViewport"))
            {
                // Setup guide line feature.
                if (!SetGuide(obj))
                    return false;
            }

            else
            {
                gameObject.AddComponent<Billboard>();
            }

            // If everything was ok, return true.
            return true;
        }
    }
}
using System;
using System.Collections.Generic;
using LobstersUnited.HumbleDI;
using UnityEngine;


namespace LobstersUnited.HumbleDI_Sample.WithProviders {

    public interface IWheel {

        bool Rotate(float acceleration);

    }
    
    [Serializable]
    public class Wheel : IWheel {
        
        [SerializeField] Transform transform;
        [SerializeField] float minAcceleration = 0.0f;

        [SerializeField] List<float> sampleList;

        public Wheel(Transform transform) {
            this.transform = transform;
        }

        public bool Rotate(float acceleration) {
            if (Math.Abs(acceleration) > minAcceleration) {
                return true;
            }
            return false;
        }
    }

    public class WheelMB : MonoBehaviour, IProvider<IWheel> {

        [SerializeField] Wheel wheel;
        
        public IWheel Get() {
            return wheel;
        }

        void Start() {
            Debug.Log($"'{gameObject.name}' has wheel ref: {wheel}");
        }
    }
}

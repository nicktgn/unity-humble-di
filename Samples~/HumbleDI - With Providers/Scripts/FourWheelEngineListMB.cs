using System;
using System.Collections.Generic;
using LobstersUnited.HumbleDI;
using UnityEngine;
using UnityEngine.Serialization;

namespace LobstersUnited.HumbleDI_Sample.WithProviders {
    

    [Serializable]
    public class FourWheelEngineList : IEngine {

        [SerializeReference] InterfaceDependencies iDeps;
        
        List<IProvider<IWheel>> driveWheels;
        
        
        [SerializeField] Transform carTransform;

        public FourWheelEngineList(Transform carTransform, List<IProvider<IWheel>> driveWheels) {
            this.carTransform = carTransform;
            this.driveWheels = driveWheels;
        }

        public void Move(Vector3 acceleration) {
            carTransform.position += acceleration;
            
            foreach (var wheel in driveWheels) {
                wheel.Get().Rotate(acceleration.magnitude);
            }
        }
    }

    public class FourWheelEngineListMB : MonoBehaviour, IProvider<IEngine> {

        [FormerlySerializedAs("engine")]
        [SerializeField] FourWheelEngineList fourWheelEngineList;
        
        public IEngine Get() {
            return fourWheelEngineList;
        }
    }
    
}

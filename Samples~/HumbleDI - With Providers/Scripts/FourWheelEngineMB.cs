using System;
using LobstersUnited.HumbleDI;
using UnityEngine;
using UnityEngine.Serialization;

namespace LobstersUnited.HumbleDI_Sample.WithProviders {
    

    [Serializable]
    public class FourWheelEngine : IEngine {

        [SerializeReference] InterfaceDependencies iDeps;
        
        IProvider<IWheel>[] driveWheels;
        
        
        [SerializeField] Transform carTransform;

        public FourWheelEngine(Transform carTransform, IWheel[] driveWheels) {
            
        }

        public void Move(Vector3 acceleration) {
            carTransform.position += acceleration;
            
            foreach (var wheel in driveWheels) {
                wheel.Get().Rotate(acceleration.magnitude);
            }
        }
    }

    public class FourWheelEngineMB : MonoBehaviour, IProvider<IEngine> {

        [FormerlySerializedAs("engine")]
        [SerializeField] FourWheelEngine fourWheelEngine;
        
        public IEngine Get() {
            return fourWheelEngine;
        }
    }
    
}

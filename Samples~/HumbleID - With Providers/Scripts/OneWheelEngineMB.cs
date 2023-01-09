using System;
using LobstersUnited.HumbleDI;
using UnityEngine;
using UnityEngine.Serialization;

namespace LobstersUnited.HumbleDI_Sample.WithProviders {

    public interface IEngine {
        void Move(Vector3 acceleration);
    }

    [Serializable]
    public class OneWheelEngine : IEngine {

        [SerializeReference] InterfaceDependencies iDeps;
        
        //[SerializeField] IWheel[] driveWheels;
        IProvider<IWheel> oneWheel;
        
        [SerializeField] Transform carTransform;

        public OneWheelEngine(Transform carTransform, IWheel[] driveWheels) {
            
        }

        public void Move(Vector3 acceleration) {
            carTransform.position += acceleration;

            oneWheel.Get().Rotate(acceleration.magnitude);

            // foreach (var wheel in driveWheels) {
            //     wheel.Rotate(acceleration.magnitude);
            // }
        }
    }

    public class OneWheelEngineMB : MonoBehaviour, IProvider<IEngine> {

        [FormerlySerializedAs("engine")]
        [SerializeField] OneWheelEngine oneWheelEngine;
        
        public IEngine Get() {
            return oneWheelEngine;
        }
    }
    
}

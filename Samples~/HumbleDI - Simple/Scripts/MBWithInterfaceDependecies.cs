using UnityEngine;
using UnityEngine.UI;

using LobstersUnited.HumbleDI;

namespace LobstersUnited.HumbleDI_Sample {
    
    public class MBWithInterfaceDependecies : MonoBehaviour {

        // THIS IS REQUIRED
        // Need to add this to any MonoBehaviour or ScriptableObject to be able to serialize declared
        //  interface fields and show them in the inspector.
        //  Do not use or modify this field
        [SerializeField] InterfaceDependencies iDeps;
        
        IDependency dependency;

        IDependency anotherDependency;

        
        [SerializeField] CharacterController controller;

        void Start() {
            var result = dependency.DoSomethingImportant("INPUT From MonoBehavior Colaborator");
            
            Debug.Log($"MBWithInterfaceDependencies result: {result}");
        }
    }
    
}

using UnityEngine;

using LobstersUnited.HumbleDI;

namespace LobstersUnited.HumbleDI_Sample.Simple {
    
    public class MyAwesomeComponent : MonoBehaviour {

        // THIS IS REQUIRED
        // Need to add this to any MonoBehaviour or ScriptableObject to be able to serialize declared
        //  interface fields and show them in the inspector.
        //  DO NOT USE OR MODIFY THIS FIELD
        [SerializeField] InterfaceDependencies iDeps;
        
        // This field will be displayed under "Dependencies" section
        IDependency myDependency;

        [SerializeField] float myFloat;
        
        void Start() {
            var result = myDependency.DoSomethingImportant("INPUT From MonoBehavior Collaborator");
            
            Debug.Log($"MyAwesomeComponent result: {result}");
        }
    }
    
}

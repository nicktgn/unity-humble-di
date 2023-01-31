using LobstersUnited.HumbleDI;
using UnityEngine;

namespace LobstersUnited.HumbleDI_Sample.Simple {
    
    public class SimpleComponent : MonoBehaviour {
    
        // THIS IS REQUIRED
        // Need to add this to any MonoBehaviour or ScriptableObject to be able to serialize declared
        //  interface fields and show them in the inspector.
        //  DO NOT USE OR MODIFY THIS FIELD
        [SerializeField] InterfaceDependencies iDeps;
        
        // This field will be displayed under "Dependencies" section
        //    marking this as [Dependency] is redundant, but can be used for visual aesthetic  
        [Dependency] IDependency myDependency;
        
        // If you want to display normal fields (non-interface ones) under "Dependencies" section too, 
        //   add [Dependency] attribute. To hide it in default section of the inspector add [HideInInspector] attribute,
        //   otherwise it will be displayed twice.
        [Dependency(External = true, Optional = true)] [SerializeField, HideInInspector] 
        Camera mainCamera;
        
        // rest of the serialized fields

        [SerializeField] float myFloat;
        
        void Start() {
            var result = myDependency.DoSomethingImportant("INPUT From MonoBehavior Collaborator");
            
            Debug.Log($"SimpleComponent result: {result}");
        }
        
    }
}

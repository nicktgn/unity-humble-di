using LobstersUnited.HumbleDI;
using UnityEngine;

namespace LobstersUnited.HumbleDI_Sample {
    
    [CreateAssetMenu(menuName = "HumbleDI/SOWithDependencies")]
    public class SOWithDependencies : ScriptableObject {

        [SerializeField] private InterfaceDependencies iDeps;
        
        IDependency dependency;

        public string OwnMethod(string input) {
            var result = dependency.DoSomethingImportant("INPUT From ScriptableObject Collaborator");
            return "This SO needs dependency ${result}";
        }

    }
}
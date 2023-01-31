using LobstersUnited.HumbleDI;
using UnityEngine;

namespace LobstersUnited.HumbleDI_Sample.Simple {
    
    [CreateAssetMenu(menuName = "HumbleDI/SimpleSO")]
    public class SimpleSO : ScriptableObject {

        [SerializeField] private InterfaceDependencies iDeps;
        
        IDependency myDependency;

        public string AwesomeMethod(string input) {
            var result = myDependency.DoSomethingImportant("INPUT From ScriptableObject Collaborator");
            return "Simple SO result ${result}";
        }

    }
}
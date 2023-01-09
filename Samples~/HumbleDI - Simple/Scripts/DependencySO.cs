using UnityEngine;

namespace LobstersUnited.HumbleDI_Sample {
    
    [CreateAssetMenu(menuName = "HumbleDI/DependencySO")]
    public class DependencySO : ScriptableObject, IDependency {
        
        public string DoSomethingImportant(string input) {
            return $"Doing important work in ScriptableObject with input '{input}'";
        }
    }
    
}
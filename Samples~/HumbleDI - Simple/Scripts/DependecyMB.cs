using UnityEngine;

namespace LobstersUnited.HumbleDI_Sample.Simple  {
    
    public class DependecyMB : MonoBehaviour, IDependency {
        
        [SerializeField] Character myChar;
        
        public string DoSomethingImportant(string input) {
            return $"Doing important work in Component with input '{input}'";
        }

        void Start() {
            var result = DoSomethingImportant("INPUT FROM DEPENDENCY");
            
            Debug.Log($"DependencyMB Result: {result}");
        }

    }
}
using UnityEngine;

namespace LobstersUnited.HumbleDI.Tests {
    
    public static class A {

        public static GameObject GO = new GameObject();

        public static T Component<T>() where T : Component {
            return GO.AddComponent<T>();
        }

    }
    
}

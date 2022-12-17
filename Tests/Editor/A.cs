using UnityEngine;

namespace LobstersUnited.HumbleDI.Tests {
    
    static class A {

        public static GameObject GO() {
            return new GameObject();
        }

        public static T Component<T>() where T : Component {
            return GO().AddComponent<T>();
        }

        public static T SO<T>() where T : ScriptableObject {
            return ScriptableObject.CreateInstance<T>();
        }

    }

}
